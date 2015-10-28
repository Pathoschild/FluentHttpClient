using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client.Delegating
{
	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI. This implementation delegates work to an inner client.</summary>
	/// <typeparam name="TMessageHandler">The HTTP message handler type.</typeparam>
	public abstract class DelegatingFluentClient<TMessageHandler> : IClient<TMessageHandler>
		where TMessageHandler : HttpMessageHandler
	{
		/*********
		** Properties
		*********/
		/// <summary>The wrapped client implementation.</summary>
		protected IClient<TMessageHandler> Implementation { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP client.</summary>
		public virtual HttpClient BaseClient
		{
			get { return this.Implementation.BaseClient; }
		}

		/// <summary>The underlying HTTP message handler.</summary>
		public virtual TMessageHandler MessageHandler
		{
			get { return this.Implementation.MessageHandler; }
		}

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public virtual MediaTypeFormatterCollection Formatters
		{
			get { return this.Implementation.Formatters; }
		}


		/*********
		** Public methods
		*********/
		/// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest DeleteAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.Implementation.DeleteAsync(resource);
		}

		/// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest GetAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.Implementation.GetAsync(resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest PostAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.Implementation.PostAsync(resource);
		}

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest PostAsync<TBody>(string resource, TBody body)
		{
			this.AssertNotDisposed();

			return this.Implementation.PostAsync<TBody>(resource, body);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest PutAsync(string resource)
		{
			this.AssertNotDisposed();

			return this.Implementation.PutAsync(resource);
		}

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest PutAsync<TBody>(string resource, TBody body)
		{
			this.AssertNotDisposed();

			return this.Implementation.PutAsync<TBody>(resource, body);
		}

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		public virtual IRequest SendAsync(HttpMethod method, string resource)
		{
			this.AssertNotDisposed();

			return this.Implementation.SendAsync(method, resource);
		}

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="message">The HTTP request message to send.</param>
		/// <returns>Returns a request builder.</returns>
		/// <remarks>This is the base method which executes every request.</remarks>
		public virtual IRequest SendAsync(HttpRequestMessage message)
		{
			this.AssertNotDisposed();

			return this.Implementation.SendAsync(message);
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="client">The wrapped client implementation.</param>
		protected DelegatingFluentClient(IClient<TMessageHandler> client)
		{
			this.Implementation = client;
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
				this.Implementation.Dispose();
			}

			_disposed = true;
		}

		/// <summary>Destruct the instance.</summary>
		~DelegatingFluentClient()
		{
			Dispose(false);
		}

		/// <summary>Assert that the instance is not disposed.</summary>
		private void AssertNotDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(DelegatingFluentClient<TMessageHandler>));
		}
	}

	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI.</summary>
	public abstract class DelegatingFluentClient : DelegatingFluentClient<HttpClientHandler>, IClient
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="client">The wrapped client implementation.</param>
		protected DelegatingFluentClient(IClient client)
			: base(client) { }
	}
}
