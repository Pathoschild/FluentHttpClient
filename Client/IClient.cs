﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Retry;

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

        /// <summary>Interceptors which can read and modify HTTP requests and responses.</summary>
        ICollection<IHttpFilter> Filters { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest SendAsync(HttpRequestMessage message);

        /// <summary>Specify the authentication that will be used with every request.</summary>
        /// <param name="scheme">The scheme to use for authorization. e.g.: "Basic", "Bearer".</param>
        /// <param name="parameter">The credentials containing the authentication information.</param>
        IClient SetAuthentication(string scheme, string parameter);

        /// <summary>Set whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions by default.</summary>
        /// <param name="enabled">Whether to raise HTTP errors as exceptions by default.</param>
        IClient SetHttpErrorAsException(bool enabled);

        /// <summary>Set the default user agent header.</summary>
        /// <param name="userAgent">The user agent header value.</param>
        IClient SetUserAgent(string userAgent);

        /// <summary>Set the default request coordinator.</summary>
        /// <param name="requestCoordinator">The request coordinator (or null to use the default behaviour).</param>
        IClient SetRequestCoordinator(IRequestCoordinator requestCoordinator);

        /// <summary>Adds a header which should be sent with each request.</summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        IClient SetHeader(string name, string value);
    }
}
