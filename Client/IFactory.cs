using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client
{
	/// <summary>Constructs implementations for the fluent client.</summary>
	public interface IFactory
	{
		/***
		** Fluent objects
		***/
		/// <summary>Construct an asynchronous HTTP request builder.</summary>
		/// <param name="message">The underlying HTTP request message.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="responseBuilder">Executes an HTTP request.</param>
		IRequestBuilder GetRequestBuilder(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequestBuilder, Task<HttpResponseMessage>> responseBuilder);

		/// <summary>Construct an asynchronous HTTP response.</summary>
		/// <param name="request">The underlying HTTP request message.</param>
		/// <param name="task">The request task.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		IResponse GetResponse(HttpRequestMessage request, Task<HttpResponseMessage> task, MediaTypeFormatterCollection formatters, bool throwError = true);

		/***
		** HttpClient
		***/
		/// <summary>Get the formatter for an HTTP content type.</summary>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="contentType">The HTTP content type (or <c>null</c> to automatically select one).</param>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		MediaTypeFormatter GetFormatter(MediaTypeFormatterCollection formatters, MediaTypeHeaderValue contentType = null);

		/// <summary>Get the default media type formatters used by the client.</summary>
		MediaTypeFormatterCollection GetDefaultFormatters();

		/// <summary>Construct an HTTP request message.</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		HttpRequestMessage GetRequestMessage(HttpMethod method, Uri resource);
	}
}
