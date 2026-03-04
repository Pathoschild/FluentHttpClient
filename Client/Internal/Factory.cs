using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Client.Internal;

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
    public static IMediaTypeFormatter GetFormatter(MediaTypeFormatterCollection formatters, MediaTypeHeaderValue? contentType = null)
    {
        if (!formatters.Any())
            throw new InvalidOperationException("No MediaTypeFormatters are available on the fluent client.");

        IMediaTypeFormatter? formatter = contentType != null
            ? formatters.FirstOrDefault(f => f.SupportedMediaTypes.Any(m => m == contentType.MediaType))
            : formatters.FirstOrDefault();
        if (formatter == null)
            throw new InvalidOperationException($"No MediaTypeFormatters are available on the fluent client for the '{contentType}' content-type.");

        return formatter;
    }

    /// <summary>Construct an HTTP request message.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="resource">The URI to send the request to.</param>
    /// <param name="formatters">The formatters used for serializing and deserializing message bodies.</param>
    public static HttpRequestMessage GetRequestMessage(HttpMethod method, Uri resource, MediaTypeFormatterCollection formatters)
    {
        HttpRequestMessage request = new(method, resource);

        // add default headers
        request.Headers.Add("accept", formatters.SelectMany(p => p.SupportedMediaTypes));

        return request;
    }
}
