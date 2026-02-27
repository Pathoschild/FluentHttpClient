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
    public Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        object? result;

        if (this.UseXmlSerializer)
        {
            XmlSerializer serializer = new(type);
            result = serializer.Deserialize(stream);
        }
        else
        {
            DataContractSerializer serializer = new(type);
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max);
            result = serializer.ReadObject(reader);
        }

        return Task.FromResult<object?>(result);
    }

    /// <inheritdoc />
    public Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
    {
        if (this.UseXmlSerializer)
        {
            XmlSerializer serializer = new(type);
            using XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { CloseOutput = false });
            serializer.Serialize(writer, value);
            writer.Flush();
        }
        else
        {
            DataContractSerializer serializer = new(type);
            using XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { CloseOutput = false });
            serializer.WriteObject(writer, value);
            writer.Flush();
        }

        return Task.CompletedTask;
    }
}
