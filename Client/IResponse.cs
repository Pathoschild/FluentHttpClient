using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client
{
    /// <summary>Asynchronously parses an HTTP response.</summary>
    public interface IResponse
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The HTTP status code.</summary>
        HttpStatusCode Status { get; }

        /// <summary>The underlying HTTP response message.</summary>
        HttpResponseMessage Message { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        MediaTypeFormatterCollection Formatters { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T> As<T>();

        /// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T[]> AsArray<T>();

        /// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<byte[]> AsByteArray();

        /// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<string> AsString();

        /// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<Stream> AsStream();
    }
}
