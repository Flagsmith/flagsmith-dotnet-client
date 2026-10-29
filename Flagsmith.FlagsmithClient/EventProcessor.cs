#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Flagsmith
{
    /// <summary>
    /// Buffers tracking and exposure events, and sends them to the Flagsmith events API in batches:
    /// on a timer, and as soon as the buffer fills. Equal exposures are only sent once per batch.
    /// </summary>
    internal class EventProcessor : IDisposable
    {
        internal const string FlagExposureEvent = "$flag_exposure";

        private const int MaxAttempts = 2;
        private static readonly TimeSpan RetryBackoff = TimeSpan.FromSeconds(1);

        private readonly HttpClient _httpClient;
        private readonly string _eventsEndpoint;
        private readonly string _environmentKey;
        private readonly int _maxBufferItems;
        private readonly TimeSpan _requestTimeout;
        private readonly ILogger? _logger;
        private readonly Timer _timer;
        private readonly object _lock = new object();
        private List<Event> _buffer = new List<Event>();
        private readonly HashSet<(string?, string?, string?, string?)> _bufferedExposures = new HashSet<(string?, string?, string?, string?)>();
        private readonly HashSet<Task> _pendingSends = new HashSet<Task>();

        public EventProcessor(HttpClient httpClient, string environmentKey, Uri eventsApiUri, TimeSpan flushInterval, int maxBufferItems, TimeSpan requestTimeout, ILogger? logger)
        {
            if (maxBufferItems < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBufferItems), "The events buffer must hold at least one event.");
            }
            if (flushInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(flushInterval), "The events flush interval must be positive.");
            }

            _httpClient = httpClient;
            _environmentKey = environmentKey;
            _eventsEndpoint = eventsApiUri.ToString().AppendToUrl(false, "v1", "events");
            _maxBufferItems = maxBufferItems;
            _requestTimeout = requestTimeout;
            _logger = logger;
            _timer = new Timer(_ => _ = Flush(), null, flushInterval, flushInterval);
        }

        public void TrackEvent(string eventName, string? identifier, object? value, List<ITrait>? traits, Dictionary<string, object?>? metadata) =>
            Buffer(new Event(eventName, null, identifier, value, traits, metadata));

        public void TrackExposureEvent(string featureName, string identifier, object? value, List<ITrait>? traits, Dictionary<string, object?>? metadata) =>
            Buffer(new Event(FlagExposureEvent, featureName, identifier, value, traits, metadata));

        /// <summary>
        /// Sends every event buffered so far, and waits for batches already being sent.
        /// A batch the events API does not accept is logged and dropped.
        /// </summary>
        public Task Flush()
        {
            List<Event>? events = null;
            TaskCompletionSource<bool>? sent = null;
            Task[] pendingSends;
            lock (_lock)
            {
                if (_buffer.Count > 0)
                {
                    events = _buffer;
                    _buffer = new List<Event>();
                    _bufferedExposures.Clear();
                    // Registered before the lock is released, so a concurrent Flush always waits for this batch.
                    sent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingSends.Add(sent.Task);
                }
                pendingSends = _pendingSends.ToArray();
            }

            if (events != null)
            {
                _ = SendTracked(events, sent!);
            }
            return Task.WhenAll(pendingSends);
        }

        public void Dispose() => _timer.Dispose();

        private void Buffer(Event @event)
        {
            bool isFull;
            lock (_lock)
            {
                if (@event.Name == FlagExposureEvent && !_bufferedExposures.Add(@event.ExposureKey))
                {
                    _logger?.LogDebug("Skipping duplicate {Event} event for feature {FeatureName} and identity {Identifier}", @event.Name, @event.FeatureName, @event.Identifier);
                    return;
                }
                _buffer.Add(@event);
                isFull = _buffer.Count >= _maxBufferItems;
            }

            if (isFull)
            {
                _ = Flush();
            }
        }

        private async Task SendTracked(List<Event> events, TaskCompletionSource<bool> sent)
        {
            try
            {
                await Send(events).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    _pendingSends.Remove(sent.Task);
                }
                sent.SetResult(true);
            }
        }

        private async Task Send(List<Event> events)
        {
            string body;
            try
            {
                body = JsonConvert.SerializeObject(new { events });
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Dropping {EventCount} events that could not be serialised", events.Count);
                return;
            }

            for (var attempt = 1; ; attempt++)
            {
                string failure;
                bool retryable;
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _eventsEndpoint)
                    {
                        Headers =
                        {
                            { "X-Environment-Key", _environmentKey },
                            { "User-Agent", SdkVersion.GetUserAgent() },
                            { "Flagsmith-SDK-User-Agent", SdkVersion.GetUserAgent() }
                        },
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                    using var cancellationTokenSource = new CancellationTokenSource(_requestTimeout);
                    var response = await _httpClient.SendAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                    failure = $"HTTP {(int)response.StatusCode}";
                    retryable = (int)response.StatusCode >= 500;
                }
                catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException)
                {
                    failure = e.Message;
                    retryable = true;
                }
                catch (Exception e)
                {
                    failure = e.Message;
                    retryable = false;
                }

                if (!retryable || attempt >= MaxAttempts)
                {
                    _logger?.LogError("Dropping {EventCount} events the events API did not accept: {Failure}", events.Count, failure);
                    return;
                }
                _logger?.LogWarning("Retrying {EventCount} events the events API did not accept: {Failure}", events.Count, failure);
                await Task.Delay(RetryBackoff).ConfigureAwait(false);
            }
        }

        private class Event
        {
            public Event(string name, string? featureName, string? identifier, object? value, List<ITrait>? traits, Dictionary<string, object?>? metadata)
            {
                Name = name;
                FeatureName = featureName;
                Identifier = identifier;
                Value = AsEventValue(value);
                Traits = AsEventTraits(traits);
                Metadata = new Dictionary<string, object?>(metadata ?? new Dictionary<string, object?>()) { ["sdk_version"] = SdkVersion.Version };
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            [JsonProperty("event")]
            public string Name { get; }

            [JsonProperty("feature_name")]
            public string? FeatureName { get; }

            [JsonProperty("identifier")]
            public string? Identifier { get; }

            [JsonProperty("value")]
            public string? Value { get; }

            [JsonProperty("traits")]
            public Dictionary<string, object?>? Traits { get; }

            [JsonProperty("metadata")]
            public Dictionary<string, object?> Metadata { get; }

            [JsonProperty("timestamp")]
            public long Timestamp { get; }

            [JsonIgnore]
            public (string?, string?, string?, string?) ExposureKey =>
                (FeatureName, Identifier, Value, Metadata.TryGetValue("experiment_id", out var experimentId) ? AsEventValue(experimentId) : null);

            private static string? AsEventValue(object? value) => value switch
            {
                null => null,
                bool boolean => boolean ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };

            private static Dictionary<string, object?>? AsEventTraits(List<ITrait>? traits)
            {
                var persisted = traits?.Where(trait => !trait.GetTransient()).ToList();
                return persisted == null || persisted.Count == 0
                    ? null
                    : persisted.ToDictionary(trait => trait.GetTraitKey(), trait => (object?)trait.GetTraitValue());
            }
        }
    }
}
