using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Pathoschild.Http.Client
{
    /// <summary>Builds and dispatches an asynchronous HTTP request, and asynchronously parses the response.</summary>
    public interface IRequest : IResponse
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP request message.</summary>
        HttpRequestMessage Message { get; set; }


        /*********
        ** Methods
        *********/
        /***
        ** Build request
        ***/
        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Client.IRequest.Formatters"/>).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        IRequest WithBody<T>(T body, MediaTypeHeaderValue contentType = null);

        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="formatter">The media type formatter with which to format the request body format.</param>
        /// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        IRequest WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null);

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

        /// <summary>Get an object that waits for the completion of the request. This enables support for the <c>await</c> keyword.</summary>
        /// <example>
        /// <code>await client.GetAsync("api/ideas").AsString();</code>
        /// <code>await client.PostAsync("api/ideas", idea);</code>
        /// </example>
        TaskAwaiter<IResponse> GetAwaiter();

        /// <summary>Create a copy of the current request message.</summary>
        /// <remarks>This lets you create a new request with different parameters in edge cases such as batch queries. Normally the underlying client will not allow you to dispatch the same request message.</remarks>
        IRequest Clone();

        /***
        ** Synchronize
        ***/
        /// <summary>Block the current thread until the asynchronous request completes. This method should only be called if you can't <c>await</c> instead, and may cause thread deadlocks in some circumstances (see https://github.com/Pathoschild/Pathoschild.FluentHttpClient#synchronous-use ).</summary>
        /// <exception cref="AggregateException">The HTTP response returned a non-success <see cref="HttpStatusCode"/> and <see cref="ThrowError"/> is <c>true</c>.</exception>
        void Wait();
    }
}