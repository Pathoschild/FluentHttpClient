using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Retry;

namespace Pathoschild.Http.Client.Internal
{
    /// <inheritdoc cref="IRequest" />
    public sealed class Request : IRequest
    {
        /*********
        ** Fields
        *********/
        /// <summary>Dispatcher that executes the request.</summary>
        private readonly Func<IRequest, Task<HttpResponseMessage>> Dispatcher;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public HttpRequestMessage Message { get; }

        /// <inheritdoc />
        public MediaTypeFormatterCollection Formatters { get; }

        /// <inheritdoc />
        public ICollection<IHttpFilter> Filters { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; private set; }

        /// <summary>The request coordinator.</summary>
        public IRequestCoordinator? RequestCoordinator { get; private set; }

        /// <inheritdoc />
        public RequestOptions Options { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="message">The underlying HTTP request message.</param>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        /// <param name="dispatcher">Executes an HTTP request.</param>
        /// <param name="filters">Middleware classes which can intercept and modify HTTP requests and responses.</param>
        public Request(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequest, Task<HttpResponseMessage>> dispatcher, ICollection<IHttpFilter> filters)
        {
            this.Message = message;
            this.Formatters = formatters;
            this.Dispatcher = dispatcher;
            this.Filters = filters;
            this.CancellationToken = CancellationToken.None;
            this.RequestCoordinator = null;
            this.Options = new RequestOptions();
        }

        /***
        ** Build request
        ***/
        /// <inheritdoc />
        public IRequest WithBody(Func<IBodyBuilder, HttpContent?> bodyBuilder)
        {
            this.Message.Content = bodyBuilder(new BodyBuilder(this));
            return this;
        }

        /// <inheritdoc />
        public IRequest WithHeader(string key, string? value)
        {
            this.Message.Headers.Add(key, value);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithAuthentication(string scheme, string parameter)
        {
            this.Message.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithArgument(string key, object? value)
        {
            this.Message.RequestUri = this.Message.RequestUri.WithArguments(this.Options.IgnoreNullArguments ?? true, new KeyValuePair<string, object?>(key, value));
            return this;
        }

        /// <inheritdoc />
        public IRequest WithArguments<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? arguments)
        {
            if (arguments == null)
                return this;

            KeyValuePair<string, object?>[] args = (
                from arg in arguments
                let key = arg.Key?.ToString()
                where !string.IsNullOrWhiteSpace(key)
                select new KeyValuePair<string, object?>(key, arg.Value)
            ).ToArray();

            this.Message.RequestUri = this.Message.RequestUri.WithArguments(this.Options.IgnoreNullArguments ?? true, args);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithArguments(object? arguments)
        {
            if (arguments == null)
                return this;

            KeyValuePair<string, object?>[] args = arguments.GetKeyValueArguments().ToArray();

            this.Message.RequestUri = this.Message.RequestUri.WithArguments(this.Options.IgnoreNullArguments ?? true, args);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithCustom(Action<HttpRequestMessage> request)
        {
            request(this.Message);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithCancellationToken(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            return this;
        }

        /// <inheritdoc />
        public IRequest WithOptions(RequestOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.Options.MergeFrom(options);

            return this;
        }

        /// <inheritdoc />
        public IRequest WithRequestCoordinator(IRequestCoordinator? requestCoordinator)
        {
            this.RequestCoordinator = requestCoordinator;
            return this;
        }

        /// <inheritdoc />
        public IRequest WithFilter(IHttpFilter filter)
        {
            this.Filters.Add(filter);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithoutFilter(IHttpFilter filter)
        {
            this.Filters.Remove(filter);
            return this;
        }

        /// <inheritdoc />
        public IRequest WithoutFilter<TFilter>()
            where TFilter : IHttpFilter
        {
            this.Filters.Remove<TFilter>();
            return this;
        }

        /***
        ** Retrieve response
        ***/
        /// <inheritdoc />
        public TaskAwaiter<IResponse> GetAwaiter()
        {
            async Task<IResponse> Waiter() => await this.Execute().ConfigureAwait(false);
            return Waiter().GetAwaiter();
        }

        /// <inheritdoc />
        public Task<IResponse> AsResponse()
        {
            return this.Execute();
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> AsMessage()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return response.Message;
        }

        /// <inheritdoc />
        public async Task<T> As<T>()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.As<T>().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<T[]> AsArray<T>()
        {
            return this.As<T[]>();
        }

        /// <inheritdoc />
        public async Task<byte[]> AsByteArray()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsByteArray().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> AsString()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsString().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Stream> AsStream()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsStream().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<JToken> AsRawJson()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsRawJson().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<JObject> AsRawJsonObject()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsRawJsonObject().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<JArray> AsRawJsonArray()
        {
            IResponse response = await this.AsResponse().ConfigureAwait(false);
            return await response.AsRawJsonArray().ConfigureAwait(false);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Execute the HTTP request and fetch the response.</summary>
        private async Task<IResponse> Execute()
        {
            // apply request filters
            foreach (IHttpFilter filter in this.Filters)
                filter.OnRequest(this);

            // execute the request
            HttpResponseMessage responseMessage = this.RequestCoordinator != null
                ? await this.RequestCoordinator.ExecuteAsync(this, this.Dispatcher).ConfigureAwait(false)
                : await this.Dispatcher(this).ConfigureAwait(false);
            IResponse response = new Response(responseMessage, this.Formatters);

            // apply response filters
            foreach (IHttpFilter filter in this.Filters)
                filter.OnResponse(response, !(this.Options.IgnoreHttpErrors ?? false));

            return response;
        }
    }
}
