using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			this.Task = this.GetTaskPreprocessor(task);
		}

		/***
		** Async
		***/
		/// <summary>Asynchronously retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<HttpResponseMessage> AsMessageAsync()
		{
			return this.Task;
		}

		/// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<T> AsAsync<T>()
		{
			return this.Task
				.ContinueWith(task => task.Result.Content.ReadAsAsync<T>(this.Formatters), TaskContinuationOptions.OnlyOnRanToCompletion)
				.Unwrap();
		}

		/// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<List<T>> AsListAsync<T>()
		{
			return this.AsAsync<List<T>>();
		}

		/// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<byte[]> AsByteArrayAsync()
		{
			return this.Task
				.ContinueWith(task => task.Result.Content.ReadAsByteArrayAsync(), TaskContinuationOptions.OnlyOnRanToCompletion)
				.Unwrap();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<string> AsStringAsync()
		{
			return this.Task
				.ContinueWith(task => task.Result.Content.ReadAsStringAsync(), TaskContinuationOptions.OnlyOnRanToCompletion)
				.Unwrap();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public Task<Stream> AsStreamAsync()
		{
			return this.Task
				.ContinueWith(task =>
					{
						Task<Stream> stream = task.Result.Content.ReadAsStreamAsync();
						stream.Result.Position = 0;
						return stream;
					}, TaskContinuationOptions.OnlyOnRanToCompletion)
				.Unwrap();
		}

		/***
		** Sync
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <returns>Returns this instance for chaining.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public IResponse Wait()
		{
			this.AsMessage();
			return this;
		}

		/// <summary>Retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public HttpResponseMessage AsMessage()
		{
			return this.Synchronize(this.AsMessageAsync());
		}

		/// <summary>Retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public T As<T>()
		{
			return this.Synchronize(this.AsAsync<T>());
		}

		/// <summary>Retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public List<T> AsList<T>()
		{
			return this.Synchronize(this.AsListAsync<T>());
		}

		/// <summary>Retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public byte[] AsByteArray()
		{
			return this.Synchronize(this.AsByteArrayAsync());
		}

		/// <summary>Retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public string AsString()
		{
			return this.Synchronize(this.AsStringAsync());
		}

		/// <summary>Retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public Stream AsStream()
		{
			return this.Synchronize(this.AsStreamAsync());
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get the HTTP response task which includes any required preprocessing validation.</summary>
		/// <param name="httpTask">The HTTP task to wrap.</param>
		protected Task<HttpResponseMessage> GetTaskPreprocessor(Task<HttpResponseMessage> httpTask)
		{
			return Task<HttpResponseMessage>.Factory.StartNew(task =>
			{
				HttpResponseMessage message = (task as Task<HttpResponseMessage>).Result;
				this.ValidateResponse(message);
				return message;
			}, httpTask);
		}

		/// <summary>Assert that the HTTP response is valid.</summary>
		/// <param name="message">The response message to validate.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected virtual void ValidateResponse(HttpResponseMessage message)
		{
			if (this.ThrowError && !message.IsSuccessStatusCode)
				throw new ApiException(message, message.StatusCode, String.Format("The API query failed with status code {0}: {1}", message.StatusCode, message.ReasonPhrase));
		}

		/// <summary>Synchronously await a task and return its result.</summary>
		/// <typeparam name="T">The task result type.</typeparam>
		/// <param name="task">The task to await.</param>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		protected T Synchronize<T>(Task<T> task)
		{
			try
			{
				task.Wait();
				return task.Result;
			}
			catch (AggregateException)
			{
				if (this.Task.Exception != null && this.Task.Exception.InnerExceptions.Count != 0)
					throw this.Task.Exception.InnerExceptions.First();
				throw;
			}
		}
	}
}
