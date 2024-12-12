using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Moq;

namespace Flagsmith.FlagsmithClientTest
{
    internal static class HttpMocker
    {
        public static ConcurrentBag<string> PayloadsSubmitted { get; set; } = new ConcurrentBag<string>();

        public static Mock<HttpClient> MockHttpResponse(HttpStatusCode statusCode, string content, bool trackPayloads)
        {
            var payloadTracker = new Action<IInvocation>(item =>
            {
                if (trackPayloads)
                {
                    var payload = new StreamReader(((item.Arguments[0] as dynamic).Content as StringContent).ReadAsStream()).ReadToEnd();

                    PayloadsSubmitted.Add(payload);
                }
            });

            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    HttpResponseMessage result = new()
                    {
                        StatusCode = statusCode,
                    };
                    if (content != null)
                    {
                        result.Content = new StringContent(content);

                    }
                    return Task.FromResult(result);
                }).Callback(payloadTracker);

            return httpClientMock;
        }
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

        public static void verifyHttpRequestWithParams(this Mock<HttpClient> mockHttpClient, HttpMethod httpMethod, string url, System.Func<Moq.Times> times, Dictionary<string, string> queryParams)
        {
            verifyHttpRequest(mockHttpClient, httpMethod, url, times, queryParams);
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
           (queryString == "" || QueryStringsMatch(req, queryString))), It.IsAny<CancellationToken>()), times);
        }

        private static bool QueryStringsMatch(HttpRequestMessage req, string expectedQueryString)
        {
            // We can't just compare the querystring directly, since .NET sometimes encodes `=`
            // as %3d and sometimes as %3D. So by parsing, we normalise and sidestep that problem.
            var query = HttpUtility.ParseQueryString(req.RequestUri.Query);

            return query.ToString() == expectedQueryString;
        }

        public static Mock<HttpClient> MockHttpResponse(Dictionary<string, HttpResponseMessage> responses)
        {
            var httpClientMock = new Mock<HttpClient>();

            httpClientMock.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns((HttpRequestMessage request, CancellationToken token) =>
                {
                    var url = request.RequestUri.PathAndQuery;

                    if (responses.TryGetValue(url, out var response))
                    {
                        return Task.FromResult(response);
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                });

            return httpClientMock;
        }
    }
}
