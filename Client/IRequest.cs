using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Retry;

namespace Pathoschild.Http.Client
{
    /// <summary>Builds and dispatches an asynchronous HTTP request, and asynchronously parses the response.</summary>
    public interface IRequest
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP request message.</summary>
        HttpRequestMessage Message { get; }

        /// <summary>The optional token used to cancel async operations.</summary>
        CancellationToken CancellationToken { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        MediaTypeFormatterCollection Formatters { get; }

        /// <summary>Middleware classes which can intercept and modify HTTP requests and responses.</summary>
        ICollection<IHttpFilter> Filters { get; }


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
        /// <param name="arguments">The arguments to add.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <example><code>client.WithArguments(new[] { new KeyValuePair&lt;string, string&gt;("genre", "drama"), new KeyValuePair&lt;string, int&gt;("genre", "comedy") })</code></example>
        IRequest WithArguments<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> arguments);

        /// <summary>Add HTTP query string arguments.</summary>
        /// <param name="arguments">An anonymous object where the property names and values are used.</param>
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

        /// <summary>Add an authentication header for this request.</summary>
        /// <param name="scheme">The authentication header scheme to use for authorization (like 'basic' or 'bearer').</param>
        /// <param name="parameter">The authentication header value (e.g. the bearer token).</param>
        IRequest WithAuthentication(string scheme, string parameter);

        /// <summary>Set whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions for this request.</summary>
        /// <param name="enabled">Whether to raise HTTP errors as exceptions.</param>
        [Obsolete("Will be removed in version 4. Use `WithOptions` instead.")]
        IRequest WithHttpErrorAsException(bool enabled);

        /// <summary>Set options for this request.</summary>
        /// <param name="options">The options.</param>
        IRequest WithOptions(RequestOptions options);

        /// <summary>Set the request coordinator for this request.</summary>
        /// <param name="requestCoordinator">The request coordinator (or null to use the default behaviour).</param>
        IRequest WithRequestCoordinator(IRequestCoordinator requestCoordinator);

        /****
        ** Response shortcuts
        ****/
        /// <summary>Get an object that waits for the completion of the request. This enables support for the <c>await</c> keyword.</summary>
        /// <example>
        /// <code>await client.GetAsync("api/ideas").AsString();</code>
        /// <code>await client.PostAsync("api/ideas", idea);</code>
        /// </example>
        TaskAwaiter<IResponse> GetAwaiter();

        /// <summary>Asynchronously retrieve the HTTP response. This method exists for discoverability but isn't strictly needed; you can just await the request (like <c>await GetAsync()</c>) to get the response.</summary>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<IResponse> AsResponse();

        /// <summary>Asynchronously retrieve the HTTP response message.</summary>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<HttpResponseMessage> AsMessage();

        /// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T> As<T>();

        /// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T[]> AsArray<T>();

        /// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<byte[]> AsByteArray();

        /// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<string> AsString();

        /// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<Stream> AsStream();
    }
}
