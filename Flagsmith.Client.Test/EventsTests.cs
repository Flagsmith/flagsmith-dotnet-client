using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Flagsmith.FlagsmithClientTest
{
    public class EventsTests
    {
        private static readonly string SdkVersionNumber = SdkVersion.GetUserAgent().Split('/')[1];

        private static Mock<HttpClient> MockFlagsmith(string identityResponse = null, HttpStatusCode eventsStatus = HttpStatusCode.Accepted) =>
            HttpMocker.MockHttpResponse(new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/identities/", new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(identityResponse ?? Fixtures.ApiIdentityResponse) } },
                { "/v1/events", new HttpResponseMessage { StatusCode = eventsStatus } },
            });

        private static FlagsmithConfiguration EventsConfiguration(Mock<HttpClient> mockHttpClient) => new FlagsmithConfiguration
        {
            EnvironmentKey = Fixtures.ApiKey,
            HttpClient = mockHttpClient.Object,
            EnableEvents = true,
            EventsApiUri = new Uri("http://events.test/"),
        };

        [Fact]
        public async Task TestTrackedEventReachesTheEventsApi()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            var traits = new List<ITrait> { new Trait("plan", "premium"), new Trait("email", "user@example.com", transient: true) };

            // When
            flagsmith.TrackEvent("checkout_completed", "user_123", 42, traits, new Dictionary<string, object> { ["currency"] = "GBP" });
            await flagsmith.FlushEvents();

            // Then
            var batch = Assert.Single(mockHttpClient.PostedJson("/v1/events"));
            var trackedEvent = (JObject)Assert.Single(batch["events"]);
            Assert.Equal(JTokenType.Integer, trackedEvent["timestamp"].Type);
            trackedEvent.Remove("timestamp");
            var expectedEvent = JObject.FromObject(new Dictionary<string, object>
            {
                ["event"] = "checkout_completed",
                ["feature_name"] = null,
                ["identifier"] = "user_123",
                ["value"] = "42",
                ["traits"] = new Dictionary<string, object> { ["plan"] = "premium" },
                ["metadata"] = new Dictionary<string, object> { ["currency"] = "GBP", ["sdk_version"] = SdkVersionNumber },
            });
            Assert.True(JToken.DeepEquals(expectedEvent, trackedEvent), trackedEvent.ToString());
            mockHttpClient.Verify(x => x.SendAsync(It.Is<HttpRequestMessage>(request =>
                request.RequestUri.ToString() == "http://events.test/v1/events" &&
                request.Headers.GetValues("X-Environment-Key").Single() == Fixtures.ApiKey &&
                request.Headers.GetValues("Flagsmith-SDK-User-Agent").Single() == SdkVersion.GetUserAgent()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TestTrackedExposureReachesTheEventsApiAsAFlagExposureEvent()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));

            // When
            flagsmith.TrackExposureEvent("banner", "user_123", "control", metadata: new Dictionary<string, object> { ["experiment_id"] = 1 });
            await flagsmith.FlushEvents();

            // Then
            var exposure = mockHttpClient.PostedJson("/v1/events").Single()["events"].Single();
            Assert.Equal("$flag_exposure", (string)exposure["event"]);
            Assert.Equal("banner", (string)exposure["feature_name"]);
            Assert.Equal("user_123", (string)exposure["identifier"]);
            Assert.Equal("control", (string)exposure["value"]);
            Assert.Equal(JTokenType.Null, exposure["traits"].Type);
            Assert.Equal(1, (int)exposure["metadata"]["experiment_id"]);
        }

        [Fact]
        public async Task TestRepeatedExposureIsOnlySentOncePerBatch()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));

            // When
            flagsmith.TrackExposureEvent("banner", "user_123", "control");
            flagsmith.TrackExposureEvent("banner", "user_123", "control");
            flagsmith.TrackExposureEvent("banner", "user_123", "treatment");
            await flagsmith.FlushEvents();
            flagsmith.TrackExposureEvent("banner", "user_123", "control");
            await flagsmith.FlushEvents();

            // Then
            var batches = mockHttpClient.PostedJson("/v1/events");
            Assert.Equal(new[] { "control", "treatment" }, batches[0]["events"].Select(exposure => (string)exposure["value"]));
            Assert.Equal(new[] { "control" }, batches[1]["events"].Select(exposure => (string)exposure["value"]));
        }

        [Fact]
        public async Task TestExposureWithoutAnIdentityIsNotSent()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));

            // When
            flagsmith.TrackExposureEvent("banner", "", "control");
            await flagsmith.FlushEvents();

            // Then
            Assert.Empty(mockHttpClient.PostedJson("/v1/events"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("$flag_exposure")]
        [InlineData("$checkout")]
        public void TestApplicationEventNamesCannotBeEmptyOrReserved(string eventName)
        {
            // Given
            var flagsmith = new FlagsmithClient(EventsConfiguration(MockFlagsmith()));

            // When / Then
            Assert.Throws<ArgumentException>(() => flagsmith.TrackEvent(eventName, "user_123"));
        }

        [Fact]
        public async Task TestTrackingRequiresEventsToBeEnabled()
        {
            // Given
            var flagsmith = new FlagsmithClient(new FlagsmithConfiguration { EnvironmentKey = Fixtures.ApiKey, HttpClient = MockFlagsmith().Object });

            // When / Then
            Assert.Throws<FlagsmithClientError>(() => flagsmith.TrackEvent("checkout_completed", "user_123"));
            Assert.Throws<FlagsmithClientError>(() => flagsmith.TrackExposureEvent("banner", "user_123", "control"));
            await Assert.ThrowsAsync<FlagsmithClientError>(() => flagsmith.GetExperimentFlag("banner", "user_123"));
            await flagsmith.FlushEvents();
        }

        [Fact]
        public void TestEventsCannotBeSentInOfflineMode()
        {
            // Given
            var config = new FlagsmithConfiguration
            {
                OfflineMode = true,
                OfflineHandler = new OfflineHandler.LocalFileHandler("../../../data/offline-environment.json"),
                EnableEvents = true,
            };

            // When / Then
            var exception = Assert.Throws<Exception>(() => new FlagsmithClient(config));
            Assert.Equal("ValueError: Cannot send events in offline mode.", exception.Message);
        }

        [Fact]
        public void TestEventsAreSentAsSoonAsTheBufferIsFull()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var config = EventsConfiguration(mockHttpClient);
            config.EventsMaxBufferItems = 2;
            var flagsmith = new FlagsmithClient(config);

            // When
            flagsmith.TrackEvent("signed_up", "user_123");
            var sentBeforeFull = mockHttpClient.PostedJson("/v1/events").Count;
            flagsmith.TrackEvent("checkout_completed", "user_123");

            // Then
            Assert.Equal(0, sentBeforeFull);
            Assert.Equal(2, mockHttpClient.PostedJson("/v1/events").Single()["events"].Count());
        }

        [Fact]
        public async Task TestEventsAreSentOnTheFlushInterval()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var config = EventsConfiguration(mockHttpClient);
            config.EventsFlushInterval = TimeSpan.FromMilliseconds(50);
            var flagsmith = new FlagsmithClient(config);

            // When
            flagsmith.TrackEvent("checkout_completed", "user_123");
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (mockHttpClient.PostedJson("/v1/events").Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            // Then
            Assert.Equal("checkout_completed", (string)mockHttpClient.PostedJson("/v1/events").Single()["events"].Single()["event"]);
        }

        [Fact]
        public async Task TestFlushWaitsForEventsAlreadyBeingSent()
        {
            // Given
            var eventsResponse = new TaskCompletionSource<HttpResponseMessage>();
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).Returns(eventsResponse.Task);
            var config = EventsConfiguration(mockHttpClient);
            config.EventsMaxBufferItems = 1;
            var flagsmith = new FlagsmithClient(config);
            flagsmith.TrackEvent("checkout_completed", "user_123");

            // When
            var flush = flagsmith.FlushEvents();

            // Then
            Assert.Single(mockHttpClient.PostedJson("/v1/events"));
            Assert.False(flush.IsCompleted);
            eventsResponse.SetResult(new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted });
            await flush;
        }

        [Fact]
        public async Task TestEventsThatCannotBeSerialisedAreDropped()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            var metadata = new Dictionary<string, object>();
            metadata["self"] = metadata;
            flagsmith.TrackEvent("checkout_completed", "user_123", metadata: metadata);

            // When
            await flagsmith.FlushEvents();

            // Then
            Assert.Empty(mockHttpClient.PostedJson("/v1/events"));
        }

        [Fact]
        public void TestEventsFlushIntervalMustBePositive()
        {
            // Given
            var config = EventsConfiguration(MockFlagsmith());
            config.EventsFlushInterval = TimeSpan.Zero;

            // When / Then
            Assert.Throws<ArgumentOutOfRangeException>(() => new FlagsmithClient(config));
        }

        // Answers the events API's requests with each of the given failures in turn, then accepts them:
        // a status code, an exception to throw, or null for a request left unanswered until it is cancelled.
        private static Mock<HttpClient> MockEventsApi(params object[] failures)
        {
            var queued = new ConcurrentQueue<object>(failures);
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    if (!queued.TryDequeue(out var failure))
                        return new HttpResponseMessage(HttpStatusCode.Accepted);
                    if (failure is Exception exception)
                        throw exception;
                    if (failure is HttpStatusCode status)
                        return new HttpResponseMessage(status);
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    throw new InvalidOperationException("Unreachable");
                });
            return mockHttpClient;
        }

        private static string[] SentEvents(Mock<HttpClient> mockHttpClient) =>
            mockHttpClient.PostedJson("/v1/events").SelectMany(batch => batch["events"].Select(e => (string)e["event"])).ToArray();

        [Fact]
        public async Task TestBatchIsRetriedUntilTheEventsApiAcceptsIt()
        {
            // Given
            var mockHttpClient = MockEventsApi(HttpStatusCode.ServiceUnavailable, new HttpRequestException("Connection reset"));
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout", "user_123");

            // When
            await flagsmith.FlushEvents();
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(new[] { "checkout", "checkout", "checkout" }, SentEvents(mockHttpClient));
        }

        [Fact]
        public async Task TestUnansweredRequestIsGivenUpAfterTheRequestTimeoutAndRetried()
        {
            // Given
            var mockHttpClient = MockEventsApi(new object[] { null });
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout", "user_123");
            var stopwatch = Stopwatch.StartNew();

            // When
            await flagsmith.FlushEvents();

            // Then
            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(2.9), TimeSpan.FromSeconds(5));
            Assert.Equal(new[] { "checkout", "checkout" }, SentEvents(mockHttpClient));
        }

        [Theory]
        [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.TooManyRequests, HttpStatusCode.RequestTimeout, HttpStatusCode.BadGateway)]
        public async Task TestBatchWhoseLastAttemptFailsTransientlyIsSentWithTheNextFlushAheadOfNewerEvents(params HttpStatusCode[] failures)
        {
            // Given
            var mockHttpClient = MockEventsApi(failures.Cast<object>().ToArray());
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("add_to_cart", "user_123");
            await flagsmith.FlushEvents();

            // When
            flagsmith.TrackEvent("checkout", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(new[] { "add_to_cart", "add_to_cart", "add_to_cart", "add_to_cart", "checkout" }, SentEvents(mockHttpClient));
        }

        [Fact]
        public async Task TestBatchIsKeptWhenTheEventsApiIsUnreachable()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout_completed", "user_123");

            // When
            await flagsmith.FlushEvents();
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(6, mockHttpClient.PostedJson("/v1/events").Count);
        }

        [Theory]
        [InlineData(new[] { HttpStatusCode.BadRequest }, 1)]
        [InlineData(new[] { HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError }, 3)]
        public async Task TestBatchTheEventsApiRejectsOrFailsWithA500IsDropped(HttpStatusCode[] failures, int expectedAttempts)
        {
            // Given
            var mockHttpClient = MockEventsApi(failures.Cast<object>().ToArray());
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("add_to_cart", "user_123");
            await flagsmith.FlushEvents();

            // When
            flagsmith.TrackEvent("checkout", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(Enumerable.Repeat("add_to_cart", expectedAttempts).Append("checkout"), SentEvents(mockHttpClient));
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task TestEventsApiRefusingTheEnvironmentKeyStopsEventTracking(HttpStatusCode status)
        {
            // Given
            var mockHttpClient = MockEventsApi(status);
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("add_to_cart", "user_123");
            await flagsmith.FlushEvents();

            // When
            flagsmith.TrackEvent("checkout", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(new[] { "add_to_cart" }, SentEvents(mockHttpClient));
        }

        [Fact]
        public async Task TestFullBufferDropsItsOldestEvent()
        {
            // Given
            var mockHttpClient = MockEventsApi(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);
            var config = EventsConfiguration(mockHttpClient);
            config.EventsMaxBufferItems = 3;
            var flagsmith = new FlagsmithClient(config);
            flagsmith.TrackEvent("event_1", "user_123");
            flagsmith.TrackEvent("event_2", "user_123");
            flagsmith.TrackEvent("event_3", "user_123");
            await flagsmith.FlushEvents();

            // When
            flagsmith.TrackEvent("event_4", "user_123");
            await flagsmith.FlushEvents();

            // Then
            var batches = mockHttpClient.PostedJson("/v1/events");
            Assert.Equal(4, batches.Count);
            Assert.Equal(new[] { "event_2", "event_3", "event_4" }, batches.Last()["events"].Select(e => (string)e["event"]));
        }

        [Fact]
        public async Task TestExposureEvictedFromAFullBufferIsSentWhenTrackedAgain()
        {
            // Given
            var mockHttpClient = MockEventsApi(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);
            var config = EventsConfiguration(mockHttpClient);
            config.EventsMaxBufferItems = 2;
            var flagsmith = new FlagsmithClient(config);
            flagsmith.TrackExposureEvent("banner", "user_123", "control");
            flagsmith.TrackEvent("signed_up", "user_123");
            await flagsmith.FlushEvents();
            flagsmith.TrackEvent("checkout", "user_123");

            // When
            flagsmith.TrackExposureEvent("banner", "user_123", "control");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(new[] { "checkout", "$flag_exposure" }, mockHttpClient.PostedJson("/v1/events").Last()["events"].Select(e => (string)e["event"]));
        }

        [Fact]
        public async Task TestKeptBatchFillingTheBufferWaitsForTheNextFlush()
        {
            // Given
            var mockHttpClient = MockEventsApi(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);
            var config = EventsConfiguration(mockHttpClient);
            config.EventsMaxBufferItems = 3;
            var flagsmith = new FlagsmithClient(config);
            flagsmith.TrackEvent("event_1", "user_123");
            flagsmith.TrackEvent("event_2", "user_123");
            flagsmith.TrackEvent("event_3", "user_123");
            await flagsmith.FlushEvents();

            // When
            flagsmith.TrackEvent("event_4", "user_123");
            flagsmith.TrackEvent("event_5", "user_123");
            var sentBeforeFlush = mockHttpClient.PostedJson("/v1/events").Count;
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(3, sentBeforeFlush);
            var batches = mockHttpClient.PostedJson("/v1/events");
            Assert.Equal(4, batches.Count);
            Assert.Equal(new[] { "event_3", "event_4", "event_5" }, batches.Last()["events"].Select(e => (string)e["event"]));
        }

        [Fact]
        public async Task TestClosingTheClientSendsTheBufferedEventsRetriesIncluded()
        {
            // Given
            var mockHttpClient = MockEventsApi(HttpStatusCode.ServiceUnavailable);
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout", "user_123");

            // When
            await flagsmith.Close(TimeSpan.FromSeconds(5));
            flagsmith.TrackEvent("signed_out", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(new[] { "checkout", "checkout" }, SentEvents(mockHttpClient));
        }

        [Fact]
        public async Task TestClosingTheClientReturnsWithinItsTimeoutWhenTheEventsApiDoesNotAnswer()
        {
            // Given
            var mockHttpClient = MockEventsApi(null, null, null);
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout", "user_123");
            var stopwatch = Stopwatch.StartNew();

            // When
            await flagsmith.Close(TimeSpan.FromSeconds(1));

            // Then
            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(2));
            Assert.Single(mockHttpClient.PostedJson("/v1/events"));
        }

        [Fact]
        public async Task TestIdentityInARunningExperimentIsExposedToTheVariantItWasServed()
        {
            // Given
            var mockHttpClient = MockFlagsmith(@"{
                'flags': [{
                    'feature': { 'id': 1, 'name': 'banner' },
                    'feature_state_value': 'hello treatment',
                    'enabled': true,
                    'variant': 'treatment',
                    'metadata': { 'experiment': { 'id': 7, 'name': 'banner experiment', 'in_experiment': true } }
                }]
            }");
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            var traits = new List<ITrait> { new Trait("plan", "premium") };

            // When
            var flag = (Flag)await flagsmith.GetExperimentFlag("banner", "user_123", traits);
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal("hello treatment", flag.Value);
            Assert.Equal("treatment", flag.Variant);
            Assert.Equal(7, flag.Experiment.Id);
            Assert.Equal("banner experiment", flag.Experiment.Name);
            Assert.True(flag.Experiment.InExperiment);
            var exposure = mockHttpClient.PostedJson("/v1/events").Single()["events"].Single();
            Assert.Equal("$flag_exposure", (string)exposure["event"]);
            Assert.Equal("banner", (string)exposure["feature_name"]);
            Assert.Equal("user_123", (string)exposure["identifier"]);
            Assert.Equal("treatment", (string)exposure["value"]);
            Assert.True(JToken.DeepEquals(JObject.FromObject(new { plan = "premium" }), exposure["traits"]), exposure.ToString());
            Assert.Equal(7, (int)exposure["metadata"]["experiment_id"]);
        }

        [Theory]
        [InlineData("'enabled': true")]
        [InlineData("'enabled': false, 'variant': 'treatment', 'metadata': { 'experiment': { 'id': 7, 'name': 'banner experiment', 'in_experiment': true } }")]
        [InlineData("'enabled': true, 'variant': 'treatment', 'metadata': { 'experiment': { 'id': 7, 'name': 'banner experiment', 'in_experiment': false } }")]
        [InlineData("'enabled': true, 'variant': null, 'metadata': { 'experiment': { 'id': 7, 'name': 'banner experiment', 'in_experiment': true } }")]
        public async Task TestIdentityIsNotExposedUnlessServedAnEnabledVariantOfARunningExperiment(string flagState)
        {
            // Given
            var mockHttpClient = MockFlagsmith($"{{ 'flags': [{{ 'feature': {{ 'id': 1, 'name': 'banner' }}, 'feature_state_value': 'hello', {flagState} }}] }}");
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));

            // When
            var flag = await flagsmith.GetExperimentFlag("banner", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal("hello", flag.Value);
            Assert.Empty(mockHttpClient.PostedJson("/v1/events"));
        }

        // An environment document running an experiment on "checkout", as Flagsmith serves it: the experiment's
        // rollout segment enrolls `rolloutPercentage` of identities, and serves them "treatment" at `variantPercentage`.
        private static string ExperimentEnvironmentDocument(int rolloutPercentage, int variantPercentage) => $@"{{
            'api_key': 'test_key',
            'name': 'Test Environment',
            'project': {{
                'id': 1,
                'name': 'Test project',
                'hide_disabled_flags': false,
                'organisation': {{ 'id': 1, 'name': 'Test Org', 'feature_analytics': false, 'persist_trait_data': true, 'stop_serving_flags': false }},
                'segments': [{{
                    'id': 1,
                    'name': 'experiment-7-rollout',
                    'rules': [{{
                        'type': 'ALL',
                        'rules': [],
                        'conditions': [{{ 'operator': 'PERCENTAGE_SPLIT', 'value': '{rolloutPercentage}', 'property_': '$.identity.key' }}]
                    }}],
                    'feature_states': [{CheckoutFeatureState(2, "{ 'priority': 0 }", variantPercentage, inExperiment: true)}]
                }}]
            }},
            'feature_states': [{CheckoutFeatureState(1, "null", 0, inExperiment: false)}],
            'identity_overrides': []
        }}";

        private static string CheckoutFeatureState(int id, string featureSegment, int variantPercentage, bool inExperiment) => $@"{{
            'feature': {{ 'id': 1, 'name': 'checkout', 'type': 'MULTIVARIATE' }},
            'enabled': true,
            'django_id': {id},
            'feature_segment': {featureSegment},
            'featurestate_uuid': '00000000-0000-0000-0000-00000000000{id}',
            'feature_state_value': 'control',
            'multivariate_feature_state_values': [{{
                'multivariate_feature_option': {{ 'id': 1, 'key': 'treatment', 'value': 'new checkout' }},
                'percentage_allocation': {variantPercentage},
                'id': {id},
                'mv_fs_value_uuid': '10000000-0000-0000-0000-00000000000{id}'
            }}],
            'metadata': {{ 'experiment': {{ 'id': 7, 'name': 'checkout experiment', 'in_experiment': {(inExperiment ? "true" : "false")} }} }}
        }}";

        private static FlagsmithClient LocalEvaluationClientWithEvents(Mock<HttpClient> mockHttpClient)
        {
            var config = EventsConfiguration(mockHttpClient);
            config.EnableLocalEvaluation = true;
            return new FlagsmithClient(config);
        }

        [Theory]
        [InlineData(100, "new checkout", "treatment")]
        [InlineData(0, "control", "control")]
        public async Task TestLocallyEvaluatedIdentityInARunningExperimentIsExposedToTheVariantItWasServed(int variantPercentage, string expectedValue, string expectedVariant)
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/environment-document/", new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(ExperimentEnvironmentDocument(100, variantPercentage)) } },
                { "/v1/events", new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted } },
            });
            var flagsmith = LocalEvaluationClientWithEvents(mockHttpClient);

            // When
            var flag = (Flag)await flagsmith.GetExperimentFlag("checkout", "user_123", new List<ITrait> { new Trait("plan", "premium") });
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(expectedValue, flag.Value);
            Assert.Equal(expectedVariant, flag.Variant);
            Assert.Equal(7, flag.Experiment.Id);
            Assert.True(flag.Experiment.InExperiment);
            mockHttpClient.VerifyHttpRequest(HttpMethod.Post, "/api/v1/identities/", Times.Never);
            var exposure = mockHttpClient.PostedJson("/v1/events").Single()["events"].Single();
            Assert.Equal("$flag_exposure", (string)exposure["event"]);
            Assert.Equal("checkout", (string)exposure["feature_name"]);
            Assert.Equal(expectedVariant, (string)exposure["value"]);
            Assert.Equal(7, (int)exposure["metadata"]["experiment_id"]);
        }

        [Fact]
        public async Task TestLocallyEvaluatedIdentityARunningExperimentDoesNotEnrollIsNotExposed()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpResponse(new Dictionary<string, HttpResponseMessage>
            {
                { "/api/v1/environment-document/", new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(ExperimentEnvironmentDocument(0, 100)) } },
                { "/v1/events", new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted } },
            });
            var flagsmith = LocalEvaluationClientWithEvents(mockHttpClient);

            // When
            var flag = (Flag)await flagsmith.GetExperimentFlag("checkout", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal("control", flag.Value);
            Assert.False(flag.Experiment.InExperiment);
            Assert.Empty(mockHttpClient.PostedJson("/v1/events"));
        }

        [Fact]
        public async Task TestDefaultFlagIsNotExposed()
        {
            // Given
            var mockHttpClient = MockFlagsmith();
            var config = EventsConfiguration(mockHttpClient);
            config.DefaultFlagHandler = _ => new Flag(null, true, "default");
            var flagsmith = new FlagsmithClient(config);

            // When
            var flag = await flagsmith.GetExperimentFlag("banner", "user_123");
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal("default", flag.Value);
            Assert.Empty(mockHttpClient.PostedJson("/v1/events"));
        }
    }
}
