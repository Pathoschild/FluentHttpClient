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
    public Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        JsonSerializer serializer = JsonSerializer.Create(this.SerializerSettings);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        using JsonTextReader jsonReader = new(reader);
        object? result = serializer.Deserialize(jsonReader, type);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        JsonSerializer serializer = JsonSerializer.Create(this.SerializerSettings);

        using StreamWriter writer = new(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
        using JsonTextWriter jsonWriter = new(writer);
        serializer.Serialize(jsonWriter, value, type);
        jsonWriter.Flush();
        return Task.CompletedTask;
    }
}
