using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client
{
	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI.</summary>
	/// <typeparam name="TMessageHandler">The HTTP message handler type.</typeparam>
	public interface IClient<out TMessageHandler>
		where TMessageHandler : HttpMessageHandler
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP client.</summary>
		HttpClient BaseClient { get; }

		/// <summary>The underlying HTTP message handler.</summary>
		TMessageHandler MessageHandler { get; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		MediaTypeFormatterCollection Formatters { get; }


		/*********
		** Methods
		*********/
		/// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Delete(string resource);

		/// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Get(string resource);

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Post(string resource);

		/// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Post<TBody>(string resource, TBody body);

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Put(string resource);

		/// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
		/// <typeparam name="TBody">The request body type.</typeparam>
		/// <param name="resource">The URI to send the request to.</param>
		/// <param name="body">The request body.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Put<TBody>(string resource, TBody body);

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Send(HttpMethod method, string resource);

		/// <summary>Create an asynchronous request message (but don't dispatch it yet).</summary>
		/// <param name="message">The HTTP request message to send.</param>
		/// <returns>Returns a request builder.</returns>
		IRequestBuilder Send(HttpRequestMessage message);
	}

	/// <summary>Sends HTTP requests and receives responses from a resource identified by a URI.</summary>
	public interface IClient : IClient<HttpClientHandler> { }
}
