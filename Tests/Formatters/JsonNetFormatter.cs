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
		[TestCase(typeof(bool), "true", Result = true)]
		[TestCase(typeof(bool?), "true", Result = true)]
		[TestCase(typeof(bool?), null, Result = null)]
		[TestCase(typeof(int), "14", Result = 14)]
		[TestCase(typeof(double), "4.2", Result = 4.2d)]
		[TestCase(typeof(ConsoleColor), "'Black'", Result = ConsoleColor.Black)]
		[TestCase(typeof(float), "4.2", Result = 4.2f)]
		[TestCase(typeof(string), null, Result = null)]
		[TestCase(typeof(string), "''", Result = "")]
		[TestCase(typeof(string), "'   '", Result = "   ")]
		[TestCase(typeof(string), "'example'", Result = "example")]
		[TestCase(typeof(string), "'<example />'", Result = "<example />")]
		[TestCase(typeof(string), "'exam\r\nple'", Result = "exam\r\nple")]
		public object Json_Deserialize(Type type, string content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, "application/json");

			// verify
			return this.GetDeserialized(type, content, request, formatter);
		}

		[Test(Description = "A value can be serialized into JSON.")]
		[TestCase(typeof(bool?), true, Result = "true")]
		[TestCase(typeof(bool?), null, Result = "null")]
		[TestCase(typeof(string), null, Result = "null")]
		[TestCase(typeof(string), "", Result = "\"\"")]
		[TestCase(typeof(string), "   ", Result = "\"   \"")]
		[TestCase(typeof(string), "example", Result = "\"example\"")]
		[TestCase(typeof(string), "<example />", Result = "\"<example />\"")]
		[TestCase(typeof(string), "exam\r\nple", Result = "\"exam\\r\\nple\"")]
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
		[Test(Description = "The formatter throws an exception if attempting to deserialize JSONP, which is a write-only format.")]
		[TestCase("value", ExpectedException = typeof(NotSupportedException))]
		public void Jsonp_Deserialize_Fails(string content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, "application/javascript");

			// verify
			this.GetDeserialized(typeof(string), content, request, formatter);
		}

		[Test(Description = "A value can be serialized into JSONP.")]
		[TestCase(typeof(bool?), true, Result = "callback(true)")]
		[TestCase(typeof(bool?), null, Result = "callback(null)")]
		[TestCase(typeof(string), null, Result = "callback(null)")]
		[TestCase(typeof(string), "", Result = "callback(\"\")")]
		[TestCase(typeof(string), "   ", Result = "callback(\"   \")")]
		[TestCase(typeof(string), "example", Result = "callback(\"example\")")]
		[TestCase(typeof(string), "<example />", Result = "callback(\"<example />\")")]
		[TestCase(typeof(string), "exam\r\nple", Result = "callback(\"exam\\r\\nple\")")]
		public string Jsonp_Serialize(Type type, object content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, type, "application/javascript");

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "The JSONP serialization respects the 'callback' query parameter to define the JavaScript method name.")]
		[TestCase("callback", 42, Result = "callback(42)")]
		[TestCase("example", 42, Result = "example(42)")]
		[TestCase("object.example", 42, Result = "object.example(42)")]
		[TestCase("examples[14]", 42, Result = "examples[14](42)")]
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
