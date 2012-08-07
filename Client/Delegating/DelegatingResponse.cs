using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Delegating
{
	/// <summary>Retrieves the response from an asynchronous HTTP request. This implementation delegates work to an inner response.</summary>
	public abstract class DelegatingResponse : IResponse
	{
		/*********
		** Properties
		*********/
		/// <summary>The wrapped response.</summary>
		protected IResponse Implementation { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		public virtual HttpRequestMessage Request
		{
			get { return this.Implementation.Request; }
		}

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		public virtual bool ThrowError
		{
			get { return this.Implementation.ThrowError; }
		}

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		public virtual MediaTypeFormatterCollection Formatters
		{
			get { return this.Implementation.Formatters; }
		}


		/*********
		** Public methods
		*********/
		/***
		** Async
		***/
		/// <summary>Asynchronously retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<HttpResponseMessage> AsMessageAsync()
		{
			return this.Implementation.AsMessageAsync();
		}

		/// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<T> AsAsync<T>()
		{
			return this.Implementation.AsAsync<T>();
		}

		/// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<List<T>> AsListAsync<T>()
		{
			return this.Implementation.AsListAsync<T>();
		}

		/// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<byte[]> AsByteArrayAsync()
		{
			return this.Implementation.AsByteArrayAsync();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<string> AsStringAsync()
		{
			return this.Implementation.AsStringAsync();
		}

		/// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		public virtual Task<Stream> AsStreamAsync()
		{
			return this.Implementation.AsStreamAsync();
		}

		/***
		** Sync
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <returns>Returns this instance for chaining.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public virtual IResponse Wait()
		{
			this.Implementation.Wait();
			return this;
		}

		/// <summary>Retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		public virtual HttpResponseMessage AsMessage()
		{
			return this.Implementation.AsMessage();
		}

		/// <summary>Retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public virtual T As<T>()
		{
			return this.Implementation.As<T>();
		}

		/// <summary>Retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public virtual List<T> AsList<T>()
		{
			return this.Implementation.AsList<T>();
		}

		/// <summary>Retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public virtual byte[] AsByteArray()
		{
			return this.Implementation.AsByteArray();
		}

		/// <summary>Retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public virtual string AsString()
		{
			return this.Implementation.AsString();
		}

		/// <summary>Retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="IResponse.ThrowError"/> is <c>true</c>.</exception>
		public virtual Stream AsStream()
		{
			return this.Implementation.AsStream();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="response">The wrapped response.</param>
		protected DelegatingResponse(IResponse response)
		{
			this.Implementation = response;
		}
	}
}
