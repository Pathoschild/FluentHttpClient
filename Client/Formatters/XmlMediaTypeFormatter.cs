using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>Serializes and deserializes data as XML. Uses <see cref="DataContractSerializer"/> by default, or <see cref="XmlSerializer"/> when <see cref="UseXmlSerializer"/> is enabled.</summary>
public class XmlMediaTypeFormatter : IMediaTypeFormatter
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedMediaTypes { get; } = ["application/xml", "text/xml"];

    /// <summary>Whether to use <see cref="XmlSerializer"/> instead of <see cref="DataContractSerializer"/>.</summary>
    public bool UseXmlSerializer { get; set; }


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
        // buffer the input stream asynchronously, then deserialize synchronously
        using MemoryStream buffer = new();
        await stream.CopyToAsync(
#if NET8_0_OR_GREATER
            buffer, cancellationToken
#else
            buffer
#endif
        ).ConfigureAwait(false);
        buffer.Position = 0;

        if (this.UseXmlSerializer)
        {
            XmlSerializer serializer = new(type);
            return serializer.Deserialize(buffer);
        }
        else
        {
            DataContractSerializer serializer = new(type);
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(buffer, XmlDictionaryReaderQuotas.Max);
            return serializer.ReadObject(reader);
        }
    }

    /// <inheritdoc />
    public async Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        // serialize synchronously into a buffer, then copy to output stream asynchronously
        using MemoryStream buffer = new();

        if (this.UseXmlSerializer)
        {
            XmlSerializer serializer = new(type);
            using XmlWriter writer = XmlWriter.Create(buffer, new XmlWriterSettings { CloseOutput = false });
            serializer.Serialize(writer, value);
            writer.Flush();
        }
        else
        {
            DataContractSerializer serializer = new(type);
            using XmlWriter writer = XmlWriter.Create(buffer, new XmlWriterSettings { CloseOutput = false });
            serializer.WriteObject(writer, value);
            writer.Flush();
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(
#if NET8_0_OR_GREATER
            stream, cancellationToken
#else
            stream
#endif
        ).ConfigureAwait(false);
    }
}
