using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client.Default
{
	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI.</summary>
	/// <typeparam name="TMessageHandler">The HTTP message handler type.</typeparam>
	public class FluentClient<TMessageHandler> : IClient<TMessageHandler>
		where TMessageHandler : HttpMessageHandler
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP client.</summary>
		public HttpClient BaseClient { get; protected set; }

		/// <summary>The underlying HTTP message handler.</summary>
		public TMessageHandler MessageHandler { get; protected set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public MediaTypeFormatterCollection Formatters { get; protected set; }

		/// <summary>Constructs implementations for the fluent client.</summary>
		public IFactory Factory { get; protected set; }



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
		public IRequestBuilder Delete(string resource)
		{
			return this.Send(HttpMethod.Delete, resource);
		}

		/// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequestBuilder Get(string resource)
		{
			return this.Send(HttpMethod.Get, resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequestBuilder Post(string resource)
		{
			return this.Send(HttpMethod.Post, resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequestBuilder Post<TBody>(string resource, TBody body)
		{
			return this.Post(resource).WithBody(body);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequestBuilder Put(string resource)
		{
			return this.Send(HttpMethod.Put, resource);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public IRequestBuilder Put<TBody>(string resource, TBody body)
		{
			return this.Put(resource).WithBody(body);
		}

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequestBuilder Send(HttpMethod method, string resource)
		{
			Uri uri = new Uri(this.BaseClient.BaseAddress, resource);
			HttpRequestMessage message = this.Factory.GetRequestMessage(method, uri);
			return this.Send(message);
		}

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="message">The HTTP request message to send.</param>
		/// <returns>Returns a request builder.</returns>
		/// <remarks>This is the base method which executes every request.</remarks>
		public virtual IRequestBuilder Send(HttpRequestMessage message)
		{
			return this.Factory.GetRequestBuilder(message, this.Formatters, request => this.BaseClient.SendAsync(request.Message));
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
			this.MessageHandler = handler;
			this.BaseClient = client;
			this.Factory = factory ?? new Factory();
			if (baseUri != null)
				this.BaseClient.BaseAddress = new Uri(baseUri);
			this.Formatters = this.Factory.GetDefaultFormatters();
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
