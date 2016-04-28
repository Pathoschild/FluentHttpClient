using System;
using System.Net.Http;
using System.Net.Http.Headers;
using NUnit.Framework;
using Pathoschild.Http.Formatters.JsonNet;

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

        /***
        ** Jsonp
        ***/
        [Test(Description = "The formatter correctly deserializes JSONP without a callback.")]
        [TestCase("\"value\"", ExpectedResult = "value")]
        public string Jsonp_Deserialize_WithoutCallback(string content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, "application/javascript");

            // verify
            return (string)this.GetDeserialized(typeof(string), content, request, formatter);
        }

        [Test(Description = "The formatter correctly throws an exception if the JSONP content contains a callback.")]
        [TestCase("callback(\"value\")")]
        public void Jsonp_Deserialize_FailsWithCallback(string content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, "application/javascript");

            // verify
            Assert.Throws<NotSupportedException>(() => this.GetDeserialized(typeof(string), content, request, formatter));
        }

        [Test(Description = "A value can be serialized into JSONP.")]
        [TestCase(typeof(bool?), true, ExpectedResult = "callback(true)")]
        [TestCase(typeof(bool?), null, ExpectedResult = "callback(null)")]
        [TestCase(typeof(string), null, ExpectedResult = "callback(null)")]
        [TestCase(typeof(string), "", ExpectedResult = "callback(\"\")")]
        [TestCase(typeof(string), "   ", ExpectedResult = "callback(\"   \")")]
        [TestCase(typeof(string), "example", ExpectedResult = "callback(\"example\")")]
        [TestCase(typeof(string), "<example />", ExpectedResult = "callback(\"<example />\")")]
        [TestCase(typeof(string), "exam\r\nple", ExpectedResult = "callback(\"exam\\r\\nple\")")]
        public string Jsonp_Serialize(Type type, object content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, type, "application/javascript");

            // verify
            return this.GetSerialized(content, request, formatter);
        }

        [Test(Description = "The JSONP serialization respects the 'callback' query parameter to define the JavaScript method name.")]
        [TestCase("callback", 42, ExpectedResult = "callback(42)")]
        [TestCase("example", 42, ExpectedResult = "example(42)")]
        [TestCase("object.example", 42, ExpectedResult = "object.example(42)")]
        [TestCase("examples[14]", 42, ExpectedResult = "examples[14](42)")]
        public string Jsonp_Serialize_WithCustomCallbackMethod(string callbackMethod, int content)
        {
            // set up
            JsonNetFormatter formatter = new JsonNetFormatter();
            HttpRequestMessage request = this.GetRequest(content, formatter, typeof(int), "application/javascript");
            request.RequestUri = new Uri("http://example.org?callback=" + callbackMethod);
            formatter = (JsonNetFormatter)formatter.GetPerRequestFormatterInstance(typeof(int), request, new MediaTypeHeaderValue("application/javascript"));

            // verify
            return this.GetSerialized(content, request, formatter);
        }
    }
}
