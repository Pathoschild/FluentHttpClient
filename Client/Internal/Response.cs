using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Internal
{
    /// <summary>Asynchronously parses an HTTP response.</summary>
    public sealed class Response : IResponse
    {
        /*********
        ** Properties
        *********/
        /// <summary>The underlying HTTP response message.</summary>
        public HttpResponseMessage Message { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        public MediaTypeFormatterCollection Formatters { get; }

        /// <summary>The HTTP status code.</summary>
        public HttpStatusCode StatusCode => this.Message.StatusCode;

        /// <summary>The HTTP response headers.</summary>
        public HttpResponseHeaders Headers => this.Message.Headers;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="message">The underlying HTTP request message.</param>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        public Response(HttpResponseMessage message, MediaTypeFormatterCollection formatters)
        {
            this.Message = message;
            this.Formatters = formatters;
        }

        /// <summary>Asynchronously retrieve the response body as a deserialized model.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public Task<T> As<T>()
        {
            return this.Message.Content.ReadAsAsync<T>(this.Formatters);
        }

        /// <summary>Asynchronously retrieve the response body as a list of deserialized models.</summary>
        /// <typeparam name="T">The response model to deserialize into.</typeparam>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public Task<List<T>> AsList<T>()
        {
            return this.As<List<T>>();
        }

        /// <summary>Asynchronously retrieve the response body as an array of <see cref="byte"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public Task<byte[]> AsByteArray()
        {
            return this.Message.Content.ReadAsByteArrayAsync();
        }

        /// <summary>Asynchronously retrieve the response body as a <see cref="string"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public Task<string> AsString()
        {
            return this.Message.Content.ReadAsStringAsync();
        }

        /// <summary>Asynchronously retrieve the response body as a <see cref="Stream"/>.</summary>
        /// <returns>Returns the response body, or <c>null</c> if the response has no body.</returns>
        /// <exception cref="ApiException">An error occurred processing the response.</exception>
        public async Task<Stream> AsStream()
        {
            Stream stream = await this.Message.Content.ReadAsStreamAsync().ConfigureAwait(false);
            stream.Position = 0;
            return stream;
        }
    }
}
