using System;
using System.Net.Http;
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
		[Test(Description = "Ensure that a string value can be read.")]
		[TestCase(null, Result = null)]
		[TestCase("''", Result = "")]
		[TestCase("'   '", Result = "   ")]
		[TestCase("'example'", Result = "example")]
		[TestCase("'<example />'", Result = "<example />")]
		[TestCase("'exam\r\nple'", Result = "exam\r\nple")]
		public string Deserialize_String(string content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return (string)this.GetDeserialized(typeof(string), content, request, formatter);
		}

		[Test(Description = "Ensure that a scalar value can be read.")]
		[TestCase(typeof(bool), "true", Result = true)]
		[TestCase(typeof(int), "14", Result = 14)]
		[TestCase(typeof(double), "4.2", Result = 4.2d)]
		[TestCase(typeof(ConsoleColor), "'Black'", Result = ConsoleColor.Black)]
		[TestCase(typeof(float), "4.2", Result = 4.2f)]
		public object Deserialize_Scalar(Type type, string content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetDeserialized(type, content, request, formatter);
		}

		[Test(Description = "Ensure that a struct value can be read.")]
		[TestCase(typeof(bool?), "true", Result = true)]
		[TestCase(typeof(bool?), null, Result = null)]
		public object Deserialize_Struct(Type type, string content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetDeserialized(type, content, request, formatter);
		}

		[Test(Description = "Ensure that a string value can be written.")]
		[TestCase(null, Result = "null")]
		[TestCase("", Result = "\"\"")]
		[TestCase("   ", Result = "\"   \"")]
		[TestCase("example", Result = "\"example\"")]
		[TestCase("<example />", Result = "\"<example />\"")]
		[TestCase("exam\r\nple", Result = "\"exam\\r\\nple\"")]
		public string Serialize_String(object content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "Ensure that a struct value can be written.")]
		[TestCase(typeof(bool?), true, Result = "true")]
		[TestCase(typeof(bool?), null, Result = "null")]
		public string Serialize_Struct(Type type, object content)
		{
			// set up
			JsonNetFormatter formatter = new JsonNetFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, type);

			// verify
			return this.GetSerialized(content, request, formatter);
		}
	}
}
