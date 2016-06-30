using System;
using System.Net.Http;
using NUnit.Framework;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters
{
    /// <summary>Unit tests verifying that the <see cref="JsonNetFormatter"/> correctly formats content.</summary>
    /// <remarks>The JSON.NET serializer itself is thoroughly unit tested; these unit tests ensure that the media type formatter correctly handles the various use cases.</remarks>
    [TestFixture]
    public class JsonNetFormatterTests : FormatterTestsBase
    {
        /*********
        ** Unit tests
        *********/
        /***
        ** Json
        ***/
        [Test(Description = "A JSON value can be deserialized.")]
        [TestCase(typeof(bool), "true", ExpectedResult = true)]
        [TestCase(typeof(bool?), "true", ExpectedResult = true)]
        [TestCase(typeof(bool?), null, ExpectedResult = null)]
        [TestCase(typeof(int), "14", ExpectedResult = 14)]
        [TestCase(typeof(double), "4.2", ExpectedResult = 4.2d)]
        [TestCase(typeof(ConsoleColor), "'Black'", ExpectedResult = ConsoleColor.Black)]
        [TestCase(typeof(float), "4.2", ExpectedResult = 4.2f)]
        [TestCase(typeof(string), null, ExpectedResult = null)]
        [TestCase(typeof(string), "''", ExpectedResult = "")]
        [TestCase(typeof(string), "'   '", ExpectedResult = "   ")]
        [TestCase(typeof(string), "'example'", ExpectedResult = "example")]
        [TestCase(typeof(string), "'<example />'", ExpectedResult = "<example />")]
        [TestCase(typeof(string), "'exam\r\nple'", ExpectedResult = "exam\r\nple")]
        public object Json_Deserialize(Type type, string content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, "application/json");

            // verify
            return this.GetDeserialized(type, content, request, formatter);
        }

        [Test(Description = "A value can be serialized into JSON.")]
        [TestCase(typeof(bool?), true, ExpectedResult = "true")]
        [TestCase(typeof(bool?), null, ExpectedResult = "null")]
        [TestCase(typeof(string), null, ExpectedResult = "null")]
        [TestCase(typeof(string), "", ExpectedResult = "\"\"")]
        [TestCase(typeof(string), "   ", ExpectedResult = "\"   \"")]
        [TestCase(typeof(string), "example", ExpectedResult = "\"example\"")]
        [TestCase(typeof(string), "<example />", ExpectedResult = "\"<example />\"")]
        [TestCase(typeof(string), "exam\r\nple", ExpectedResult = "\"exam\\r\\nple\"")]
        public string Json_Serialize(Type type, object content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, type, "application/json");

            // verify
            return this.GetSerialized(content, request, formatter);
        }
    }
}
