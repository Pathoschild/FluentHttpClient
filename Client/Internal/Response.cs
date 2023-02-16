using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Pathoschild.Http.Client.Internal
{
    /// <inheritdoc cref="IResponse" />
    public sealed class Response : IResponse
    {
        /*********
        ** Fields
        *********/
        /// <inheritdoc />
        public bool IsSuccessStatusCode => this.Message.IsSuccessStatusCode;

        /// <inheritdoc />
        public HttpResponseMessage Message { get; }

        /// <inheritdoc />
        public MediaTypeFormatterCollection Formatters { get; }

        /// <inheritdoc />
        public HttpStatusCode Status => this.Message.StatusCode;


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

        /// <inheritdoc />
        public Task<T> As<T>(CancellationToken cancellationToken = default)
        {
            return this.Message.Content.ReadAsAsync<T>(this.Formatters, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T[]> AsArray<T>(CancellationToken cancellationToken = default)
        {
            return this.As<T[]>(cancellationToken);
        }

        /// <inheritdoc />
        public Task<byte[]> AsByteArray(CancellationToken cancellationToken = default)
        {
            return this.AssertContent().ReadAsByteArrayAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> AsString(CancellationToken cancellationToken = default)
        {
            return this.AssertContent().ReadAsStringAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Stream> AsStream(CancellationToken cancellationToken = default)
        {
            Stream stream = await this.AssertContent().ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
            return stream;
        }

        /// <inheritdoc />
        public async Task<JToken> AsRawJson(CancellationToken cancellationToken = default)
        {
            string content = await this.AsString(cancellationToken);
            return JToken.Parse(content);
        }

        /// <inheritdoc />
        public async Task<JObject> AsRawJsonObject(CancellationToken cancellationToken = default)
        {
            string content = await this.AsString(cancellationToken);
            return JObject.Parse(content);
        }

        /// <inheritdoc />
        public async Task<JArray> AsRawJsonArray(CancellationToken cancellationToken = default)
        {
            string content = await this.AsString(cancellationToken);
            return JArray.Parse(content);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that the response has a body.</summary>
        /// <exception cref="NullReferenceException">The response has no response body to read.</exception>
        private HttpContent AssertContent()
        {
            return this.Message?.Content ?? throw new NullReferenceException("The response has no body to read.");
        }
    }
}
