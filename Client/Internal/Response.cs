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

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="message">The underlying HTTP request message.</param>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Response(HttpResponseMessage message, MediaTypeFormatterCollection formatters, CancellationToken cancellationToken = default)
        {
            this.Message = message;
            this.Formatters = formatters;
            this.CancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        public IResponse WithCancellationToken(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            return this;
        }

        /// <inheritdoc />
        public Task<T> As<T>()
        {
            return this.Message.Content.ReadAsAsync<T>(this.Formatters, this.CancellationToken);
        }

        /// <inheritdoc />
        public Task<T[]> AsArray<T>()
        {
            return this.As<T[]>();
        }

        /// <inheritdoc />
        public Task<byte[]> AsByteArray()
        {
            return this.AssertContent().ReadAsByteArrayAsync(
#if NET5_0_OR_GREATER
                this.CancellationToken
#endif
            );
        }

        /// <inheritdoc />
        public Task<string> AsString()
        {
            return this.AssertContent().ReadAsStringAsync(
#if NET5_0_OR_GREATER
                this.CancellationToken
#endif
            );
        }

        /// <inheritdoc />
        public async Task<Stream> AsStream()
        {
            Stream stream = await this.AssertContent()
                .ReadAsStreamAsync(
#if NET5_0_OR_GREATER
                this.CancellationToken
#endif
                )
                .ConfigureAwait(false);
            stream.Position = 0;
            return stream;
        }

        /// <inheritdoc />
        public async Task<JToken> AsRawJson()
        {
            string content = await this.AsString().ConfigureAwait(false);
            return JToken.Parse(content);
        }

        /// <inheritdoc />
        public async Task<JObject> AsRawJsonObject()
        {
            string content = await this.AsString().ConfigureAwait(false);
            return JObject.Parse(content);
        }

        /// <inheritdoc />
        public async Task<JArray> AsRawJsonArray()
        {
            string content = await this.AsString().ConfigureAwait(false);
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
