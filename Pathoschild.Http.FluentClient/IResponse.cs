using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Pathoschild.Http.FluentClient
{
	/// <summary>Retrieves the response from an asynchronous HTTP request.</summary>
	public interface IResponse
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		HttpRequestMessage Request { get; }

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		bool ThrowError { get; }

		/// <summary>The formatters used for serializing and deserializing message bodies.</summary>
		MediaTypeFormatterCollection Formatters { get; }


		/*********
		** Methods
		*********/
		/***
		** Async
		***/
		/// <summary>Asynchronously retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<HttpResponseMessage> AsMessageAsync();

		/// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<T> AsAsync<T>();

		/// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<List<T>> AsListAsync<T>();

		/// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<byte[]> AsByteArrayAsync();

		/// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<string> AsStringAsync();

		/// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<Stream> AsStreamAsync();

		/***
		** Sync
		***/
		/// <summary>Block the current thread until the asynchronous request completes.</summary>
		/// <returns>Returns this instance for chaining.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		IResponse Wait();

		/// <summary>Retrieve the underlying response message.</summary>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		HttpResponseMessage AsMessage();

		/// <summary>Retrieve the response body as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		T As<T>();

		/// <summary>Retrieve the response body as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		List<T> AsList<T>();

		/// <summary>Retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		byte[] AsByteArray();

		/// <summary>Retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		string AsString();

		/// <summary>Retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		Stream AsStream();
	}
}