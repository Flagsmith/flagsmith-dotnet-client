using System;
using System.Collections.Generic;
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

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, 2)]
        [InlineData(HttpStatusCode.BadRequest, 1)]
        public async Task TestEventsTheEventsApiDoesNotAcceptAreRetriedOnlyOnServerErrorsThenDropped(HttpStatusCode eventsStatus, int expectedAttempts)
        {
            // Given
            var mockHttpClient = MockFlagsmith(eventsStatus: eventsStatus);
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout_completed", "user_123");

            // When
            await flagsmith.FlushEvents();
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(expectedAttempts, mockHttpClient.PostedJson("/v1/events").Count);
        }

        [Fact]
        public async Task TestEventsAreRetriedOnceWhenTheEventsApiIsUnreachable()
        {
            // Given
            var mockHttpClient = HttpMocker.MockHttpThrowConnectionError();
            var flagsmith = new FlagsmithClient(EventsConfiguration(mockHttpClient));
            flagsmith.TrackEvent("checkout_completed", "user_123");

            // When
            await flagsmith.FlushEvents();

            // Then
            Assert.Equal(2, mockHttpClient.PostedJson("/v1/events").Count);
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
