using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Delegating
{
	/// <summary>Builds an asynchronous HTTP request. This implementation delegates work to an inner request builder.</summary>
	public abstract class DelegatingRequest : IRequest
	{
		/*********
		** Properties
		*********/
		/// <summary>The wrapped request builder implementation.</summary>
		protected IRequest Implementation { get; set; }


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

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		public bool ThrowError
		{
			get { return this.Implementation.ThrowError; }
			set { this.Implementation.ThrowError = value; }
		}


		/*********
		** Public methods
		*********/
		/***
		** Build request
		***/
		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="IRequest.Formatters"/>).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
		public virtual IRequest WithBody<T>(T body, MediaTypeHeaderValue contentType = null)
		{
			this.Implementation.WithBody<T>(body, contentType);
			return this;
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The value to serialize into the HTTP body content.</param>
		/// <param name="formatter">The media type formatter with which to format the request body format.</param>
		/// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithBody<T>(T body, MediaTypeFormatter formatter, string mediaType = null)
		{
			this.Implementation.WithBody<T>(body, formatter, mediaType);
			return this;
		}

		/// <summary>Set the body content of the HTTP request.</summary>
		/// <param name="body">The formatted HTTP body content.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithBodyContent(HttpContent body)
		{
			this.Implementation.WithBodyContent(body);
			return this;
		}

		/// <summary>Set an HTTP header.</summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithHeader(string key, string value)
		{
			this.Implementation.WithHeader(key, value);
			return this;
		}

		/// <summary>Add an HTTP query string argument.</summary>
		/// <param name="key">The key of the query argument.</param>
		/// <param name="value">The value of the query argument.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithArgument(string key, object value)
		{
			this.Implementation.WithArgument(key, value);
			return this;
		}

		/// <summary>Add HTTP query string arguments.</summary>
		/// <param name="arguments">The key=>value pairs in the query string. If this is a dictionary, the keys and values are used. Otherwise, the property names and values are used.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		/// <example><code>client.WithArguments(new { id = 14, name = "Joe" })</code></example>
		public IRequest WithArguments(object arguments)
		{
			this.Implementation.WithArguments(arguments);
			return this;
		}

		/// <summary>Customize the underlying HTTP request message.</summary>
		/// <param name="request">The HTTP request message.</param>
		/// <returns>Returns the request builder for chaining.</returns>
		public virtual IRequest WithCustom(Action<HttpRequestMessage> request)
		{
			this.Implementation.WithCustom(request);
			return this;
		}

		/***
		** Retrieve message
		***/
		/// <summary>Asynchronously retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<HttpResponseMessage> AsMessage()
		{
			return this.Implementation.AsMessage();
		}

		/// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<T> As<T>()
		{
			return this.Implementation.As<T>();
		}

		/// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<List<T>> AsList<T>()
		{
			return this.Implementation.AsList<T>();
		}

		/// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<byte[]> AsByteArray()
		{
			return this.Implementation.AsByteArray();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<string> AsString()
		{
			return this.Implementation.AsString();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<Stream> AsStream()
		{
			return this.Implementation.AsStream();
		}

		/***
		** Synchronize
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IRequest.ThrowError"/> is <c>true</c>.</exception>
		public void Wait()
		{
			this.Implementation.Wait();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="request">The wrapped request builder implementation.</param>
		protected DelegatingRequest(IRequest request)
		{
			this.Implementation = request;
		}
	}
}
