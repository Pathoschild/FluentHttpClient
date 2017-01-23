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

        /// <summary>A method which indicates whether a request should be retried.</summary>
        Func<HttpResponseMessage, bool> ShouldRetry { get; }

        /// <summary>A method which returns the time to wait until the next retry. This is passed the retry index (starting at 1) and the last HTTP response received.</summary>
        Func<int, HttpResponseMessage, TimeSpan> GetDelay { get; }
    }
}
