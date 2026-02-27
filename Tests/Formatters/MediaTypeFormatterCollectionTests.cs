using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters;

/// <summary>Unit tests verifying that <see cref="MediaTypeFormatterCollection"/> correctly finds formatters.</summary>
[TestFixture]
public class MediaTypeFormatterCollectionTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** FindReader
    ****/
    [Test(Description = "Ensure that FindReader returns a matching formatter by media type.")]
    public void FindReader_ReturnsMatchingFormatter()
    {
        // arrange
        MediaTypeFormatterCollection collection = [];
        JsonMediaTypeFormatter jsonFormatter = new();
        XmlMediaTypeFormatter xmlFormatter = new();
        collection.Add(jsonFormatter);
        collection.Add(xmlFormatter);

        // act
        IMediaTypeFormatter? result = collection.FindReader("application/json");

        // assert
        Assert.That(result, Is.SameAs(jsonFormatter));
    }

    [Test(Description = "Ensure that FindReader returns null when no formatter matches.")]
    public void FindReader_ReturnsNull_WhenNoMatch()
    {
        // arrange
        MediaTypeFormatterCollection collection = [];
        collection.Add(new JsonMediaTypeFormatter());

        // act
        IMediaTypeFormatter? result = collection.FindReader("text/plain");

        // assert
        Assert.That(result, Is.Null);
    }

    /****
    ** FindWriter
    ****/
    [Test(Description = "Ensure that FindWriter returns a matching formatter by media type.")]
    public void FindWriter_ReturnsMatchingFormatter()
    {
        // arrange
        MediaTypeFormatterCollection collection = [];
        JsonMediaTypeFormatter jsonFormatter = new();
        XmlMediaTypeFormatter xmlFormatter = new();
        collection.Add(jsonFormatter);
        collection.Add(xmlFormatter);

        // act
        IMediaTypeFormatter? result = collection.FindWriter("application/xml");

        // assert
        Assert.That(result, Is.SameAs(xmlFormatter));
    }

    [Test(Description = "Ensure that FindWriter returns null when no formatter matches.")]
    public void FindWriter_ReturnsNull_WhenNoMatch()
    {
        // arrange
        MediaTypeFormatterCollection collection = [];
        collection.Add(new XmlMediaTypeFormatter());

        // act
        IMediaTypeFormatter? result = collection.FindWriter("application/json");

        // assert
        Assert.That(result, Is.Null);
    }
}
