using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using Pathoschild.Http.Client.Retry;

namespace Pathoschild.Http.Client
{
    /// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
    public class FluentClient : IClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether the instance has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>Whether to dispose the <see cref="BaseClient"/> when disposing.</summary>
        private readonly bool MustDisposeBaseClient;

        /// <summary>The default behaviors to apply to all requests.</summary>
        private readonly IList<Func<IRequest, IRequest>> Defaults = new List<Func<IRequest, IRequest>>();

        /// <summary>Options for the fluent client.</summary>
        private readonly FluentClientOptions Options = new FluentClientOptions();


        /*********
        ** Accessors
        *********/
        /// <summary>Interceptors which can read and modify HTTP requests and responses.</summary>
        public ICollection<IHttpFilter> Filters { get; } = new List<IHttpFilter> { new DefaultErrorFilter() };

        /// <summary>The underlying HTTP client.</summary>
        public HttpClient BaseClient { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; } = new MediaTypeFormatterCollection();

        /// <summary>The request coordinator.</summary>
        public IRequestCoordinator? RequestCoordinator { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance with no base URL.</summary>
        public FluentClient()
            : this(null, GetDefaultClient(), manageBaseClient: true) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        public FluentClient(string? baseUri)
            : this(baseUri != null ? new Uri(baseUri) : null, GetDefaultClient(), manageBaseClient: true) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        public FluentClient(Uri? baseUri)
            : this(baseUri, GetDefaultClient(), manageBaseClient: true) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="proxy">The web proxy.</param>
        public FluentClient(Uri? baseUri, IWebProxy? proxy)
            : this(baseUri, new HttpClient(GetDefaultHandler(proxy)), manageBaseClient: true) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="baseClient">The underlying HTTP client.</param>
        /// <param name="manageBaseClient">Whether to dispose the <paramref name="baseClient"/> when the instance is disposed.</param>
        public FluentClient(Uri? baseUri, HttpClient? baseClient, bool manageBaseClient = false)
        {
            this.MustDisposeBaseClient = baseClient == null || manageBaseClient;
            this.BaseClient = baseClient ?? new HttpClient(GetDefaultHandler());
            if (baseUri != null)
                this.BaseClient.BaseAddress = baseUri;

            this.SetDefaultUserAgent();
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseClient">The underlying HTTP client.</param>
        /// <param name="manageBaseClient">Whether to dispose the <paramref name="baseClient"/> when the instance is disposed.</param>
        public FluentClient(HttpClient? baseClient, bool manageBaseClient = false)
            : this(null, baseClient, manageBaseClient) { }

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public virtual IRequest SendAsync(HttpRequestMessage message)
        {
            this.AssertNotDisposed();

            IRequest request = new Request(message, this.Formatters, async req => await this.SendImplAsync(req).ConfigureAwait(false), this.Filters.ToList()) // clone the underlying message because HttpClient doesn't normally allow re-sending the same request, which would break IRequestCoordinator
                .WithRequestCoordinator(this.RequestCoordinator)
                .WithOptions(this.Options.ToRequestOptions());
            foreach (Func<IRequest, IRequest> apply in this.Defaults)
                request = apply(request);
            return request;
        }

        /// <summary>Specify the authentication that will be used with every request.</summary>
        /// <param name="scheme">The scheme to use for authorization. e.g.: "Basic", "Bearer".</param>
        /// <param name="parameter">The credentials containing the authentication information.</param>
        public IClient SetAuthentication(string scheme, string parameter)
        {
            this.BaseClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
            return this;
        }

        /// <summary>Set default options for all requests.</summary>
        /// <param name="options">The options to set. (Fields set to <c>null</c> won't change the current value.)</param>
        public IClient SetOptions(FluentClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.Options.MergeFrom(options);

            return this;
        }

        /// <summary>Set the default user agent header.</summary>
        /// <param name="userAgent">The user agent header value.</param>
        public IClient SetUserAgent(string userAgent)
        {
            this.BaseClient.DefaultRequestHeaders.Remove("User-Agent");
            this.BaseClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            return this;
        }

        /// <summary>Set the default request coordinator</summary>
        /// <param name="requestCoordinator">The request coordinator.</param>
        /// <remarks>If the request coordinator is null, it will cause requests to be executed once without any retry attempts.</remarks>
        public IClient SetRequestCoordinator(IRequestCoordinator? requestCoordinator)
        {
            this.RequestCoordinator = requestCoordinator;
            return this;
        }

        /// <summary>Add a default behaviour for all subsequent HTTP requests.</summary>
        /// <param name="apply">The default behaviour to apply.</param>
        public IClient AddDefault(Func<IRequest, IRequest> apply)
        {
            this.Defaults.Add(apply);
            return this;
        }

        /// <summary>Free resources used by the client.</summary>
        public virtual void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Set the default user agent header.</summary>
        private void SetDefaultUserAgent()
        {
            Version version = typeof(FluentClient).GetTypeInfo().Assembly.GetName().Version;
            this.SetUserAgent($"FluentHttpClient/{version} (+http://github.com/Pathoschild/FluentHttpClient)");
        }

        /// <summary>Dispatch an HTTP request message and fetch the response message.</summary>
        /// <param name="request">The request to send.</param>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        protected virtual async Task<HttpResponseMessage> SendImplAsync(IRequest request)
        {
            this.AssertNotDisposed();

            // clone request (to avoid issues when resending messages)
            HttpRequestMessage requestMessage = await request.Message.CloneAsync().ConfigureAwait(false);

            // MaxSize = 0 or MaxSize = ulong.MaxValue means no limit.
            if (request.MaxSize > 0 && request.MaxSize < ulong.MaxValue)
            {
                // ensure the size of the request does not exceed the max size
                string headers = requestMessage.Headers.ToString();
                byte[] content = await requestMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                ulong requestSizeInBytes = (ulong)((headers.Length * sizeof(char)) + content.Length);

                if (requestSizeInBytes > request.MaxSize)
                {
                    throw new InvalidOperationException($"The request size ({requestSizeInBytes} bytes) exceeds the maximum size ({request.MaxSize} bytes).");
                }
            }

            // dispatch request
            return await this.BaseClient
                .SendAsync(requestMessage, request.CancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>Assert that the instance has not been disposed.</summary>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        protected void AssertNotDisposed()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(FluentClient));
        }

        /// <summary>Free resources used by the client.</summary>
        /// <param name="isDisposing">Whether the dispose method was explicitly called.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (this.IsDisposed)
                return;

            if (isDisposing && this.MustDisposeBaseClient)
                this.BaseClient.Dispose();

            this.IsDisposed = true;
        }

        /// <summary>Get a default HTTP client.</summary>
        private static HttpClient GetDefaultClient()
        {
            return new HttpClient(GetDefaultHandler());
        }

        /// <summary>Get a default HTTP client handler.</summary>
        private static HttpClientHandler GetDefaultHandler()
        {
            return new HttpClientHandler
            {
                // don't use cookie container (so we can set cookies directly in request headers)
                UseCookies = false
            };
        }

        /// <summary>Get a default HTTP client handler.</summary>
        /// <param name="proxy">The web proxy to use.</param>
        /// <remarks>Whereas <see cref="GetDefaultHandler()"/> leaves the default proxy unchanged, this method will explicitly override it (e.g. setting a null proxy will disable the default proxy).</remarks>
        private static HttpClientHandler GetDefaultHandler(IWebProxy? proxy)
        {
            var handler = FluentClient.GetDefaultHandler();
            handler.Proxy = proxy;
            handler.UseProxy = proxy != null;
            return handler;
        }

        /// <summary>Destruct the instance.</summary>
        ~FluentClient()
        {
            this.Dispose(false);
        }
    }
}
