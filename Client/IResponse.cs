using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Pathoschild.Http.Client
{
    /// <summary>Asynchronously parses an HTTP response.</summary>
    public interface IResponse
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether the HTTP response was successful.</summary>
        bool IsSuccessStatusCode { get; }

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T> As<T>(CancellationToken cancellationToken = default);

        /// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<T[]> AsArray<T>(CancellationToken cancellationToken = default);

        /// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<byte[]> AsByteArray(CancellationToken cancellationToken = default);

        /// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<string> AsString(CancellationToken cancellationToken = default);

        /// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<Stream> AsStream(CancellationToken cancellationToken = default);

        /// <summary>Get a raw JSON representation of the response, which can also be accessed as a <c>dynamic</c> value.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<JToken> AsRawJson(CancellationToken cancellationToken = default);

        /// <summary>Get a raw JSON object representation of the response, which can also be accessed as a <c>dynamic</c> value.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<JObject> AsRawJsonObject(CancellationToken cancellationToken = default);

        /// <summary>Get a raw JSON array representation of the response, which can also be accessed as a <c>dynamic</c> value.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        Task<JArray> AsRawJsonArray(CancellationToken cancellationToken = default);
    }
}
