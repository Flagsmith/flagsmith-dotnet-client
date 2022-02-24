﻿using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            mockHttpClient.Verify(x => x.SendAsync(It.Is<HttpRequestMessage>(req =>
          req.Method == httpMethod &&
          req.RequestUri.AbsolutePath == url), It.IsAny<CancellationToken>()), times);
        }
    }
}
