﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Extensibility;

namespace Pathoschild.Http.Client.Internal
{
    /// <summary>Builds and dispatches an asynchronous HTTP request, and asynchronously parses the response.</summary>
    public sealed class Request : IRequest
    {
        /*********
        ** Properties
        *********/
        /// <summary>Middleware classes which can intercept and modify HTTP requests and responses.</summary>
        private readonly IHttpFilter[] Filters;

        /// <summary>Executes the current HTTP request.</summary>
        private readonly Lazy<Task<HttpResponseMessage>> Dispatch;


        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP request message.</summary>
        public HttpRequestMessage Message { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="message">The underlying HTTP request message.</param>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        /// <param name="dispatcher">Executes an HTTP request.</param>
        /// <param name="filters">Middleware classes which can intercept and modify HTTP requests and responses.</param>
        public Request(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequest, Task<HttpResponseMessage>> dispatcher, IHttpFilter[] filters)
        {
            this.Message = message;
            this.Formatters = formatters;
            this.Dispatch = new Lazy<Task<HttpResponseMessage>>(() => dispatcher(this));
            this.Filters = filters;
        }

        /***
        ** Build request
        ***/
        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Client.IRequest.Formatters"/>).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        public IRequest WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
        {
            MediaTypeFormatter formatter = Factory.GetFormatter(this.Formatters, contentType);
            string mediaType = contentType != null ? contentType.MediaType : null;
            return this.WithBody(body, formatter, mediaType);
        }

        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="formatter">The media type formatter with which to format the request body format.</param>
        /// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public IRequest WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null)
        {
            return this.WithBodyContent(new ObjectContent<T>(body, formatter, mediaType));
        }

        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="body">The formatted HTTP body content.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public IRequest WithBodyContent(HttpContent body)
        {
            this.Message.Content = body;
            return this;
        }

        /// <summary>Set an HTTP header.</summary>
        /// <param name="key">The key of the HTTP header.</param>
        /// <param name="value">The value of the HTTP header.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public IRequest WithHeader(string key, string value)
        {
            this.Message.Headers.Add(key, value);
            return this;
        }

        /// <summary>Add an HTTP query string argument.</summary>
        /// <param name="key">The key of the query argument.</param>
        /// <param name="value">The value of the query argument.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public IRequest WithArgument(string key, object value)
        {
            this.Message.RequestUri = this.Message.RequestUri.WithArguments(new KeyValuePair<string, object>(key, value));
            return this;
        }

        /// <summary>Add HTTP query string arguments.</summary>
        /// <param name="arguments">The key=>value pairs in the query string. If this is a dictionary, the keys and values are used. Otherwise, the property names and values are used.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <example><code>client.WithArguments(new { id = 14, name = "Joe" })</code></example>
        public IRequest WithArguments(object arguments)
        {
            this.Message.RequestUri = this.Message.RequestUri.WithArguments(this.GetArguments(arguments).ToArray());
            return this;
        }

        /// <summary>Customize the underlying HTTP request message.</summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public IRequest WithCustom(Action<HttpRequestMessage> request)
        {
            request(this.Message);
            return this;
        }

        /// <summary>Get an object that waits for the completion of the request. This enables support for the <c>await</c> keyword.</summary>
        /// <example>
        /// <code>await client.PostAsync("api/ideas", idea);</code>
        /// <code>await client.GetAsync("api/ideas").AsString();</code>
        /// </example>
        public TaskAwaiter<IResponse> GetAwaiter()
        {
            Func<Task<IResponse>> waiter = async () =>
            {
                await this.AsMessage();
                return this;
            };
            return waiter().GetAwaiter();
        }

        /***
        ** Retrieve response
        ***/
        /// <summary>Asynchronously retrieve the HTTP response.</summary>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<HttpResponseMessage> AsMessage()
        {
            return await this.GetResponse(this.Dispatch.Value).ConfigureAwait(false);
        }

        /// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<T> As<T>()
        {
            HttpResponseMessage message = await this.AsMessage().ConfigureAwait(false);
            return await message.Content.ReadAsAsync<T>(this.Formatters).ConfigureAwait(false);
        }

        /// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public Task<List<T>> AsList<T>()
        {
            return this.As<List<T>>();
        }

        /// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<byte[]> AsByteArray()
        {
            HttpResponseMessage message = await this.AsMessage().ConfigureAwait(false);
            return await message.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<string> AsString()
        {
            HttpResponseMessage message = await this.AsMessage().ConfigureAwait(false);
            return await message.Content.ReadAsStringAsync(null).ConfigureAwait(false);
        }

        /// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<Stream> AsStream()
        {
            HttpResponseMessage message = await this.AsMessage().ConfigureAwait(false);
            Stream stream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
            stream.Position = 0;
            return stream;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Validate the HTTP response and raise any errors in the response as exceptions.</summary>
        /// <param name="request">The response message to validate.</param>
        private async Task<HttpResponseMessage> GetResponse(Task<HttpResponseMessage> request)
        {
            foreach (IHttpFilter filter in this.Filters)
                filter.OnRequest(this, this.Message);
            HttpResponseMessage response = await request.ConfigureAwait(false);
            foreach (IHttpFilter filter in this.Filters)
                filter.OnResponse(this, response);
            return response;
        }

        /// <summary>Get the key=>value pairs represented by a dictionary or anonymous object.</summary>
        /// <param name="arguments">The key=>value pairs in the query argument. If this is a dictionary, the keys and values are used. Otherwise, the property names and values are used.</param>
        private IDictionary<string, object> GetArguments(object arguments)
        {
            // null
            if (arguments == null)
                return new Dictionary<string, object>();

            // generic dictionary
            if (arguments is IDictionary<string, object>)
                return (IDictionary<string, object>)arguments;

            // dictionary
            if (arguments is IDictionary)
            {
                IDictionary<string, object> dict = new Dictionary<string, object>();
                IDictionary argDict = (IDictionary)arguments;
                foreach (var key in argDict.Keys)
                    dict.Add(key.ToString(), argDict[key]);
                return dict;
            }

            // object
            return arguments.GetType()
                .GetRuntimeProperties()
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p.GetValue(arguments));
        }
    }
}
