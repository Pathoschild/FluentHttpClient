using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters;

/// <summary>Unit tests verifying that the <see cref="XmlMediaTypeFormatter"/> correctly serializes and deserializes content.</summary>
[TestFixture]
public class XmlMediaTypeFormatterTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** SupportedMediaTypes
    ****/
    [Test(Description = "Ensure that the formatter supports 'application/xml'.")]
    public void SupportedMediaTypes_ContainsApplicationXml()
    {
        XmlMediaTypeFormatter formatter = new();
        Assert.That(formatter.SupportedMediaTypes, Does.Contain("application/xml"));
    }

    [Test(Description = "Ensure that the formatter supports 'text/xml'.")]
    public void SupportedMediaTypes_ContainsTextXml()
    {
        XmlMediaTypeFormatter formatter = new();
        Assert.That(formatter.SupportedMediaTypes, Does.Contain("text/xml"));
    }

    /****
    ** CanReadType / CanWriteType
    ****/
    [Test(Description = "Ensure that CanReadType returns true for all types.")]
    [TestCase(typeof(string))]
    [TestCase(typeof(int))]
    [TestCase(typeof(XmlSampleModel))]
    public void CanReadType_ReturnsTrue(Type type)
    {
        XmlMediaTypeFormatter formatter = new();
        Assert.That(formatter.CanReadType(type), Is.True);
    }

    [Test(Description = "Ensure that CanWriteType returns true for all types.")]
    [TestCase(typeof(string))]
    [TestCase(typeof(int))]
    [TestCase(typeof(XmlSampleModel))]
    public void CanWriteType_ReturnsTrue(Type type)
    {
        XmlMediaTypeFormatter formatter = new();
        Assert.That(formatter.CanWriteType(type), Is.True);
    }

    /****
    ** Round-trip with DataContractSerializer (default)
    ****/
    [Test(Description = "Ensure that a model round-trips correctly with DataContractSerializer.")]
    public async Task RoundTrip_DataContractSerializer()
    {
        // arrange
        XmlMediaTypeFormatter formatter = new();
        XmlSampleModel original = new() { Id = 42, Name = "test" };

        // act
        object? result = await this.RoundTrip(original, typeof(XmlSampleModel), formatter);

        // assert
        XmlSampleModel deserialized = (XmlSampleModel)result!;
        Assert.That(deserialized.Id, Is.EqualTo(42));
        Assert.That(deserialized.Name, Is.EqualTo("test"));
    }

    [Test(Description = "Ensure that a string round-trips correctly with DataContractSerializer.")]
    public async Task RoundTrip_DataContractSerializer_String()
    {
        XmlMediaTypeFormatter formatter = new();
        object? result = await this.RoundTrip("hello", typeof(string), formatter);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test(Description = "Ensure that an integer round-trips correctly with DataContractSerializer.")]
    public async Task RoundTrip_DataContractSerializer_Int()
    {
        XmlMediaTypeFormatter formatter = new();
        object? result = await this.RoundTrip(42, typeof(int), formatter);
        Assert.That(result, Is.EqualTo(42));
    }

    /****
    ** Round-trip with XmlSerializer
    ****/
    [Test(Description = "Ensure that a model round-trips correctly with XmlSerializer.")]
    public async Task RoundTrip_XmlSerializer()
    {
        // arrange
        XmlMediaTypeFormatter formatter = new() { UseXmlSerializer = true };
        XmlSampleModel original = new() { Id = 42, Name = "test" };

        // act
        object? result = await this.RoundTrip(original, typeof(XmlSampleModel), formatter);

        // assert
        XmlSampleModel deserialized = (XmlSampleModel)result!;
        Assert.That(deserialized.Id, Is.EqualTo(42));
        Assert.That(deserialized.Name, Is.EqualTo("test"));
    }

    [Test(Description = "Ensure that a string round-trips correctly with XmlSerializer.")]
    public async Task RoundTrip_XmlSerializer_String()
    {
        XmlMediaTypeFormatter formatter = new() { UseXmlSerializer = true };
        object? result = await this.RoundTrip("hello", typeof(string), formatter);
        Assert.That(result, Is.EqualTo("hello"));
    }

    /****
    ** Error handling
    ****/
    [Test(Description = "Ensure that deserializing invalid XML throws.")]
    public void ReadFromStreamAsync_InvalidXml_Throws()
    {
        // arrange
        XmlMediaTypeFormatter formatter = new();
        byte[] bytes = Encoding.UTF8.GetBytes("not valid xml <<<");
        using MemoryStream stream = new(bytes);
        using StringContent content = new("");

        // act & assert
        Assert.ThrowsAsync<SerializationException>(async () =>
            await formatter.ReadFromStreamAsync(typeof(XmlSampleModel), stream, content, CancellationToken.None)
        );
    }

    [Test(Description = "Ensure that deserializing invalid XML throws with XmlSerializer.")]
    public void ReadFromStreamAsync_InvalidXml_XmlSerializer_Throws()
    {
        // arrange
        XmlMediaTypeFormatter formatter = new() { UseXmlSerializer = true };
        byte[] bytes = Encoding.UTF8.GetBytes("not valid xml <<<");
        using MemoryStream stream = new(bytes);
        using StringContent content = new("");

        // act & assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await formatter.ReadFromStreamAsync(typeof(XmlSampleModel), stream, content, CancellationToken.None)
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Serialize then deserialize a value through the formatter.</summary>
    private async Task<object?> RoundTrip(object value, Type type, XmlMediaTypeFormatter formatter)
    {
        using MemoryStream stream = new();
        using StringContent content = new("");

        await formatter.WriteToStreamAsync(type, value, stream, content, CancellationToken.None);
        stream.Position = 0;
        return await formatter.ReadFromStreamAsync(type, stream, content, CancellationToken.None);
    }


    /*********
    ** Test models
    *********/
    [DataContract]
    public class XmlSampleModel
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string? Name { get; set; }
    }
}
