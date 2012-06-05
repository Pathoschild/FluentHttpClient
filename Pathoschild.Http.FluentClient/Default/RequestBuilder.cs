using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.FluentClient.Default
{
	/// <summary>Builds an asynchronous HTTP request.</summary>
	public class RequestBuilder : IRequestBuilder
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		public HttpRequestMessage Message { get; set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public MediaTypeFormatterCollection Formatters { get; set; }

		/// <summary>Executes an HTTP request.</summary>
		public Func<IRequestBuilder, Task<HttpResponseMessage>> ResponseBuilder { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="message">The underlying HTTP request message.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="responseBuilder">Executes an HTTP request.</param>
		public RequestBuilder(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequestBuilder, Task<HttpResponseMessage>> responseBuilder)
		{
			this.Message = message;
			this.Formatters = formatters;
			this.ResponseBuilder = responseBuilder;
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The HTTP body content.</param>
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="IRequestBuilder.Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		public IRequestBuilder WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
		{
			MediaTypeFormatter formatter = this.GetFormatter(contentType);
			return this.WithBody<T>(body, formatter, contentType != null ? contentType.MediaType : null);
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The HTTP body content.</param>
		/// <param name="formatter">The media type formatter with which to format the request body format.</param>
		/// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public IRequestBuilder WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null)
		{
			this.Message.Content = new ObjectContent<T>(body, formatter, mediaType);
			return this;
		}

		/// <summary>Set an HTTP header.</summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public IRequestBuilder WithHeader(string key, string value)
		{
			this.Message.Headers.Add(key, value);
			return this;
		}

		/// <summary>Set an HTTP query string argument.</summary>
		/// <param name="key">The key of the query argument.</param>
		/// <param name="value">The value of the query argument.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public IRequestBuilder WithArgument(string key, object value)
		{
			var query = this.Message.RequestUri.ParseQueryString();
			query.Add(key, value.ToString());
			string uri = this.Message.RequestUri.GetLeftPart(UriPartial.Path) + "?" + query;
			this.Message.RequestUri = new Uri(uri);
			return this;
		}

		/// <summary>Customize the underlying HTTP request message.</summary>
		/// <param name="request">The HTTP request message.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public IRequestBuilder WithCustom(Action<HttpRequestMessage> request)
		{
			request(this.Message);
			return this;
		}

		/// <summary>Asynchronously dispatch the request.</summary>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a response.</returns>
		public virtual IResponse Retrieve(bool throwError = true)
		{
			return new Response(this.Message, this.ResponseBuilder(this), this.Formatters, throwError);
		}

		/// <summary>Dispatch the request and retrieve the response as a deserialized model.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized model.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public TResponse RetrieveAs<TResponse>(bool throwError = true)
		{
			return this.Retrieve(throwError).As<TResponse>();
		}

		/// <summary>Dispatch the request and retrieve the response as a deserialized list of models.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized list of models.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public List<TResponse> RetrieveAsList<TResponse>(bool throwError = true)
		{
			return this.Retrieve(throwError).AsList<TResponse>();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get the formatter for an HTTP content type.</summary>
		/// <param name="contentType">The HTTP content type (or <c>null</c> to automatically select one).</param>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		protected MediaTypeFormatter GetFormatter(MediaTypeHeaderValue contentType = null)
		{
			if (!this.Formatters.Any())
				throw new InvalidOperationException("No MediaTypeFormatters are available on the API client.");

			MediaTypeFormatter formatter = contentType != null
				? this.Formatters.FirstOrDefault(f => f.MediaTypeMappings.Any(m => m.MediaType.MediaType == contentType.MediaType))
				: this.Formatters.FirstOrDefault();
			if (formatter == null)
				throw new InvalidOperationException(String.Format("No MediaTypeFormatters are available on the API client for the '{0}' content-type.", contentType));

			return formatter;
		}
	}
}
