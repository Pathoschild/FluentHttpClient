using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Pathoschild.Http.Client.Internal
{
    /// <summary>Internal helper for constructing instances.</summary>
    internal static class Factory
    {
        /*********
        ** Public methods
        *********/
        /***
        ** HttpClient
        ***/
        /// <summary>Get the formatter for an HTTP content type.</summary>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        /// <param name="contentType">The HTTP content type (or <c>null</c> to automatically select one).</param>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        public static MediaTypeFormatter GetFormatter(MediaTypeFormatterCollection formatters, MediaTypeHeaderValue contentType = null)
        {
            if (!formatters.Any())
                throw new InvalidOperationException("No MediaTypeFormatters are available on the fluent client.");

            MediaTypeFormatter formatter = contentType != null
                ? formatters.FirstOrDefault(f => f.MediaTypeMappings.Any(m => m.MediaType.MediaType == contentType.MediaType))
                : formatters.FirstOrDefault();
            if (formatter == null)
                throw new InvalidOperationException(String.Format("No MediaTypeFormatters are available on the fluent client for the '{0}' content-type.", contentType));

            return formatter;
        }

        /// <summary>Construct an HTTP request message.</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
        public static HttpRequestMessage GetRequestMessage(HttpMethod method, Uri resource, MediaTypeFormatterCollection formatters)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, resource);

            // add default headers
            request.Headers.Add("user-agent", "FluentHttpClient/0.4 (+http://github.com/Pathoschild/Pathoschild.FluentHttpClient)");
            request.Headers.Add("accept", formatters.SelectMany(p => p.SupportedMediaTypes).Select(p => p.MediaType));

            return request;
        }
    }
}
