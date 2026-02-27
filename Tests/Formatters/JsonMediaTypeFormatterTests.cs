using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters;

/// <summary>Unit tests verifying that the <see cref="JsonMediaTypeFormatter"/> correctly serializes and deserializes content.</summary>
[TestFixture]
public class JsonMediaTypeFormatterTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** SupportedMediaTypes
    ****/
    [Test(Description = "Ensure that the formatter supports 'application/json'.")]
    public void SupportedMediaTypes_ContainsApplicationJson()
    {
        JsonMediaTypeFormatter formatter = new();
        Assert.That(formatter.SupportedMediaTypes, Does.Contain("application/json"));
    }

    [Test(Description = "Ensure that the formatter supports 'text/json'.")]
    public void SupportedMediaTypes_ContainsTextJson()
    {
        JsonMediaTypeFormatter formatter = new();
        Assert.That(formatter.SupportedMediaTypes, Does.Contain("text/json"));
    }

    /****
    ** CanReadType / CanWriteType
    ****/
    [Test(Description = "Ensure that CanReadType returns true for all types.")]
    [TestCase(typeof(string))]
    [TestCase(typeof(int))]
    [TestCase(typeof(SampleModel))]
    public void CanReadType_ReturnsTrue(Type type)
    {
        JsonMediaTypeFormatter formatter = new();
        Assert.That(formatter.CanReadType(type), Is.True);
    }

    [Test(Description = "Ensure that CanWriteType returns true for all types.")]
    [TestCase(typeof(string))]
    [TestCase(typeof(int))]
    [TestCase(typeof(SampleModel))]
    public void CanWriteType_ReturnsTrue(Type type)
    {
        JsonMediaTypeFormatter formatter = new();
        Assert.That(formatter.CanWriteType(type), Is.True);
    }

    /****
    ** Round-trip serialization
    ****/
    [Test(Description = "Ensure that a string value round-trips correctly.")]
    [TestCase("hello")]
    [TestCase("")]
    [TestCase("with \"quotes\" and \\slashes")]
    public async Task RoundTrip_String(string value)
    {
        JsonMediaTypeFormatter formatter = new();
        object? result = await this.RoundTrip(value, typeof(string), formatter);
        Assert.That(result, Is.EqualTo(value));
    }

    [Test(Description = "Ensure that integer values round-trip correctly.")]
    [TestCase(0)]
    [TestCase(42)]
    [TestCase(-1)]
    public async Task RoundTrip_Int(int value)
    {
        JsonMediaTypeFormatter formatter = new();
        object? result = await this.RoundTrip(value, typeof(int), formatter);
        Assert.That(result, Is.EqualTo(value));
    }

    [Test(Description = "Ensure that boolean values round-trip correctly.")]
    [TestCase(true)]
    [TestCase(false)]
    public async Task RoundTrip_Bool(bool value)
    {
        JsonMediaTypeFormatter formatter = new();
        object? result = await this.RoundTrip(value, typeof(bool), formatter);
        Assert.That(result, Is.EqualTo(value));
    }

    [Test(Description = "Ensure that a complex object round-trips correctly.")]
    public async Task RoundTrip_ComplexObject()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new();
        SampleModel original = new() { Id = 42, Name = "test", Nested = new SampleNested { Value = "inner" } };

        // act
        object? result = await this.RoundTrip(original, typeof(SampleModel), formatter);

        // assert
        SampleModel deserialized = (SampleModel)result!;
        Assert.That(deserialized.Id, Is.EqualTo(42));
        Assert.That(deserialized.Name, Is.EqualTo("test"));
        Assert.That(deserialized.Nested, Is.Not.Null);
        Assert.That(deserialized.Nested!.Value, Is.EqualTo("inner"));
    }

    [Test(Description = "Ensure that null values round-trip correctly.")]
    public async Task RoundTrip_Null()
    {
        JsonMediaTypeFormatter formatter = new();

        using MemoryStream stream = new();
        using StringContent content = new("");

        await formatter.WriteToStreamAsync(typeof(SampleModel), null, stream, content, CancellationToken.None);
        stream.Position = 0;

        object? result = await formatter.ReadFromStreamAsync(typeof(SampleModel), stream, content, CancellationToken.None);
        Assert.That(result, Is.Null);
    }

    /****
    ** Custom settings
    ****/
    [Test(Description = "Ensure that custom JsonSerializerSettings are applied.")]
    public async Task CustomSettings_NullValueHandlingIgnore()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new()
        {
            SerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        };
        SampleModel model = new() { Id = 1, Name = null };

        // act
        using MemoryStream stream = new();
        using StringContent content = new("");
        await formatter.WriteToStreamAsync(typeof(SampleModel), model, stream, content, CancellationToken.None);

        // assert
        stream.Position = 0;
        string json = new StreamReader(stream).ReadToEnd();
        Assert.That(json, Does.Not.Contain("Name"));
    }

    /****
    ** Error handling
    ****/
    [Test(Description = "Ensure that deserializing invalid JSON throws.")]
    public void ReadFromStreamAsync_InvalidJson_Throws()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new();
        byte[] bytes = Encoding.UTF8.GetBytes("not valid json {{{");
        using MemoryStream stream = new(bytes);
        using StringContent content = new("");

        // act & assert
        Assert.ThrowsAsync<JsonReaderException>(async () =>
            await formatter.ReadFromStreamAsync(typeof(SampleModel), stream, content, CancellationToken.None)
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Serialize then deserialize a value through the formatter.</summary>
    private async Task<object?> RoundTrip(object value, Type type, JsonMediaTypeFormatter formatter)
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
    public class SampleModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public SampleNested? Nested { get; set; }
    }

    public class SampleNested
    {
        public string? Value { get; set; }
    }
}
