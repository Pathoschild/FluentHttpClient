using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>Base implementation of an <see cref="IMediaTypeFormatter"/> for serialization providers.</summary>
/// <remarks>This class handles the common code for implementing a media type formatter, so most subclasses only need to implement the <see cref="Serialize"/> and <see cref="Deserialize"/> methods.</remarks>
public abstract class MediaTypeFormatterBase : IMediaTypeFormatter
{
    /*********
    ** Fields
    *********/
    /// <summary>The supported media types.</summary>
    private readonly List<string> MediaTypes = [];


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedMediaTypes => this.MediaTypes;


    /*********
    ** Public methods
    *********/
    /***
    ** Generic
    ***/
    /// <inheritdoc />
    public virtual bool CanReadType(Type type)
    {
        return true;
    }

    /// <inheritdoc />
    public virtual bool CanWriteType(Type type)
    {
        return true;
    }

    /// <inheritdoc />
    public virtual Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        object result = this.Deserialize(type, stream, content);
        return Task.FromResult<object?>(result);
    }

    /// <inheritdoc />
    public virtual Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        this.Serialize(type, value, stream, content);
        return Task.CompletedTask;
    }

    /// <summary>Add a media type which can be read or written by this formatter.</summary>
    /// <param name="mediaType">The media type string.</param>
    public MediaTypeFormatterBase AddMediaType(string mediaType)
    {
        this.MediaTypes.Add(mediaType);
        return this;
    }

    /***
    ** Abstract
    ***/
    /// <summary>Deserialize an object from the stream.</summary>
    /// <param name="type">The type of object to read.</param>
    /// <param name="stream">The stream from which to read.</param>
    /// <param name="content">The HTTP content being read.</param>
    /// <returns>Returns a deserialized object.</returns>
    public abstract object Deserialize(Type type, Stream stream, HttpContent content);

    /// <summary>Serialize an object into the stream.</summary>
    /// <param name="type">The type of object to write.</param>
    /// <param name="value">The object instance to write.</param>
    /// <param name="stream">The stream to which to write.</param>
    /// <param name="content">The HTTP content being written.</param>
    public abstract void Serialize(Type type, object? value, Stream stream, HttpContent content);
}
