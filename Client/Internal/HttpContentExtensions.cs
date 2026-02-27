using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Client.Internal;

/// <summary>Extension methods for <see cref="HttpContent"/>.</summary>
internal static class HttpContentExtensions
{
    /// <summary>Read the HTTP content as a deserialized model.</summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="content">The HTTP content to read.</param>
    /// <param name="formatters">The formatters to use for deserialization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<T> ReadAsAsync<T>(this HttpContent content, MediaTypeFormatterCollection formatters, CancellationToken cancellationToken = default)
    {
        string? mediaType = content.Headers.ContentType?.MediaType;

        // find a matching formatter
        IMediaTypeFormatter? formatter = mediaType != null
            ? formatters.FirstOrDefault(f => f.SupportedMediaTypes.Any(m => m == mediaType) && f.CanReadType(typeof(T)))
            : null;

        // fall back to first formatter that can read this type
        formatter ??= formatters.FirstOrDefault(f => f.CanReadType(typeof(T)));

        if (formatter == null)
            throw new InvalidOperationException($"No MediaTypeFormatter is available to read type '{typeof(T).FullName}' from content with media type '{mediaType}'.");

        Stream stream = await content
            .ReadAsStreamAsync(
#if NET8_0_OR_GREATER
                cancellationToken
#endif
            )
            .ConfigureAwait(false);

        if (stream.CanSeek)
            stream.Position = 0;

        object? result = await formatter.ReadFromStreamAsync(typeof(T), stream, content, cancellationToken).ConfigureAwait(false);
        return (T)result!;
    }
}
