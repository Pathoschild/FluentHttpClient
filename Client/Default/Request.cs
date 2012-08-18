using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Default
{
	/// <summary>Builds and dispatches an asynchronous HTTP request.</summary>
	public class Request : IRequest
	{
		/*********
		** Properties
		*********/
		/// <summary>Constructs implementations for the fluent client.</summary>
		protected IFactory Factory { get; set; }

		/// <summary>Executes an HTTP request.</summary>
		protected Func<IRequest, Task<HttpResponseMessage>> ResponseBuilder { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		public HttpRequestMessage Message { get; set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public MediaTypeFormatterCollection Formatters { get; set; }

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		public bool ThrowError { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="message">The underlying HTTP request message.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="dispatcher">Executes an HTTP request.</param>
		/// <param name="factory">Constructs implementations for the fluent client.</param>
		public Request(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequest, Task<HttpResponseMessage>> dispatcher, IFactory factory = null)
		{
			this.Message = message;
			this.Formatters = formatters;
			this.ResponseBuilder = dispatcher;
			this.Factory = factory ?? new Factory();
			this.ThrowError = true;
		}

		/***
		** Build request
		***/
		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Client.IRequest.Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		public virtual IRequest WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
		{
			MediaTypeFormatter formatter = this.Factory.GetFormatter(this.Formatters, contentType);
			string mediaType = contentType != null ? contentType.MediaType : null;
			return this.WithBody<T>(body, formatter, mediaType);
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="formatter">The media type formatter with which to format the request body format.</param>
		/// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null)
		{
			return this.WithBodyContent(new ObjectContent<T>(body, formatter, mediaType));
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The formatted HTTP body content.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithBodyContent(HttpContent body)
		{
			this.Message.Content = body;
			return this;
		}

		/// <summary>Set an HTTP header.</summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithHeader(string key, string value)
		{
			this.Message.Headers.Add(key, value);
			return this;
		}

		/// <summary>Set an HTTP query string argument.</summary>
		/// <param name="key">The key of the query argument.</param>
		/// <param name="value">The value of the query argument.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithArgument(string key, object value)
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
		public virtual IRequest WithCustom(Action<HttpRequestMessage> request)
		{
			request(this.Message);
			return this;
		}

		/***
		** Retrieve response
		***/
		/// <summary>Asynchronously retrieve the HTTP response.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<HttpResponseMessage> AsMessage()
		{
			return this.ValidateResponse(this.ResponseBuilder(this));
		}

		/// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual async Task<T> As<T>()
		{
			HttpResponseMessage message = await this.AsMessage();
			return await message.Content.ReadAsAsync<T>(this.Formatters);
		}

		/// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<List<T>> AsList<T>()
		{
			return this.As<List<T>>();
		}

		/// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual async Task<byte[]> AsByteArray()
		{
			HttpResponseMessage message = await this.AsMessage();
			return await message.Content.ReadAsByteArrayAsync();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual async Task<string> AsString()
		{
			HttpResponseMessage message = await this.AsMessage();
			return await message.Content.ReadAsStringAsync();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual async Task<Stream> AsStream()
		{
			HttpResponseMessage message = await this.AsMessage();
			Stream stream = await message.Content.ReadAsStreamAsync();
			stream.Position = 0;
			return stream;
		}

		/***
		** Synchronize
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/> and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public void Wait()
		{
			this.AsMessage().Wait();
		}

		/*********
		** Protected methods
		*********/
		/// <summary>Validate the HTTP response and raise any errors in the response as exceptions.</summary>
		/// <param name="request">The response message to validate.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/> and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected async Task<HttpResponseMessage> ValidateResponse(Task<HttpResponseMessage> request)
		{
			// fetch request
			HttpResponseMessage response = await request;
			this.ValidateResponse(response);
			return response;
		}

		/// <summary>Validate the HTTP response and raise any errors in the response as exceptions.</summary>
		/// <param name="message">The response message to validate.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/> and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected virtual void ValidateResponse(HttpResponseMessage message)
		{
			if (this.ThrowError && !message.IsSuccessStatusCode)
				throw new ApiException(message, message.StatusCode, String.Format("The API query failed with status code {0}: {1}", message.StatusCode, message.ReasonPhrase));
		}
	}
}
