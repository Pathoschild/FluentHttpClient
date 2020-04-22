using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Pathoschild.Http.Client
{
    /// <summary>Constructs HTTP request bodies.</summary>
    public interface IBodyBuilder
    {
        /*********
        ** Methods
        *********/
        /****
        ** Form URL encoded
        ****/
        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        /// <example><code>client.WithArguments(new { id = 14, name = "Joe" })</code></example>
        HttpContent FormUrlEncoded(object? arguments);

        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        HttpContent FormUrlEncoded(IDictionary<string, string?>? arguments);

        /// <summary>Get a form URL-encoded body.</summary>
        /// <param name="arguments">An anonymous object containing the property names and values to set.</param>
        /// <example><code>client.WithArguments(new[] { new KeyValuePair&lt;string, string&gt;("genre", "drama"), new KeyValuePair&lt;string, int&gt;("genre", "comedy") })</code></example>
        HttpContent FormUrlEncoded(IEnumerable<KeyValuePair<string, object?>>? arguments);

        /****
        ** File upload
        ****/
        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="fullPath">The absolute path to the file to upload.</param>
        /// <exception cref="KeyNotFoundException">The given path doesn't match a file.</exception>
        HttpContent FileUpload(string fullPath);

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="file">The file to upload.</param>
        /// <exception cref="KeyNotFoundException">The given file doesn't exist.</exception>
        HttpContent FileUpload(FileInfo file);

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="files">The files to upload.</param>
        /// <exception cref="KeyNotFoundException">A given file doesn't exist.</exception>
        HttpContent FileUpload(IEnumerable<FileInfo> files);

        /// <summary>Get a file upload body (using multi-part form data).</summary>
        /// <param name="files">The file streams and file names to upload.</param>
        HttpContent FileUpload(IEnumerable<KeyValuePair<string, Stream>> files);

        /****
        ** Model
        ****/
        /// <summary>Get a serialized model body.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the client's formatter).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        HttpContent Model<T>(T body, MediaTypeHeaderValue? contentType = null);

        /// <summary>Get a serialized model body.</summary>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="formatter">The media type formatter with which to format the request body format.</param>
        /// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        HttpContent Model<T>(T body, MediaTypeFormatter formatter, string? mediaType = null);
    }
}
