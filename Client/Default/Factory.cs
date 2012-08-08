using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Default
{
	/// <summary>Constructs implementations for the fluent client.</summary>
	public class Factory : IFactory
	{
		/*********
		** Public methods
		*********/
		/***
		** Fluent objects
		***/
		/// <summary>Construct an asynchronous HTTP request builder.</summary>
		/// <param name="message">The underlying HTTP request message.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="responseBuilder">Executes an HTTP request.</param>
		public virtual IRequestBuilder GetRequestBuilder(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequestBuilder, Task<HttpResponseMessage>> responseBuilder)
		{
			return new RequestBuilder(message, formatters, responseBuilder) { Factory = this };
		}

		/// <summary>Construct an asynchronous HTTP response.</summary>
		/// <param name="request">The underlying HTTP request message.</param>
		/// <param name="task">The request task.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		public virtual IResponse GetResponse(HttpRequestMessage request, Task<HttpResponseMessage> task, MediaTypeFormatterCollection formatters, bool throwError = true)
		{
			return new Response(request, task, formatters, throwError);
		}

		/***
		** HttpClient
		***/
		/// <summary>Get the formatter for an HTTP content type.</summary>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="contentType">The HTTP content type (or <c>null</c> to automatically select one).</param>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		public virtual MediaTypeFormatter GetFormatter(MediaTypeFormatterCollection formatters, MediaTypeHeaderValue contentType = null)
		{
			if (!formatters.Any())
				throw new InvalidOperationException("No MediaTypeFormatters are available on the fluent client.");

			MediaTypeFormatter formatter = contentType != null
				? formatters.FirstOrDefault(f => f.MediaTypeMappings.Any(m => m.MediaType.MediaType == contentType.MediaType))
				: formatters.FirstOrDefault();
			if (formatter == null)
				throw new InvalidOperationException(String.Format("No MediaTypeFormatters are available on the fluent client for the '{0}' content-type.", contentType));

			return formatter;
		}

		/// <summary>Get the default media type formatters used by the client.</summary>
		public virtual MediaTypeFormatterCollection GetDefaultFormatters()
		{
			return new MediaTypeFormatterCollection();
		}

		/// <summary>Construct an HTTP request message.</summary>
		/// <param name="method">The HTTP method.</param>
		/// <param name="resource">The URI to send the request to.</param>
		public virtual HttpRequestMessage GetRequestMessage(HttpMethod method, Uri resource)
		{
			return new HttpRequestMessage(method, resource);
		}
	}
}
