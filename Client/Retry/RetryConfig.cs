
using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>Configures a request retry strategy.</summary>
    public class RetryConfig : IRetryConfig
    {
        /*********
        ** Properties
        *********/
        /// <summary>A method which indicates whether a request should be retried.</summary>
        private Func<HttpResponseMessage, bool> ShouldRetryCallback { get; }

        /// <summary>A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</summary>
        private Func<int, HttpResponseMessage, TimeSpan> GetDelayCallback { get; }


        /*********
        ** Accessors
        *********/
        /// <summary>The maximum number of times to retry a request before failing.</summary>
        public int MaxRetries { get; }

        /// <summary>Whether to retry if the request times out.</summary>
        public bool RetryOnTimeout { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Initializes a new instance of the <see cref="RetryConfig"/> class.</summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="shouldRetry">A method which indicates whether a request should be retried.</param>
        /// <param name="getDelay">A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</param>
        /// <param name="retryOnTimeout">Whether to retry if the request times out.</param>
        public RetryConfig(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay, bool retryOnTimeout = true)
        {
            this.MaxRetries = maxRetries;
            this.ShouldRetryCallback = shouldRetry;
            this.GetDelayCallback = getDelay;
            this.RetryOnTimeout = retryOnTimeout;
        }

        /// <summary>Get whether a request should be retried.</summary>
        /// <param name="response">The last HTTP response received.</param>
        public bool ShouldRetry(HttpResponseMessage response)
        {
            return this.ShouldRetryCallback(response);
        }

        /// <summary>Get the time to wait until the next retry.</summary>
        /// <param name="retry">The retry index (starting at 1).</param>
        /// <param name="response">The last HTTP response received.</param>
        public TimeSpan GetDelay(int retry, HttpResponseMessage response)
        {
            return this.GetDelayCallback(retry, response);
        }
    }
}
