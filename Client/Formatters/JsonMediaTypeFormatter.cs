using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>Serializes and deserializes data as JSON using Newtonsoft.Json.</summary>
public class JsonMediaTypeFormatter : IMediaTypeFormatter
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedMediaTypes { get; } = ["application/json", "text/json"];

    /// <summary>The JSON serializer settings.</summary>
    public JsonSerializerSettings SerializerSettings { get; set; } = new();


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public bool CanReadType(Type type)
    {
        return true;
    }

    /// <inheritdoc />
    public bool CanWriteType(Type type)
    {
        return true;
    }

    /// <inheritdoc />
    public async Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        string json = await reader.ReadToEndAsync(
#if NET8_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);
        return JsonConvert.DeserializeObject(json, type, this.SerializerSettings);
    }

    /// <inheritdoc />
    public async Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        string json = JsonConvert.SerializeObject(value, type, this.SerializerSettings);
        byte[] bytes = new UTF8Encoding(false).GetBytes(json);
        await stream.WriteAsync(
#if NET8_0_OR_GREATER
            bytes.AsMemory(), cancellationToken
#else
            bytes, 0, bytes.Length, cancellationToken
#endif
        ).ConfigureAwait(false);
    }
}
