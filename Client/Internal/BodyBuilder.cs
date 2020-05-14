using System;
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
    /// <summary>Constructs HTTP request bodies.</summary>
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
        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        /// <example><code>client.WithArguments(new { id = 14, name = "Joe" })</code></example>
        public HttpContent FormUrlEncoded(object? arguments)
        {
            return this.FormUrlEncodedImpl(arguments.GetKeyValueArguments());
        }

        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
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

        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        /// <example><code>client.WithArguments(new[] { new KeyValuePair&lt;string, string&gt;("genre", "drama"), new KeyValuePair&lt;string, int&gt;("genre", "comedy") })</code></example>
        public HttpContent FormUrlEncoded(IEnumerable<KeyValuePair<string, object?>>? arguments)
        {
            return this.FormUrlEncodedImpl(arguments);
        }

        /****
        ** File upload
        ****/
        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="fullPath">The absolute path to the file to upload.</param>
        /// <exception cref="KeyNotFoundException">The given path doesn't match a file.</exception>
        public HttpContent FileUpload(string fullPath)
        {
            return this.FileUpload(new FileInfo(fullPath));
        }

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="file">The file to upload.</param>
        /// <exception cref="KeyNotFoundException">The given file doesn't exist.</exception>
        public HttpContent FileUpload(FileInfo file)
        {
            return this.FileUpload(new[] { file });
        }

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="files">The files to upload.</param>
        /// <exception cref="KeyNotFoundException">A given file doesn't exist.</exception>
        public HttpContent FileUpload(IEnumerable<FileInfo> files)
        {
            return this.FileUpload(
                files.Select(file => file.Exists
                    ? new KeyValuePair<string, Stream>(file.Name, file.OpenRead())
                    : throw new FileNotFoundException($"There's no file matching path '{file.FullName}'.")
                )
            );
        }

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="files">The file streams and file names to upload.</param>
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
        /// <summary>Get the serialized model body.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the client's formatter).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        public HttpContent Model<T>(T body, MediaTypeHeaderValue? contentType = null)
        {
            MediaTypeFormatter formatter = Factory.GetFormatter(this.Request.Formatters, contentType);
            string? mediaType = contentType?.MediaType;
            return new ObjectContent<T>(body, formatter, mediaType);
        }

        /// <summary>Get a serialized model body.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="formatter">The media type formatter with which to format the request body format.</param>
        /// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
        /// <returns>Returns the request builder for chaining.</returns>
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
