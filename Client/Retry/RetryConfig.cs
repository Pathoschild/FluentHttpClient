
using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>Configures a request retry strategy.</summary>
    public class RetryConfig : IRetryConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The maximum number of times to retry a request before failing.</summary>
        public int MaxRetries { get; }

        /// <summary>A method which indicates whether a request should be retried.</summary>
        public Func<HttpResponseMessage, bool> ShouldRetry { get; }

        /// <summary>A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</summary>
        public Func<int, HttpResponseMessage, TimeSpan> GetDelay { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Initializes a new instance of the <see cref="RetryConfig"/> class.</summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="shouldRetry">A method which indicates whether a request should be retried.</param>
        /// <param name="getDelay">A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</param>
        public RetryConfig(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
        {
            this.MaxRetries = maxRetries;
            this.ShouldRetry = shouldRetry;
            this.GetDelay = getDelay;
        }
    }
}
