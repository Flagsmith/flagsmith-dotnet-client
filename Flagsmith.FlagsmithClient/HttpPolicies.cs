using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Polly.Retry;
using System.Net;
using Polly;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Flagsmith
{
    internal static class HttpPolicies
    {
        private static HttpStatusCode[] httpStatusCodesWorthRetrying = {
                   HttpStatusCode.RequestTimeout, // 408
                   HttpStatusCode.InternalServerError, // 500
                   HttpStatusCode.BadGateway, // 502
                   HttpStatusCode.ServiceUnavailable, // 503
                   HttpStatusCode.GatewayTimeout // 504
                };
        public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyAwaitable(int? retries)
        => Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(retries ?? 3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(0.5, retryAttempt)));
    }
}
