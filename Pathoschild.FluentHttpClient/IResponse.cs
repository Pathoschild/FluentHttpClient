using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pathoschild.FluentHttpClient
{
	/// <summary>Executes an HTTP request and retrieves the response.</summary>
	public interface IResponse
	{
		/*********
		** Accessors
		*********/
		/// <summary>The underlying HTTP request message.</summary>
		HttpRequestMessage Request { get; }

		/// <summary>Whether to handle errors from the upstream server by throwing an exception.</summary>
		bool ThrowError { get; }


		/*********
		** Methods
		*********/
		/***
		** Async
		***/
		/// <summary>Execute the request and asynchronously retrieve the response as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<T> AsAsync<T>();

		/// <summary>Execute the request and asynchronously retrieve the response as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<List<T>> AsListAsync<T>();

		/// <summary>Execute the request and asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<byte[]> AsByteArrayAsync();

		/// <summary>Execute the request and asynchronously retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<string> AsStringAsync();

		/// <summary>Execute the request and asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">An error occurred processing the response.</exception>
		Task<Stream> AsStreamAsync();

		/***
		** Sync
		***/
		/// <summary>Execute the request and retrieve the response as a deserialized model.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		T As<T>();

		/// <summary>Execute the request and retrieve the response as a list of deserialized models.</summary>
		/// <typeparam name="T">The response model to deserialize into.</typeparam>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		List<T> AsList<T>();

		/// <summary>Execute the request and retrieve the response body as an array of <see cref="byte"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		byte[] AsByteArray();

		/// <summary>Execute the request and retrieve the response body as a <see cref="string"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		string AsString();

		/// <summary>Execute the request and retrieve the response body as a <see cref="Stream"/>.</summary>
		/// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
		/// <exception cref="ApiException">The HTTP response returned a non-success <see cref="HttpStatusCode"/>, and <see cref="ThrowError"/> is <c>true</c>.</exception>
		Stream AsStream();
	}
}