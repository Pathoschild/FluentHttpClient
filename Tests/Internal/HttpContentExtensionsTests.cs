using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;
using Pathoschild.Http.Client.Internal;

namespace Pathoschild.Http.Tests.Internal;

/// <summary>Unit tests verifying that <see cref="HttpContentExtensions"/> correctly reads content using formatters.</summary>
[TestFixture]
public class HttpContentExtensionsTests
{
    /*********
    ** Unit tests
    *********/
    [Test(Description = "Ensure that ReadAsAsync matches a formatter by Content-Type header.")]
    public async Task ReadAsAsync_MatchesByContentType()
    {
        // arrange
        string json = "{\"Id\":1,\"Name\":\"test\"}";
        using HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        MediaTypeFormatterCollection formatters = [new JsonMediaTypeFormatter(), new XmlMediaTypeFormatter()];

        // act
        SampleModel result = await content.ReadAsAsync<SampleModel>(formatters);

        // assert
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("test"));
    }

    [Test(Description = "Ensure that ReadAsAsync falls back to first formatter that can read the type when no Content-Type match.")]
    public async Task ReadAsAsync_FallsBackToFirstReadableFormatter()
    {
        // arrange
        string json = "{\"Id\":2,\"Name\":\"fallback\"}";
        using ByteArrayContent content = new(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/custom-type");
        MediaTypeFormatterCollection formatters = [new JsonMediaTypeFormatter()];

        // act
        SampleModel result = await content.ReadAsAsync<SampleModel>(formatters);

        // assert
        Assert.That(result.Id, Is.EqualTo(2));
        Assert.That(result.Name, Is.EqualTo("fallback"));
    }

    [Test(Description = "Ensure that ReadAsAsync throws when no formatter can read the type.")]
    public void ReadAsAsync_ThrowsWhenNoFormatterCanRead()
    {
        // arrange
        using HttpContent content = new StringContent("hello", Encoding.UTF8, "text/plain");
        PlainTextFormatter plainText = new();
        MediaTypeFormatterCollection formatters = [plainText];

        // act & assert (PlainTextFormatter can only read strings, not SampleModel)
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await content.ReadAsAsync<SampleModel>(formatters)
        );
    }

    [Test(Description = "Ensure that ReadAsAsync handles null Content-Type gracefully.")]
    public async Task ReadAsAsync_HandlesNullContentType()
    {
        // arrange
        string json = "{\"Id\":3,\"Name\":\"noheader\"}";
        using ByteArrayContent content = new(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = null;
        MediaTypeFormatterCollection formatters = [new JsonMediaTypeFormatter()];

        // act
        SampleModel result = await content.ReadAsAsync<SampleModel>(formatters);

        // assert
        Assert.That(result.Id, Is.EqualTo(3));
        Assert.That(result.Name, Is.EqualTo("noheader"));
    }


    /*********
    ** Test models
    *********/
    public class SampleModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
