using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;
using Pathoschild.Http.Client.Internal;

namespace Pathoschild.Http.Tests.Internal;

/// <summary>Unit tests verifying that <see cref="FormatterContent{T}"/> correctly constructs HTTP content.</summary>
[TestFixture]
public class FormatterContentTests
{
    /*********
    ** Unit tests
    *********/
    [Test(Description = "Ensure that the constructor throws when the formatter can't write the type.")]
    public void Constructor_ThrowsWhenFormatterCantWriteType()
    {
        // arrange
        PlainTextFormatter formatter = new();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => new FormatterContent<int>(42, formatter));
    }

    [Test(Description = "Ensure that Content-Type is set from the explicit media type.")]
    public void Constructor_SetsContentType_FromExplicitMediaType()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new();

        // act
        using FormatterContent<string> content = new("test", formatter, "text/json");

        // assert
        Assert.That(content.Headers.ContentType?.MediaType, Is.EqualTo("text/json"));
    }

    [Test(Description = "Ensure that Content-Type defaults to the formatter's first supported media type.")]
    public void Constructor_SetsContentType_DefaultsToFirstSupported()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new();

        // act
        using FormatterContent<string> content = new("test", formatter);

        // assert
        Assert.That(content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test(Description = "Ensure that Content-Type falls back to application/octet-stream when formatter has no supported types.")]
    public void Constructor_SetsContentType_FallsBackToOctetStream()
    {
        // arrange
        StubFormatter formatter = new();

        // act
        using FormatterContent<string> content = new("test", formatter);

        // assert
        Assert.That(content.Headers.ContentType?.MediaType, Is.EqualTo("application/octet-stream"));
    }

    [Test(Description = "Ensure that the content serializes to the stream correctly.")]
    public async Task SerializeToStreamAsync_WritesContent()
    {
        // arrange
        JsonMediaTypeFormatter formatter = new();
        using FormatterContent<string> content = new("hello", formatter);

        // act
        using MemoryStream stream = new();
        await content.CopyToAsync(stream);
        stream.Position = 0;
        string result = new StreamReader(stream).ReadToEnd();

        // assert
        Assert.That(result, Is.EqualTo("\"hello\""));
    }


    /*********
    ** Test helpers
    *********/
    /// <summary>A stub formatter with no supported media types.</summary>
    private class StubFormatter : IMediaTypeFormatter
    {
        public IReadOnlyCollection<string> SupportedMediaTypes { get; } = Array.Empty<string>();
        public bool CanReadType(Type type) { return true; }
        public bool CanWriteType(Type type) { return true; }

        public Task<object?> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(null);
        }

        public Task WriteToStreamAsync(Type type, object? value, Stream stream, HttpContent content, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
