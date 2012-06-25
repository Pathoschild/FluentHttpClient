using System;
using System.Net.Http;
using System.Net.Http.Headers;
using NUnit.Framework;
using Pathoschild.Http.Formatters.JsonNet;

namespace Pathoschild.Http.Tests.Formatters
{
	/// <summary>Unit tests verifying that the <see cref="JsonNetJsonpFormatter"/> correctly formats content.</summary>
	/// <remarks>The JSON.NET serializer itself is thoroughly unit tested; these unit tests ensure that the media type formatter correctly handles the various use cases.</remarks>
	[TestFixture]
	public class JsonNetJsonpFormatterTests : FormatterTestsBase
	{
		/*********
		** Unit tests
		*********/
		[Test(Description = "Ensure that the formatter is write-only.")]
		[TestCase("ignored", ExpectedException = typeof(NotSupportedException))]
		public void WriteOnly(string content)
		{
			// set up
			JsonNetJsonpFormatter formatter = new JsonNetJsonpFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			this.GetDeserialized(typeof(string), content, request, formatter);
		}

		[Test(Description = "Ensure that a string value can be written.")]
		[TestCase(null, Result = "callback(null)")]
		[TestCase("", Result = "callback(\"\")")]
		[TestCase("   ", Result = "callback(\"   \")")]
		[TestCase("example", Result = "callback(\"example\")")]
		[TestCase("<example />", Result = "callback(\"<example />\")")]
		[TestCase("exam\r\nple", Result = "callback(\"exam\\r\\nple\")")]
		public string Serialize_String(object content)
		{
			// set up
			JsonNetJsonpFormatter formatter = new JsonNetJsonpFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "Ensure that a struct value can be written.")]
		[TestCase(typeof(bool?), true, Result = "callback(true)")]
		[TestCase(typeof(bool?), null, Result = "callback(null)")]
		public string Serialize_Struct(Type type, object content)
		{
			// set up
			JsonNetJsonpFormatter formatter = new JsonNetJsonpFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, type);

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "Ensure that a struct value can be written.")]
		[TestCase("callback", 42, Result = "callback(42)")]
		[TestCase("example", 42, Result = "example(42)")]
		[TestCase("object.example", 42, Result = "object.example(42)")]
		[TestCase("examples[14]", 42, Result = "examples[14](42)")]
		public string Serialize_WithCustomCallbackMethod(string callbackMethod, int content)
		{
			// set up
			JsonNetJsonpFormatter formatter = new JsonNetJsonpFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, typeof(int));
			request.RequestUri = new Uri("http://example.org?callback=" + callbackMethod);
			formatter = (JsonNetJsonpFormatter)formatter.GetPerRequestFormatterInstance(typeof(int), request, new MediaTypeHeaderValue("application/javascript"));

			// verify
			return this.GetSerialized(content, request, formatter);
		}
	}
}
