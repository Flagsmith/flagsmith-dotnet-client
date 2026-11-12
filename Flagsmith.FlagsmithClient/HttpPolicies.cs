using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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

        public static async Task<HttpResponseMessage> ExecuteWithRetryAsync(int? retries, Func<Task<HttpResponseMessage>> action)
        {
            var retryCount = retries ?? 3;
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retries), retryCount, "Value must be greater than or equal to zero.");
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    var response = await action().ConfigureAwait(false);
                    if (attempt > retryCount || !httpStatusCodesWorthRetrying.Contains(response.StatusCode))
                    {
                        return response;
                    }
                }
                catch (HttpRequestException) when (attempt <= retryCount)
                {
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(0.5, attempt))).ConfigureAwait(false);
            }
        }
    }
}
