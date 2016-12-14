using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;

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

        private readonly bool _mustDisposeHttpClient = false;


        /*********
        ** Accessors
        *********/
        /// <summary>Interceptors which can read and modify HTTP requests and responses.</summary>
        public List<IHttpFilter> Filters { get; private set; }

        /// <summary>The underlying HTTP client.</summary>
        public HttpClient BaseClient { get; private set; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; protected set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        public FluentClient(string baseUri)
            : this(baseUri, null) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="client">The underlying HTTP client.</param>
        public FluentClient(string baseUri, HttpClient client = null)
        {
            _mustDisposeHttpClient = client == null;
            this.BaseClient = client ?? new HttpClient();
            this.Filters = new List<IHttpFilter> { new DefaultErrorFilter() };
            if (baseUri != null)
                this.BaseClient.BaseAddress = new Uri(baseUri);
            this.Formatters = new MediaTypeFormatterCollection();
        }

        /// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest DeleteAsync(string resource)
        {
            return this.SendAsync(HttpMethod.Delete, resource);
        }

        /// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest GetAsync(string resource)
        {
            return this.SendAsync(HttpMethod.Get, resource);
        }

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest PostAsync(string resource)
        {
            return this.SendAsync(HttpMethod.Post, resource);
        }

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest PostAsync<TBody>(string resource, TBody body)
        {
            return this.PostAsync(resource).WithBody(body);
        }

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest PutAsync(string resource)
        {
            return this.SendAsync(HttpMethod.Put, resource);
        }

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public IRequest PutAsync<TBody>(string resource, TBody body)
        {
            return this.PutAsync(resource).WithBody(body);
        }

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public virtual IRequest SendAsync(HttpMethod method, string resource)
        {
            this.AssertNotDisposed();

            Uri uri = new Uri(this.BaseClient.BaseAddress, resource);
            HttpRequestMessage message = Factory.GetRequestMessage(method, uri, this.Formatters);
            return this.SendAsync(message);
        }

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public virtual IRequest SendAsync(HttpRequestMessage message)
        {
            this.AssertNotDisposed();
            return new Request(message, this.Formatters, request => this.BaseClient.SendAsync(request.Message), this.Filters.ToArray());
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

            if (isDisposing && _mustDisposeHttpClient)
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
