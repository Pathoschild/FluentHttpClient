using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>Defines methods for serializing and deserializing HTTP message bodies.</summary>
public interface IMediaTypeFormatter
{
    /// <summary>The media types supported by this formatter.</summary>
    IReadOnlyCollection<string> SupportedMediaTypes { get; }

    /// <summary>Determines whether this formatter can deserialize an object of the specified type.</summary>
    /// <param name="type">The type of object that will be deserialized.</param>
    bool CanReadType(Type type);

    /// <summary>Determines whether this formatter can serialize an object of the specified type.</summary>
    /// <param name="type">The type of object that will be serialized.</param>
    bool CanWriteType(Type type);

    /// <summary>Reads an object from the stream asynchronously.</summary>
    /// <param name="type">The type of object to read.</param>
    /// <param name="stream">The stream from which to read.</param>
    /// <param name="content">The HTTP content being read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken);

    /// <summary>Writes an object to the stream asynchronously.</summary>
    /// <param name="type">The type of object to write.</param>
    /// <param name="value">The object instance to write.</param>
    /// <param name="stream">The stream to which to write.</param>
    /// <param name="content">The HTTP content being written.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken);
}
