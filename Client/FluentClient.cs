using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using Pathoschild.Http.Client.Retry;

namespace Pathoschild.Http.Client
{
    /// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
    public class FluentClient : IClient
    {
        /*********
        ** Properties
        *********/
        /// <summary>Whether the instance has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>Whether to dispose the <see cref="BaseClient"/> when disposing.</summary>
        private readonly bool MustDisposeBaseClient;

        /// <summary>Whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions.</summary>
        private bool HttpErrorAsException = true;


        /*********
        ** Accessors
        *********/
        /// <summary>Interceptors which can read and modify HTTP requests and responses.</summary>
        public ICollection<IHttpFilter> Filters { get; }

        /// <summary>The underlying HTTP client.</summary>
        public HttpClient BaseClient { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; }

        /// <summary>The request coordinator.</summary>
        public IRequestCoordinator RequestCoordinator { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="proxy">The web proxy.</param>
        public FluentClient(string baseUri, IWebProxy proxy)
            : this(new Uri(baseUri), proxy) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="proxy">The web proxy.</param>
        public FluentClient(Uri baseUri, IWebProxy proxy)
            : this(baseUri, new HttpClient(new HttpClientHandler { Proxy = proxy, UseProxy = proxy != null }))
        {
            this.MustDisposeBaseClient = true;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="client">The underlying HTTP client.</param>
        public FluentClient(string baseUri, HttpClient client = null)
            : this(new Uri(baseUri), client) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="client">The underlying HTTP client.</param>
        public FluentClient(Uri baseUri, HttpClient client = null)
        {
            // initialise
            this.MustDisposeBaseClient = client == null;
            this.BaseClient = client ?? new HttpClient();
            this.Filters = new List<IHttpFilter> { new DefaultErrorFilter() };
            if (baseUri != null)
                this.BaseClient.BaseAddress = baseUri;
            this.Formatters = new MediaTypeFormatterCollection();

            // set default user agent
            Version version = typeof(FluentClient).GetTypeInfo().Assembly.GetName().Version;
            this.SetUserAgent($"FluentHttpClient/{version} (+http://github.com/Pathoschild/FluentHttpClient)");
        }

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public virtual IRequest SendAsync(HttpRequestMessage message)
        {
            this.AssertNotDisposed();

            // clone the underlying message because HttpClient doesn't normally allow re-sending
            // the same request, which would break IRequestCoordinator.
            return new Request(message, this.Formatters, request => this.BaseClient.SendAsync(request.Message.Clone(), request.CancellationToken), this.Filters.ToArray())
                .WithRequestCoordinator(this.RequestCoordinator)
                .WithHttpErrorAsException(this.HttpErrorAsException);
        }

        /// <summary>Specify the authentication that will be used with every request.</summary>
        /// <param name="scheme">The scheme to use for authorization. e.g.: "Basic", "Bearer".</param>
        /// <param name="parameter">The credentials containing the authentication information.</param>
        public IClient SetAuthentication(string scheme, string parameter)
        {
            this.BaseClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
            return this;
        }

        /// <summary>Set whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions by default.</summary>
        /// <param name="enabled">Whether to raise HTTP errors as exceptions by default.</param>
        public IClient SetHttpErrorAsException(bool enabled)
        {
            this.HttpErrorAsException = enabled;
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
        public IClient SetRequestCoordinator(IRequestCoordinator requestCoordinator)
        {
            this.RequestCoordinator = requestCoordinator;
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

        /// <summary>Destruct the instance.</summary>
        ~FluentClient()
        {
            Dispose(false);
        }
    }
}
