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
		/// <summary>Construct an asynchronous HTTP request.</summary>
		/// <param name="message">The underlying HTTP request message.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="dispatcher">Executes an HTTP request.</param>
		public virtual IRequest GetRequest(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
		{
			return new Request(message, formatters, dispatcher, this);
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
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		public virtual HttpRequestMessage GetRequestMessage(HttpMethod method, Uri resource, MediaTypeFormatterCollection formatters)
		{
			HttpRequestMessage request = new HttpRequestMessage(method, resource);

			// add default headers
			request.Headers.Add("user-agent", "FluentHttpClient/0.4 (+http://github.com/Pathoschild/Pathoschild.FluentHttpClient)");
			request.Headers.Add("accept", formatters.SelectMany(p => p.SupportedMediaTypes).Select(p => p.MediaType));

			return request;
		}
	}
}
