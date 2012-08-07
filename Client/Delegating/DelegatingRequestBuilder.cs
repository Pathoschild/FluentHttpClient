using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Pathoschild.Http.Client.Delegating
{
	/// <summary>Builds an asynchronous HTTP request. This implementation delegates work to an inner request builder.</summary>
	public abstract class DelegatingRequestBuilder : IRequestBuilder
	{
		/*********
		** Properties
		*********/
		/// <summary>The wrapped request builder implementation.</summary>
		protected IRequestBuilder Implementation { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		public virtual HttpRequestMessage Message
		{
			get { return this.Implementation.Message; }
			set { this.Implementation.Message = value; }
		}

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public virtual MediaTypeFormatterCollection Formatters
		{
			get { return this.Implementation.Formatters; }
			set { this.Implementation.Formatters = value; }
		}


		/*********
		** Public methods
		*********/
		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="IRequestBuilder.Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		public virtual IRequestBuilder WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
		{
			this.Implementation.WithBody<T>(body, contentType);
			return this;
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="formatter">The media type formatter with which to format the request body format.</param>
		/// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequestBuilder WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null)
		{
			this.Implementation.WithBody<T>(body, formatter, mediaType);
			return this;
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The formatted HTTP body content.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequestBuilder WithBodyContent(HttpContent body)
		{
			this.Implementation.WithBodyContent(body);
			return this;
		}

		/// <summary>Set an HTTP header.</summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequestBuilder WithHeader(string key, string value)
		{
			this.Implementation.WithHeader(key, value);
			return this;
		}

		/// <summary>Set an HTTP query string argument.</summary>
		/// <param name="key">The key of the query argument.</param>
		/// <param name="value">The value of the query argument.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequestBuilder WithArgument(string key, object value)
		{
			this.Implementation.WithArgument(key, value);
			return this;
		}

		/// <summary>Customize the underlying HTTP request message.</summary>
		/// <param name="request">The HTTP request message.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequestBuilder WithCustom(Action<HttpRequestMessage> request)
		{
			this.Implementation.WithCustom(request);
			return this;
		}

		/// <summary>Asynchronously dispatch the request.</summary>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a response.</returns>
		public virtual IResponse Retrieve(bool throwError = true)
		{
			return this.Implementation.Retrieve(throwError);
		}

		/// <summary>Dispatch the request and retrieve the response as a deserialized model.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized model.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public virtual TResponse RetrieveAs<TResponse>(bool throwError = true)
		{
			return this.Implementation.RetrieveAs<TResponse>(throwError);
		}

		/// <summary>Dispatch the request and retrieve the response as a deserialized list of models.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized list of models.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public virtual List<TResponse> RetrieveAsList<TResponse>(bool throwError = true)
		{
			return this.Implementation.RetrieveAsList<TResponse>(throwError);
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="requestBuilder">The wrapped request builder implementation.</param>
		protected DelegatingRequestBuilder(IRequestBuilder requestBuilder)
		{
			this.Implementation = requestBuilder;
		}
	}
}
