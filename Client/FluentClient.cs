using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using System.Net.Http.Headers;
using System.Linq;

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


        /*********
        ** Accessors
        *********/
        /// <summary>Interceptors which can read and modify HTTP requests and responses.</summary>
        public ICollection<IHttpFilter> Filters { get; }

        /// <summary>The underlying HTTP client.</summary>
        public HttpClient BaseClient { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; protected set; }

        /// <summary>The retry strategy.</summary>
        public IRetryStrategy RetryStrategy { get; private set; }


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
            return new Request(message, this.Formatters, request => this.BaseClient.SendAsync(request.Message), this.Filters.ToArray(), this.RetryStrategy);
        }

        /// <summary>Specify the authentication that will be used with every request.</summary>
        /// <param name="scheme">The scheme to use for authorization. e.g.: "Basic", "Bearer".</param>
        /// <param name="parameter">The credentials containing the authentication information.</param>
        public IClient SetAuthentication(string scheme, string parameter)
        {
            this.BaseClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
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

        /// <summary>Set the default retry strategy.</summary>
        /// <param name="retryStrategy">The retry strategy.</param>
        public IClient SetRetryStrategy(IRetryStrategy retryStrategy)
        {
            this.RetryStrategy = retryStrategy;
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
