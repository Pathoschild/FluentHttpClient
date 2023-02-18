using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Formatting;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Retry;

namespace Pathoschild.Http.Client
{
    /// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "This is a public API.")]
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

        /// <summary>The request coordinator.</summary>
        IRequestCoordinator? RequestCoordinator { get; }


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

        /// <summary>Set default options for all requests.</summary>
        /// <param name="options">The options to set. (Fields set to <c>null</c> won't change the current value.)</param>
        IClient SetOptions(FluentClientOptions options);

        /// <summary>Set the default user agent header.</summary>
        /// <param name="userAgent">The user agent header value.</param>
        IClient SetUserAgent(string userAgent);

        /// <summary>Set the default request coordinator.</summary>
        /// <param name="requestCoordinator">The request coordinator (or null to use the default behaviour).</param>
        IClient SetRequestCoordinator(IRequestCoordinator? requestCoordinator);

        /// <summary>Add a default behaviour for all subsequent HTTP requests.</summary>
        /// <param name="apply">The default behaviour to apply.</param>
        IClient AddDefault(Func<IRequest, IRequest> apply);
    }
}
