using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace Pathoschild.Http.Client.Internal
{
    /// <inheritdoc cref="IBodyBuilder" />
    internal class BodyBuilder : IBodyBuilder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying request.</summary>
        private readonly IRequest Request;


        /*********
        ** Public methods
        *********/
        /****
        ** Constructors
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="request">The underlying request.</param>
        public BodyBuilder(IRequest request)
        {
            this.Request = request;
        }

        /****
        ** Form URL encoded
        ****/
        /// <inheritdoc />
        public HttpContent FormUrlEncoded(object? arguments)
        {
            return this.FormUrlEncodedImpl(arguments.GetKeyValueArguments());
        }

        /// <inheritdoc />
        public HttpContent FormUrlEncoded(IDictionary<string, string?>? arguments)
        {
            if (arguments == null)
                return this.FormUrlEncodedImpl(null);

            return this.FormUrlEncodedImpl(
                from pair in arguments
                where pair.Value != null || this.Request.Options.IgnoreNullArguments == false
                select new KeyValuePair<string, object?>(pair.Key, pair.Value)
            );
        }

        /// <inheritdoc />
        public HttpContent FormUrlEncoded(IEnumerable<KeyValuePair<string, object?>>? arguments)
        {
            return this.FormUrlEncodedImpl(arguments);
        }

        /****
        ** File upload
        ****/
        /// <inheritdoc />
        public HttpContent FileUpload(string fullPath)
        {
            return this.FileUpload(new FileInfo(fullPath));
        }

        /// <inheritdoc />
        public HttpContent FileUpload(FileInfo file)
        {
            return this.FileUpload(new[] { file });
        }

        /// <inheritdoc />
        public HttpContent FileUpload(IEnumerable<FileInfo> files)
        {
            return this.FileUpload(
                files.Select(file => file.Exists
                    ? new KeyValuePair<string, Stream>(file.Name, file.OpenRead())
                    : throw new FileNotFoundException($"There's no file matching path '{file.FullName}'.")
                )
            );
        }

        /// <inheritdoc />
        public HttpContent FileUpload(IEnumerable<KeyValuePair<string, Stream>> files)
        {
            var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
                StreamContent streamContent = new StreamContent(file.Value);
                content.Add(streamContent, file.Key, file.Key);
            }

            return content;
        }

        /****
        ** Model
        ****/
        /// <inheritdoc />
        public HttpContent Model<T>(T body, MediaTypeHeaderValue? contentType = null)
        {
            MediaTypeFormatter formatter = Factory.GetFormatter(this.Request.Formatters, contentType);
            string? mediaType = contentType?.MediaType;
            return new ObjectContent<T>(body, formatter, mediaType);
        }

        /// <inheritdoc />
        public HttpContent Model<T>(T body, MediaTypeFormatter formatter, string? mediaType = null)
        {
            return new ObjectContent<T>(body, formatter, mediaType);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        /// <remarks>This bypasses <see cref="FormUrlEncodedContent"/>, which restricts the body length to the maximum size of a URL. That's not applicable for a URL-encoded body.</remarks>
        private HttpContent FormUrlEncodedImpl(IEnumerable<KeyValuePair<string, object?>>? arguments)
        {
            IEnumerable<string> pairs = arguments != null
                ? (
                    from pair in arguments
                    where pair.Value != null || this.Request.Options.IgnoreNullArguments == false
                    select $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value?.ToString())}"
                )
                : Enumerable.Empty<string>();

            return new StringContent(string.Join("&", pairs), Encoding.UTF8, "application/x-www-form-urlencoded");
        }
    }
}
