using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Client.Internal;

/// <summary>An <see cref="HttpContent"/> that serializes an object using an <see cref="IMediaTypeFormatter"/>.</summary>
/// <typeparam name="T">The body type.</typeparam>
internal sealed class FormatterContent<T> : HttpContent
{
    /*********
    ** Fields
    *********/
    /// <summary>The value to serialize.</summary>
    private readonly T Value;

    /// <summary>The formatter to use for serialization.</summary>
    private readonly IMediaTypeFormatter Formatter;

    /// <summary>The type to serialize as.</summary>
    private readonly Type ObjectType;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="mediaType">The media type, or <c>null</c> for the formatter's first supported type.</param>
    public FormatterContent(T value, IMediaTypeFormatter formatter, string? mediaType = null)
        : this(value, typeof(T), formatter, mediaType) { }

    /// <summary>Construct an instance with an explicit type.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="objectType">The type to use for serialization.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="mediaType">The media type, or <c>null</c> for the formatter's first supported type.</param>
    public FormatterContent(T value, Type objectType, IMediaTypeFormatter formatter, string? mediaType = null)
    {
        if (!formatter.CanWriteType(objectType))
            throw new InvalidOperationException($"The configured formatter '{formatter.GetType().Name}' can't write content of type '{objectType.FullName}'.");

        this.Value = value;
        this.Formatter = formatter;
        this.ObjectType = objectType;

        string resolvedMediaType = mediaType ?? (formatter.SupportedMediaTypes.Count > 0 ? System.Linq.Enumerable.First(formatter.SupportedMediaTypes) : "application/octet-stream");
        this.Headers.ContentType = new MediaTypeHeaderValue(resolvedMediaType);
    }


    /*********
    ** Protected methods
    *********/
    /// <inheritdoc />
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        await this.Formatter.WriteToStreamAsync(this.ObjectType, this.Value, stream, this, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}
