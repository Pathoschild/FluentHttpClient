using System;
using System.Net.Http;
using NUnit.Framework;
using Pathoschild.Http.Formatters.Core;

namespace Pathoschild.Http.FluentClient.Tests.Formatters
{
	/// <summary>Unit tests verifying that the <see cref="PlainTextFormatter"/> correctly formats content.</summary>
	[TestFixture]
	public class PlainTextFormatterTests : FormatterTestsBase
	{
		/*********
		** Unit tests
		*********/
		[Test(Description = "Ensure that a string value can be read.")]
		[TestCase(null, Result = "")]
		[TestCase("", Result = "")]
		[TestCase("   ", Result = "   ")]
		[TestCase("example", Result = "example")]
		[TestCase("<example />", Result = "<example />")]
		[TestCase("exam\r\nple", Result = "exam\r\nple")]
		public object Deserialize_String(string content)
		{
			// set up
			PlainTextFormatter formatter = new PlainTextFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetDeserialized(typeof(string), content, request, formatter);
		}

		[Test(Description = "Ensure that a string value can be written.")]
		[TestCase(null, Result = "")]
		[TestCase("", Result = "")]
		[TestCase("   ", Result = "   ")]
		[TestCase("example", Result = "example")]
		[TestCase("<example />", Result = "<example />")]
		[TestCase("exam\r\nple", Result = "exam\r\nple")]
		public string Serialize_String(object content)
		{
			// set up
			PlainTextFormatter formatter = new PlainTextFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter);

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "Ensure that an IFormattable value can be written if AllowIrreversibleSerialization is true.")]
		[TestCase(typeof(double), 4.2d, Result = "4.2")]
		[TestCase(typeof(Enum), ConsoleColor.Black, Result = "Black")]
		[TestCase(typeof(float), 4.2F, Result = "4.2")]
		[TestCase(typeof(int), 42, Result = "42")]
		public string Serialize_IFormattable(Type type, object content)
		{
			// set up
			PlainTextFormatter formatter = new PlainTextFormatter { AllowIrreversibleSerialization = true };
			HttpRequestMessage request = this.GetRequest(content, formatter, type);

			// verify
			return this.GetSerialized(content, request, formatter);
		}

		[Test(Description = "Ensure that an IFormattable value cannot be written if AllowIrreversibleSerialization is false.")]
		[TestCase(typeof(double), 4.2d, ExpectedException = typeof(InvalidOperationException))]
		[TestCase(typeof(Enum), ConsoleColor.Black, ExpectedException = typeof(InvalidOperationException))]
		[TestCase(typeof(float), 4.2F, ExpectedException = typeof(InvalidOperationException))]
		[TestCase(typeof(int), 42, ExpectedException = typeof(InvalidOperationException))]
		public void Serialize_IFormattable_WithoutIrreversibleSerialization(Type type, object content)
		{
			// set up
			PlainTextFormatter formatter = new PlainTextFormatter();
			HttpRequestMessage request = this.GetRequest(content, formatter, type);

			// verify
			this.GetSerialized(content, request, formatter);
		}
	}
}
