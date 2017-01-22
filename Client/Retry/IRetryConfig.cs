using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>Configures a request retry strategy.</summary>
    public interface IRetryConfig
    {
        /// <summary>The maximum number of times to retry a request before failing.</summary>
        int MaxRetries { get; }

        /// <summary>Get whether to retry a request.</summary>
        Func<HttpResponseMessage, bool> ShouldRetry { get; }

        /// <summary>Get the delay before the next retry.</summary>
        Func<int, HttpResponseMessage, TimeSpan> GetDelay { get; }
    }
}
