﻿using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client.Default
{
	/// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
	/// <typeparam name="TMessageHandler">The HTTP message handler type.</typeparam>
	public class FluentClient<TMessageHandler> : IClient<TMessageHandler>
		where TMessageHandler : HttpMessageHandler
	{
		/*********
		** Properties
		*********/
		/// <summary>Constructs implementations for the fluent client.</summary>
		public IFactory Factory { get; protected set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP client.</summary>
		public HttpClient BaseClient { get; protected set; }

		/// <summary>The underlying HTTP message handler.</summary>
		public TMessageHandler MessageHandler { get; protected set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public MediaTypeFormatterCollection Formatters { get; protected set; }
		

		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="client">The underlying HTTP client.</param>
		/// <param name="handler">The underlying HTTP message handler. This should be the same handler used by the <paramref name="client"/>.</param>
		/// <param name="baseUri">The base URI prepended to relative request URIs.</param>
		/// <param name="factory">Constructs implementations for the fluent client.</param>
		public FluentClient(HttpClient client, TMessageHandler handler, string baseUri = null, IFactory factory = null)
			: this()
		{
			this.Initialize(client, handler, baseUri, factory);
		}

		/// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest DeleteAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.SendAsync(HttpMethod.Delete, resource);
		}

		/// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest GetAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.SendAsync(HttpMethod.Get, resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest PostAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.SendAsync(HttpMethod.Post, resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest PostAsync<TBody>(string resource, TBody body)
		{
			this.AssertNotDisposed();

			return this.PostAsync(resource).WithBody(body);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest PutAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.SendAsync(HttpMethod.Put, resource);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequest PutAsync<TBody>(string resource, TBody body)
		{
			this.AssertNotDisposed();

			return this.PutAsync(resource).WithBody(body);
		}

		/// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest SendAsync(HttpMethod method, string resource)
		{
			this.AssertNotDisposed();

			Uri uri = new Uri(this.BaseClient.BaseAddress, resource);
			HttpRequestMessage message = this.Factory.GetRequestMessage(method, uri, this.Formatters);
			return this.SendAsync(message);
		}

		/// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
		/// <param name="message">The HTTP request message to send.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest SendAsync(HttpRequestMessage message)
		{
			this.AssertNotDisposed();

			return this.Factory.GetRequest(message, this.Formatters, request => this.BaseClient.SendAsync(request.Message));
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an uninitialized instance.</summary>
		protected FluentClient()
		{
			this.Factory = new Factory();
		}

		/// <summary>Initialize the client.</summary>
		/// <param name="client">The underlying HTTP client.</param>
		/// <param name="handler">The underlying HTTP message handler. This should be the same handler used by the <paramref name="client"/>.</param>
		/// <param name="baseUri">The base URI prepended to relative request URIs.</param>
		/// <param name="factory">Constructs implementations for the fluent client.</param>
		protected void Initialize(HttpClient client, TMessageHandler handler, string baseUri = null, IFactory factory = null)
		{
			this.AssertNotDisposed();

			this.MessageHandler = handler;
			this.BaseClient = client;
			this.Factory = factory ?? new Factory();
			if (baseUri != null)
				this.BaseClient.BaseAddress = new Uri(baseUri);
			this.Formatters = this.Factory.GetDefaultFormatters();
		}


		/*********
		** Dispose methods
		*********/
		/// <summary>Whether the client has been disposed.</summary>
		private bool _disposed = false;

		// Public implementation of Dispose pattern callable by consumers.
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Protected implementation of Dispose pattern.</summary>
		/// <param name="isDisposing">Set if the dispose method was explicitly called.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (_disposed)
				return;

			if (isDisposing)
			{
				this.MessageHandler.Dispose();
				this.BaseClient.Dispose();
			}

			_disposed = true;
		}

		/// <summary>Destruct the instance.</summary>
		~FluentClient()
		{
			Dispose(false);
		}

		/// <summary>Assert that the instance is not disposed.</summary>
		private void AssertNotDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(FluentClient<TMessageHandler>));
		}
	}

	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI.</summary>
	public class FluentClient : FluentClient<HttpClientHandler>, IClient
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="client">The underlying HTTP client.</param>
		/// <param name="handler">The underlying HTTP message handler. This should be the same handler used by the <paramref name="client"/>.</param>
		/// <param name="baseUri">The base URI prepended to relative request URIs.</param>
		/// <param name="factory">Constructs implementations for the fluent client.</param>
		public FluentClient(HttpClient client, HttpClientHandler handler, string baseUri = null, IFactory factory = null)
			: base(client, handler, baseUri, factory) { }

		/// <summary>Construct an instance.</summary>
		/// <param name="baseUri">The base URI prepended to relative request URIs.</param>
		/// <param name="factory">Constructs implementations for the fluent client.</param>
		public FluentClient(string baseUri, IFactory factory = null)
		{
			var handler = new HttpClientHandler();
			this.Initialize(new HttpClient(handler), handler, baseUri, factory);
		}
	}
}
