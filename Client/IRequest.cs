using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Pathoschild.Http.Client
{
    /// <summary>Builds and dispatches an asynchronous HTTP request, and asynchronously parses the response.</summary>
    public interface IRequest : IResponse
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP request message.</summary>
        HttpRequestMessage Message { get; }

        /// <summary>The optional token used to cancel async operations.</summary>
        CancellationToken CancellationToken { get; }


        /*********
        ** Methods
        *********/
        /***
        ** Build request
        ***/
        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The formatted HTTP body content.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithBodyContent(HttpContent body);

        /// <summary>Set an HTTP header.</summary>
        /// <param name="key">The key of the HTTP header.</param>
        /// <param name="value">The value of the HTTP header.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithHeader(string key, string value);

        /// <summary>Add an HTTP query string argument.</summary>
        /// <param name="key">The key of the query argument.</param>
        /// <param name="value">The value of the query argument.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithArgument(string key, object value);

        /// <summary>Add HTTP query string arguments.</summary>
        /// <param name="arguments">The key=>value pairs in the query string. If this is a dictionary, the keys and values are used. Otherwise, the property names and values are used.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <example><code>client.WithArguments(new { id = 14, name = "Joe" })</code></example>
        IRequest WithArguments(object arguments);

        /// <summary>Customize the underlying HTTP request message.</summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithCustom(Action<HttpRequestMessage> request);

        /// <summary>Specify the token that can be used to cancel the async operation.</summary>
        /// <param name="cancellationToken">The cancellationtoken.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithCancellationToken(CancellationToken cancellationToken);

        /// <summary>Get an object that waits for the completion of the request. This enables support for the <c>await</c> keyword.</summary>
        /// <example>
        /// <code>await client.GetAsync("api/ideas").AsString();</code>
        /// <code>await client.PostAsync("api/ideas", idea);</code>
        /// </example>
        TaskAwaiter<IResponse> GetAwaiter();

        /// <summary>Specify the authentication for this request.</summary>
        /// <param name="scheme">The scheme to use for authorization. e.g.: "Basic", "Bearer".</param>
        /// <param name="parameter">The credentials containing the authentication information.</param>
        IRequest WithAuthentication(string scheme, string parameter);
    }
}