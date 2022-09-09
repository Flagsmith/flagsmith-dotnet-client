using Moq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Flagsmith.FlagsmithClientTest
{
    internal static class HttpClientMocker
    {
        public static Mock<HttpClient> MockHttpResponse(HttpResponseMessage httpResponseMessage)
        {
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = httpResponseMessage.StatusCode,
                    Content = httpResponseMessage.Content
                }));
            httpClientMock.Object.BaseAddress = new Uri(Fixtures.ApiUrl);
            return httpClientMock;
        }

        public static Mock<HttpClient> MockHttpThrowConnectionError()
        {
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());
            httpClientMock.Object.BaseAddress = new Uri(Fixtures.ApiUrl);
            return httpClientMock;
        }

        public static void verifyHttpRequest(this Mock<HttpClient> mockHttpClient, HttpMethod httpMethod, string url, Func<Times> times)
        {
            mockHttpClient.Verify(x => x.SendAsync(
                It.Is<HttpRequestMessage>(req => req.Method == httpMethod && req.RequestUri.ToString() == url),
                It.IsAny<CancellationToken>()),
                times);
        }
    }
}
