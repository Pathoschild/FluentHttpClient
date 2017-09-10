using System;
using System.Net.Http;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Internal;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>A request coordinator which retries failed requests with a delay between each attempt.</summary>
    public class RetryCoordinator : IRequestCoordinator
    {
        /*********
        ** Properties
        *********/
        /// <summary>The retry configuration.</summary>
        private readonly IRetryConfig Config;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="shouldRetry">A method which returns whether a request should be retried.</param>
        /// <param name="intervals">The intervals between each retry attempt.</param>
        public RetryCoordinator(Func<HttpResponseMessage, bool> shouldRetry, params TimeSpan[] intervals)
            : this(new RetryConfig(intervals.Length, shouldRetry, (attempts, response) => intervals[attempts - 1])) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="maxRetries">The maximum number of times to retry a request before failing.</param>
        /// <param name="shouldRetry">A method which returns whether a request should be retried.</param>
        /// <param name="getDelay">A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</param>
        /// <param name="retryOnTimeout">Whether to retry if the request times out.</param>
        public RetryCoordinator(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay, bool retryOnTimeout = true)
           : this(new RetryConfig(maxRetries, shouldRetry, getDelay, retryOnTimeout)) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="config">The retry configuration.</param>
        public RetryCoordinator(IRetryConfig config)
        {
            this.Config = config ?? RetryConfig.None();
        }

        /// <summary>Dispatch an HTTP request.</summary>
        /// <param name="request">The response message to validate.</param>
        /// <param name="dispatcher">A method which executes the request.</param>
        /// <returns>The final HTTP response.</returns>
        public async Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
        {
            int attempt = 0;
            int maxAttempt = 1 + this.Config.MaxRetries;
            while (true)
            {
                // dispatch request
                attempt++;
                HttpResponseMessage response;
                try
                {
                    response = await dispatcher(request).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (this.Config.RetryOnTimeout && attempt < maxAttempt) // timeout
                {
                    response = null;
                }

                // exit if done
                bool shouldRetry = response != null ? this.Config.ShouldRetry(response) : this.Config.RetryOnTimeout;
                if (!shouldRetry)
                    return response;
                if (attempt >= maxAttempt)
                    throw new ApiException(new Response(response, request.Formatters), $"The HTTP request {(response != null ? "failed" : "timed out")}, and the retry coordinator gave up after the maximum {this.Config.MaxRetries} retries");

                // set up retry
                TimeSpan delay = this.Config.GetDelay(attempt, response);
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }
}
