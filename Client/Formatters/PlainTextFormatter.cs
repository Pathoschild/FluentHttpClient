using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>Serializes and deserializes data as plaintext.</summary>
/// <remarks>This formatter is derived from <a href="http://github.com/WebApiContrib">WebApiContrib</a>, which was not compatible with the release candidate at the time of creation.</remarks>
public class PlainTextFormatter : MediaTypeFormatterBase
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether to allow formatting of types that cannot be deserialized using a <see cref="PlainTextFormatter"/>.</summary>
    public bool AllowIrreversibleSerialization { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public PlainTextFormatter()
    {
        this.AddMediaType("text/plain");
    }

    /// <inheritdoc />
    public override bool CanReadType(Type type)
    {
        return type == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanWriteType(Type type)
    {
        return type == typeof(string) || (this.AllowIrreversibleSerialization && typeof(IFormattable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
    }

    /// <inheritdoc />
    public override object Deserialize(Type type, Stream stream, HttpContent content)
    {
        StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        return reader.ReadToEnd();
    }

    /// <inheritdoc />
    public override void Serialize(Type type, object? value, Stream stream, HttpContent content)
    {
        StreamWriter writer = new(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
        writer.Write(value != null ? value.ToString() : string.Empty);
        writer.Flush();
    }

    /// <inheritdoc />
    public override async Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        string result = await reader.ReadToEndAsync(
#if NET8_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public override async Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        StreamWriter writer = new(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
        string text = value != null ? value.ToString()! : string.Empty;
        await writer.WriteAsync(
#if NET8_0_OR_GREATER
            text.AsMemory(), cancellationToken
#else
            text
#endif
        ).ConfigureAwait(false);
        await writer.FlushAsync(
#if NET8_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);
    }
}
