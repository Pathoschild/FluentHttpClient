using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Pathoschild.Http.Client
{
	/// <summary>Builds an asynchronous HTTP request.</summary>
	public interface IRequestBuilder
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		HttpRequestMessage Message { get; set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		MediaTypeFormatterCollection Formatters { get; set; }


		/*********
		** Methods
		*********/
		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		IRequestBuilder WithBody<T>(T body, MediaTypeHeaderValue contentType = null);

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="formatter">The media type formatter with which to format the request body format.</param>
		/// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		IRequestBuilder WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null);

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The formatted HTTP body content.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		IRequestBuilder WithBodyContent(HttpContent body);

		/// <summary>Set an HTTP header.</summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		IRequestBuilder WithHeader(string key, string value);

		/// <summary>Set an HTTP query string argument.</summary>
		/// <param name="key">The key of the query argument.</param>
		/// <param name="value">The value of the query argument.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		IRequestBuilder WithArgument(string key, object value);

		/// <summary>Customize the underlying HTTP request message.</summary>
		/// <param name="request">The HTTP request message.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		IRequestBuilder WithCustom(Action<HttpRequestMessage> request);

		/// <summary>Asynchronously dispatch the request.</summary>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a response.</returns>
		IResponse RetrieveAsync(bool throwError = true);

		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <returns>Returns this instance for chaining.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		IResponse Wait(bool throwError = true);
	}
}