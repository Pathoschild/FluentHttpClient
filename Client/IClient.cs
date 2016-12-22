﻿using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client
{
    /// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
    public interface IClient : IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP client.</summary>
        HttpClient BaseClient { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        MediaTypeFormatterCollection Formatters { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest SendAsync(HttpRequestMessage message);

        /// <summary>Set the default user agent header.</summary>
        /// <param name="userAgent">The user agent header value.</param>
        void SetUserAgent(string userAgent);
    }
}
