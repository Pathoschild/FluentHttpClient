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
        public RetryCoordinator(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
           : this(new RetryConfig(maxRetries, shouldRetry, getDelay)) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="config">The retry configuration.</param>
        public RetryCoordinator(IRetryConfig config)
        {
            this.Config = config;
        }

        /// <summary>Dispatch an HTTP request.</summary>
        /// <param name="request">The response message to validate.</param>
        /// <param name="dispatcher">A method which executes the request.</param>
        /// <returns>The final HTTP response.</returns>
        public async Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
        {
            // initial request
            HttpResponseMessage response = await dispatcher(request).ConfigureAwait(false);

            // retry if needed
            if (this.Config != null)
            {
                var attempt = 1;
                while (attempt <= this.Config.MaxRetries && this.Config.ShouldRetry(response))
                {
                    // fail on too many attempts
                    if (attempt == this.Config.MaxRetries)
                        throw new ApiException(new Response(response, request.Formatters),
                            $"The HTTP request failed, and the retry coordinator gave up after the maximum {this.Config.MaxRetries} retries");

                    // apply delay
                    TimeSpan delay = this.Config.GetDelay(attempt, response);
                    if (delay.TotalMilliseconds > 0)
                        await Task.Delay(delay).ConfigureAwait(false);

                    // retry request
                    response = await dispatcher(request).ConfigureAwait(false);
                    attempt++;
                }
            }

            // return final response
            return response;
        }
    }
}
