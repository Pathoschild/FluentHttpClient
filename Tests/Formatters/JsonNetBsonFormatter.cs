using System.Net.Http;
using NUnit.Framework;
using Pathoschild.Http.Formatters.JsonNet;

namespace Pathoschild.Http.Tests.Formatters
{
    /// <summary>Unit tests verifying that the <see cref="JsonNetBsonFormatter"/> correctly formats content.</summary>
    /// <remarks>The JSON.NET serializer itself is thoroughly unit tested; these unit tests ensure that the media type formatter correctly handles the various use cases.</remarks>
    [TestFixture]
    public class JsonNetBsonFormatterTests : FormatterTestsBase
    {
        /*********
        ** Test objects
        *********/
        /// <summary>An example BSON-serializable object.</summary>
        public class ExampleObject
        {
            /// <summary>An example numeric value.</summary>
            public int Number { get; set; }

            /// <summary>An example string value.</summary>
            public string Text { get; set; }
        }


        /*********
        ** Unit tests
        *********/
        [Test(Description = "Ensure that a string value can be read.")]
        [TestCase("&\0\0\0Number\0*\0\0\0Text\0\v\0\0\0fourty-two\0\0")]
        public void Deserialize_Object(string content)
        {
            // set up
            JsonNetBsonFormatter formatter = new JsonNetBsonFormatter();
            ExampleObject expected = new ExampleObject { Number = 42, Text = "fourty-two" };
            HttpRequestMessage request = this.GetRequest(content, formatter);

            // execute
            ExampleObject result = (ExampleObject)this.GetDeserialized(typeof(ExampleObject), content, request, formatter);

            // verify
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Number, Is.EqualTo(expected.Number));
            Assert.That(result.Text, Is.EqualTo(expected.Text));
        }

        [Test(Description = "Ensure that an object value can be written.")]
        [TestCase(Result = "&\0\0\0Number\0*\0\0\0Text\0\v\0\0\0fourty-two\0\0")]
        public string Serialize_Object()
        {
            // set up
            JsonNetBsonFormatter formatter = new JsonNetBsonFormatter();
            ExampleObject expected = new ExampleObject { Number = 42, Text = "fourty-two" };
            HttpRequestMessage request = this.GetRequest(expected, formatter);

            // verify
            return this.GetSerialized(expected, request, formatter);
        }
    }
}