using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Default
{
	/// <summary>Retrieves the response from an asynchronous HTTP request.</summary>
	public class Response : IResponse
	{
		/*********
		** Properties
		*********/
		/// <summary>The underlying HTTP response task.</summary>
		protected Task<HttpResponseMessage> Task { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		public HttpRequestMessage Request { get; protected set; }

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		public bool ThrowError { get; protected set; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public MediaTypeFormatterCollection Formatters { get; protected set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="request">The underlying HTTP request message.</param>
		/// <param name="task">The request task.</param>
		/// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
		/// <param name="throwError">Whether to handle errors from the upstream server by throwing an exception.</param>
		public Response(HttpRequestMessage request, Task<HttpResponseMessage> task, MediaTypeFormatterCollection formatters, bool throwError = true)
		{
			this.Request = request;
			this.Formatters = formatters;
			this.ThrowError = throwError;
			this.Task = this.ValidateResponse(task);
		}

		/***
		** Async
		***/
		/// <summary>Asynchronously retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<HttpResponseMessage> AsMessage()
		{
			return this.Task;
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
		** Sync
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <returns>Returns this instance for chaining.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public IResponse Wait()
		{
			this.Task.Wait();
			return this;
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Assert that the HTTP response is valid.</summary>
		/// <param name="request">The response to validate.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected async Task<HttpResponseMessage> ValidateResponse(Task<HttpResponseMessage> request)
		{
			// fetch request
			HttpResponseMessage response = await request;
			this.ValidateResponse(response);
			return response;
		}

		/// <summary>Assert that the HTTP response is valid.</summary>
		/// <param name="message">The response message to validate.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected virtual void ValidateResponse(HttpResponseMessage message)
		{
			if (this.ThrowError && !message.IsSuccessStatusCode)
				throw new ApiException(message, message.StatusCode, String.Format("The API query failed with status code {0}: {1}", message.StatusCode, message.ReasonPhrase));
		}
	}
}
