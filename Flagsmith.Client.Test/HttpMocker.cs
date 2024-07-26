using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Flagsmith.FlagsmithClientTest
{
    internal static class HttpMocker
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
            return httpClientMock;
        }
        public static Mock<HttpClient> MockHttpThrowConnectionError()
        {
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());
            return httpClientMock;
        }

        public static void verifyHttpRequest(this Mock<HttpClient> mockHttpClient, HttpMethod httpMethod, string url, System.Func<Moq.Times> times)
        {
            verifyHttpRequest(mockHttpClient, httpMethod, url, times, null);
        }

        public static void verifyHttpRequest(this Mock<HttpClient> mockHttpClient, HttpMethod httpMethod, string url, System.Func<Moq.Times> times, string expectedBodyJson = null)
        {
            verifyHttpRequest(mockHttpClient, httpMethod, url, times, null, expectedBodyJson);
        }

        public static void verifyHttpRequest(this Mock<HttpClient> mockHttpClient, HttpMethod httpMethod, string url, System.Func<Moq.Times> times, Dictionary<string, string> queryParams, string expectedBodyJson = null)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (queryParams != null)
            {
                foreach (KeyValuePair<string, string> entry in queryParams)
                {
                    query[entry.Key] = entry.Value;
                }
            }
            string queryString = query.ToString();

            mockHttpClient.Verify(x => x.SendAsync(It.Is<HttpRequestMessage>(req =>
           req.Method == httpMethod &&
           req.RequestUri.AbsolutePath == url &&
           ((req.Content != null && req.Content.ReadAsStringAsync().Result == expectedBodyJson) || (expectedBodyJson == null)) &&
           (queryString == "" || req.RequestUri.Query.Equals($"?{queryString}"))), It.IsAny<CancellationToken>()), times);
        }
    }
}
