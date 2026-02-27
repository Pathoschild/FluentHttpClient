using System;
using System.Net.Http;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters;

/// <summary>Unit tests verifying that the <see cref="PlainTextFormatter"/> correctly formats content.</summary>
[TestFixture]
public class PlainTextFormatterTests : FormatterTestsBase
{
    /*********
    ** Unit tests
    *********/
    [Test(Description = "Ensure that a string value can be read.")]
    [TestCase("", ExpectedResult = "")]
    [TestCase("   ", ExpectedResult = "   ")]
    [TestCase("example", ExpectedResult = "example")]
    [TestCase("<example />", ExpectedResult = "<example />")]
    [TestCase("exam\r\nple", ExpectedResult = "exam\r\nple")]
    public object Deserialize_String(string? content)
    {
        // arrange
        PlainTextFormatter formatter = new();
        HttpRequestMessage request = this.GetRequest(content, formatter);

        // assert
        return this.GetDeserialized(typeof(string), content, request, formatter);
    }

    [Test(Description = "Ensure that a string value can be written.")]
    [TestCase("", ExpectedResult = "")]
    [TestCase("   ", ExpectedResult = "   ")]
    [TestCase("example", ExpectedResult = "example")]
    [TestCase("<example />", ExpectedResult = "<example />")]
    [TestCase("exam\r\nple", ExpectedResult = "exam\r\nple")]
    public string Serialize_String(string? content)
    {
        // arrange
        PlainTextFormatter formatter = new();
        HttpRequestMessage request = this.GetRequest(content, formatter);

        // assert
        return this.GetSerialized(content, request, formatter);
    }

    [Test(Description = "Ensure that an IFormattable value can be written if AllowIrreversibleSerialization is true.")]
    [TestCase(typeof(double), 4.2d, ExpectedResult = "4.2")]
    [TestCase(typeof(Enum), ConsoleColor.Black, ExpectedResult = "Black")]
    [TestCase(typeof(float), 4.2F, ExpectedResult = "4.2")]
    [TestCase(typeof(int), 42, ExpectedResult = "42")]
    public string Serialize_IFormattable(Type type, object content)
    {
        // arrange
        PlainTextFormatter formatter = new() { AllowIrreversibleSerialization = true };
        HttpRequestMessage request = this.GetRequest(content, formatter, type);

        // assert
        return this.GetSerialized(content, request, formatter);
    }

    [Test(Description = "Ensure that an IFormattable value cannot be written if AllowIrreversibleSerialization is false.")]
    [TestCase(typeof(double), 4.2d)]
    [TestCase(typeof(Enum), ConsoleColor.Black)]
    [TestCase(typeof(float), 4.2F)]
    [TestCase(typeof(int), 42)]
    public void Serialize_IFormattable_WithoutIrreversibleSerialization(Type type, object content)
    {
        // arrange
        PlainTextFormatter formatter = new();

        // assert
        Assert.Throws<InvalidOperationException>(() => this.GetRequest(content, formatter, type));
    }

    /****
    ** CanReadType
    ****/
    [Test(Description = "Ensure that CanReadType returns true for string.")]
    public void CanReadType_String_ReturnsTrue()
    {
        PlainTextFormatter formatter = new();
        Assert.That(formatter.CanReadType(typeof(string)), Is.True);
    }

    [Test(Description = "Ensure that CanReadType returns false for int.")]
    public void CanReadType_Int_ReturnsFalse()
    {
        PlainTextFormatter formatter = new();
        Assert.That(formatter.CanReadType(typeof(int)), Is.False);
    }

    [Test(Description = "Ensure that CanReadType returns false for object.")]
    public void CanReadType_Object_ReturnsFalse()
    {
        PlainTextFormatter formatter = new();
        Assert.That(formatter.CanReadType(typeof(object)), Is.False);
    }

    /****
    ** CanWriteType
    ****/
    [Test(Description = "Ensure that CanWriteType returns true for string.")]
    public void CanWriteType_String_ReturnsTrue()
    {
        PlainTextFormatter formatter = new();
        Assert.That(formatter.CanWriteType(typeof(string)), Is.True);
    }

    [Test(Description = "Ensure that CanWriteType returns false for IFormattable when AllowIrreversibleSerialization is false.")]
    public void CanWriteType_IFormattable_ReturnsFalse_WhenNotAllowed()
    {
        PlainTextFormatter formatter = new();
        Assert.That(formatter.CanWriteType(typeof(int)), Is.False);
    }

    [Test(Description = "Ensure that CanWriteType returns true for IFormattable when AllowIrreversibleSerialization is true.")]
    public void CanWriteType_IFormattable_ReturnsTrue_WhenAllowed()
    {
        PlainTextFormatter formatter = new() { AllowIrreversibleSerialization = true };
        Assert.That(formatter.CanWriteType(typeof(int)), Is.True);
    }

    /****
    ** Null value
    ****/
    [Test(Description = "Ensure that null value serializes to empty string.")]
    public void Serialize_NullValue()
    {
        // arrange
        PlainTextFormatter formatter = new();
        HttpRequestMessage request = this.GetRequest("placeholder", formatter);

        // act
        string result = this.GetSerialized<string?>(null, request, formatter);

        // assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }
}