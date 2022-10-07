using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Internal;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>A request coordinator which retries failed requests with a delay between each attempt.</summary>
    public class RetryCoordinator : IRequestCoordinator
    {
        /*********
        ** Fields
        *********/
        /// <summary>The retry configurations to apply.</summary>
        private readonly IRetryConfig[] Configs;

        /// <summary>The status code representing a request timeout.</summary>
        /// <remarks>HTTP 598 Network Read Timeout is the closest match, though it's non-standard so there's no <see cref="HttpStatusCode"/> constant. This is needed to avoid passing <c>null</c> into <see cref="IRetryConfig.ShouldRetry"/>, which isn't intuitive and would cause errors.</remarks>
        private const HttpStatusCode TimeoutStatusCode = (HttpStatusCode)589;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="shouldRetry">A method which returns whether a request should be retried.</param>
        /// <param name="intervals">The intervals between each retry attempt.</param>
        public RetryCoordinator(Func<HttpResponseMessage, bool> shouldRetry, params TimeSpan[] intervals)
            : this(new RetryConfig(intervals.Length, shouldRetry, (attempts, _) => intervals[attempts - 1])) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="maxRetries">The maximum number of times to retry a request before failing.</param>
        /// <param name="shouldRetry">A method which returns whether a request should be retried.</param>
        /// <param name="getDelay">A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</param>
        public RetryCoordinator(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
           : this(new RetryConfig(maxRetries, shouldRetry, getDelay)) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="config">The retry configuration to apply.</param>
        public RetryCoordinator(IRetryConfig? config)
            : this(new[] { config }) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="configs">The retry configurations to apply. Each config will be given the opportunity to retry a request.</param>
        public RetryCoordinator(IEnumerable<IRetryConfig?>? configs)
        {
            this.Configs = configs
                ?.Where(config => config != null)
                .Select(config => config!)
                .ToArray() ??
#if NET452
                    LegacyShims.EmptyArray<IRetryConfig>();
#else
                    Array.Empty<IRetryConfig>();
#endif
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
        {
            int attempt = 0;
            while (true)
            {
                // dispatch request
                attempt++;
                HttpResponseMessage response;
                try
                {
                    response = await dispatcher(request).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (!request.CancellationToken.IsCancellationRequested)
                {
                    response = request.Message.CreateResponse(RetryCoordinator.TimeoutStatusCode);
                }

                // find the applicable retry configuration
                IRetryConfig? retryConfig = null;
                foreach (var config in this.Configs)
                {
                    if (config.ShouldRetry(response))
                    {
                        retryConfig = config;
                        break;
                    }
                }

                // exit if we can't retry
                if (retryConfig == null)
                    return response;

                // throw exception if we've exceeded max retries
                int maxAttempt = 1 + retryConfig.MaxRetries;
                if (attempt >= maxAttempt)
                    throw new ApiException(new Response(response, request.Formatters), $"The HTTP request {(response != null ? "failed" : "timed out")}, and the retry coordinator gave up after the maximum {retryConfig.MaxRetries} retries");

                // set up retry
                TimeSpan delay = retryConfig.GetDelay(attempt, response);
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }
}
