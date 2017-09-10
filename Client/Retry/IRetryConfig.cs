using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>Configures a request retry strategy.</summary>
    public interface IRetryConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The maximum number of times to retry a request before failing.</summary>
        int MaxRetries { get; }

        /// <summary>Whether to retry if the request times out.</summary>
        bool RetryOnTimeout { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether a request should be retried.</summary>
        /// <param name="response">The last HTTP response received.</param>
        bool ShouldRetry(HttpResponseMessage response);

        /// <summary>Get the time to wait until the next retry.</summary>
        /// <param name="retry">The retry index (starting at 1).</param>
        /// <param name="response">The last HTTP response received.</param>
        TimeSpan GetDelay(int retry, HttpResponseMessage response);
    }
}
