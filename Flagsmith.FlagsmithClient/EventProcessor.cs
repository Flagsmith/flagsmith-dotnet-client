#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// One batch is in flight at a time. A batch is sent up to <see cref="MaxAttempts"/> times, backing off
    /// with full jitter between attempts. A batch whose last attempt fails transiently is kept for the next
    /// flush, unless that attempt got a 500. A batch the events API rejects is dropped, and an events API
    /// refusing the environment key stops event tracking. When the buffer is full, its oldest event is dropped.
    /// </summary>
    internal class EventProcessor : IDisposable
    {
        internal const string FlagExposureEvent = "$flag_exposure";

        internal const int MaxAttempts = 3;
        internal static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);
        internal static readonly TimeSpan DefaultCloseTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RetryBackoff = TimeSpan.FromSeconds(1);
        private static readonly int[] RetriedClientErrors = { 408, 429 };
        private static readonly Random Jitter = new Random();

        private readonly HttpClient _httpClient;
        private readonly string _eventsEndpoint;
        private readonly string _environmentKey;
        private readonly int _maxBufferItems;
        private readonly TimeSpan _requestTimeout;
        private readonly ILogger? _logger;
        private readonly Timer _timer;
        private readonly object _lock = new object();
        // Held while a batch is in flight, so that one batch is sent at a time.
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        // Cancelled on close, to stop waiting on retries and unanswered requests.
        private readonly CancellationTokenSource _closed = new CancellationTokenSource();
        private List<Event> _buffer = new List<Event>();
        private readonly HashSet<(string?, string?, string?, string?)> _bufferedExposures = new HashSet<(string?, string?, string?, string?)>();
        // Events tracked so far, and of those, events taken into a batch: a flush waiting for a batch in flight
        // does not send again the events it carried.
        private long _trackedCount;
        private long _takenCount;
        private bool _isSending;
        private bool _fullBufferFlushPending;
        // Whether the buffer holds a batch kept after failing: filling the buffer then waits for the next
        // flush rather than starting another round of retries with each event tracked.
        private bool _holdsKeptBatch;
        private bool _refused;
        private bool _isClosed;

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
        /// Sends the events tracked so far, and completes once each of them has been delivered, dropped,
        /// or kept for the next flush, retries included. Waits for a batch already in flight first, and
        /// does not send again the events it carried.
        /// </summary>
        public Task Flush()
        {
            long? trackedCount;
            lock (_lock)
            {
                trackedCount = _isSending ? _trackedCount : (long?)null;
            }
            return Send(trackedCount);
        }

        /// <summary>
        /// Stops sending on a timer, and sends the buffered events, waiting for at most <paramref name="timeout"/>.
        /// Events not sent by then are not retried, and events tracked afterwards are not sent.
        /// </summary>
        public async Task Close(TimeSpan timeout)
        {
            lock (_lock)
            {
                if (_isClosed)
                {
                    return;
                }
                _isClosed = true;
            }
            _timer.Dispose();

            var finalFlush = Send(null);
            if (await Task.WhenAny(finalFlush, Task.Delay(timeout)).ConfigureAwait(false) != finalFlush)
            {
                _logger?.LogWarning("Gave up sending events after {Timeout} while closing", timeout);
            }
            _closed.Cancel();
        }

        public void Dispose()
        {
            _timer.Dispose();
            _closed.Cancel();
        }

        private void Buffer(Event @event)
        {
            long trackedCount;
            lock (_lock)
            {
                if (_isClosed || _refused)
                {
                    _logger?.LogDebug("Not tracking {Event} event: {Reason}", @event.Name, _isClosed ? "the client is closed" : "the events API refused the environment key");
                    return;
                }
                if (@event.Name == FlagExposureEvent && !_bufferedExposures.Add(@event.ExposureKey))
                {
                    _logger?.LogDebug("Skipping duplicate {Event} event for feature {FeatureName} and identity {Identifier}", @event.Name, @event.FeatureName, @event.Identifier);
                    return;
                }
                if (_buffer.Count >= _maxBufferItems)
                {
                    _logger?.LogWarning("Events buffer is full, dropping the oldest {Event} event", _buffer[0].Name);
                    _buffer.RemoveAt(0);
                }
                _buffer.Add(@event);
                _trackedCount++;
                if (_buffer.Count < _maxBufferItems || _fullBufferFlushPending || _holdsKeptBatch)
                {
                    return;
                }
                _fullBufferFlushPending = true;
                trackedCount = _trackedCount;
            }

            _ = Send(trackedCount, fullBuffer: true);
        }

        /// <summary>
        /// Sends the buffered events as one batch. Given <paramref name="trackedCount"/>, does nothing if every
        /// event tracked up to it has been taken into a batch since.
        /// </summary>
        private async Task Send(long? trackedCount, bool fullBuffer = false)
        {
            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                List<Event> events;
                lock (_lock)
                {
                    if (fullBuffer)
                    {
                        _fullBufferFlushPending = false;
                    }
                    if (_buffer.Count == 0 || (trackedCount != null && _takenCount >= trackedCount))
                    {
                        return;
                    }
                    events = _buffer;
                    _buffer = new List<Event>();
                    _bufferedExposures.Clear();
                    _takenCount = _trackedCount;
                    _holdsKeptBatch = false;
                    _isSending = true;
                }

                var outcome = await Deliver(events).ConfigureAwait(false);

                lock (_lock)
                {
                    _isSending = false;
                    if (outcome == DeliveryOutcome.Kept)
                    {
                        _holdsKeptBatch = true;
                        _buffer = events.Concat(_buffer).Skip(Math.Max(0, events.Count + _buffer.Count - _maxBufferItems)).ToList();
                        _bufferedExposures.Clear();
                        _bufferedExposures.UnionWith(_buffer.Where(e => e.Name == FlagExposureEvent).Select(e => e.ExposureKey));
                    }
                    else if (outcome == DeliveryOutcome.Refused)
                    {
                        _refused = true;
                        _buffer.Clear();
                        _bufferedExposures.Clear();
                    }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task<DeliveryOutcome> Deliver(List<Event> events)
        {
            string body;
            try
            {
                body = JsonConvert.SerializeObject(new { events });
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Dropping {EventCount} events that could not be serialised", events.Count);
                return DeliveryOutcome.Dropped;
            }

            int? status = null;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    var backoff = TimeSpan.FromMilliseconds(RetryBackoff.TotalMilliseconds * Math.Pow(2, attempt - 2) * NextJitter());
                    try
                    {
                        await Task.Delay(backoff, _closed.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogWarning("Not retrying {EventCount} events: the client is closed", events.Count);
                        return DeliveryOutcome.Kept;
                    }
                }

                string failure;
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
                    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token);
                    cancellationTokenSource.CancelAfter(_requestTimeout);
                    using var response = await _httpClient.SendAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);
                    status = (int)response.StatusCode;
                    if (response.IsSuccessStatusCode)
                    {
                        return DeliveryOutcome.Delivered;
                    }
                    if (status == 401 || status == 403)
                    {
                        _logger?.LogError("The events API refused the environment key with HTTP {Status}, stopping event tracking", status);
                        return DeliveryOutcome.Refused;
                    }
                    if (status < 500 && !RetriedClientErrors.Contains(status.Value))
                    {
                        _logger?.LogError("Dropping {EventCount} events the events API rejected with HTTP {Status}", events.Count, status);
                        return DeliveryOutcome.Dropped;
                    }
                    failure = $"HTTP {status}";
                }
                catch (Exception e) when (e is HttpRequestException || e is OperationCanceledException || e is IOException)
                {
                    status = null;
                    failure = e is OperationCanceledException ? "no response" : e.Message;
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Dropping {EventCount} events that could not be sent", events.Count);
                    return DeliveryOutcome.Dropped;
                }

                _logger?.LogWarning("Failed to send {EventCount} events, attempt {Attempt} of {MaxAttempts}: {Failure}", events.Count, attempt, MaxAttempts, failure);
            }

            // A 500 may be the batch's own doing, so sending it again would only fail again.
            if (status == 500)
            {
                _logger?.LogError("Dropping {EventCount} events the events API failed with HTTP 500", events.Count);
                return DeliveryOutcome.Dropped;
            }
            _logger?.LogWarning("Keeping {EventCount} events for the next flush", events.Count);
            return DeliveryOutcome.Kept;
        }

        private static double NextJitter()
        {
            lock (Jitter)
            {
                return Jitter.NextDouble();
            }
        }

        private enum DeliveryOutcome
        {
            Delivered,
            Dropped,
            Kept,
            Refused,
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
