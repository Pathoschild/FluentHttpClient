
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
        public int MaxRetries { get; private set; }

        /// <summary>Get whether to retry a request.</summary>
        public Func<HttpResponseMessage, bool> ShouldRetry { get; private set; }

        /// <summary>Get the delay before the next retry.</summary>
        public Func<int, HttpResponseMessage, TimeSpan> GetDelay { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Initializes a new instance of the <see cref="RetryConfig"/> class.</summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="shouldRetry">A lambda which indicates if a request should be retried.</param>
        /// <param name="getDelay">A lambda which returns the delay before the next attempt.</param>
        public RetryConfig(int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
        {
            this.MaxRetries = maxRetries;
            this.ShouldRetry = shouldRetry;
            this.GetDelay = getDelay;
        }
    }
}
