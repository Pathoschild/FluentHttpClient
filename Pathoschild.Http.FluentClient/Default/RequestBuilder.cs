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
	/// <summary>Builds an HTTP request.</summary>
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
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public IRequestBuilder WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
		{
			this.Message.Content = this.Message.CreateContent<T>(body);
			this.Message.Content.Headers.ContentType = contentType ?? this.Formatters.SelectMany(p => p.SupportedMediaTypes).First();
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

		/// <summary>Execute the request and retrieve the response as a deserialized model.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized model.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public TResponse RetrieveAs<TResponse>(bool throwError = true)
		{
			return this.Retrieve(throwError).As<TResponse>();
		}

		/// <summary>Execute the request and retrieve the response as a deserialized list of models.</summary>
		/// <typeparam name="TResponse">The response body type.</typeparam>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a deserialized list of models.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <paramref name="throwError"/> is <c>true</c>.</exception>
		public List<TResponse> RetrieveAsList<TResponse>(bool throwError = true)
		{
			return this.Retrieve(throwError).AsList<TResponse>();
		}

		/// <summary>Execute the request and retrieve the response.</summary>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		/// <returns>Returns a response.</returns>
		/// <remarks>This is the base method which executes every request.</remarks>
		public virtual IResponse Retrieve(bool throwError = true)
		{
			return new Response(this.Message, this.ResponseBuilder(this), this.Formatters, throwError);
		}
	}
}
