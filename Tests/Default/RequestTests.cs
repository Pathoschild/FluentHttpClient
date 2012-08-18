using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Default;

namespace Pathoschild.Http.Tests.Default
{
	/// <summary>Integration tests verifying that the default <see cref="Request"/> correctly creates and alters the underlying objects.</summary>
	[TestFixture]
	public class RequestTests
	{
		/*********
		** Unit tests
		*********/
		[Test(Description = "Ensure that the request builder is constructed with the expected initial state.")]
		[TestCase("DELETE", "resource")]
		[TestCase("GET", "resource")]
		[TestCase("HEAD", "resource")]
		[TestCase("PUT", "resource")]
		[TestCase("OPTIONS", "resource")]
		[TestCase("POST", "resource")]
		[TestCase("TRACE", "resource")]
		public void Construct(string methodName, string uri)
		{
			this.ConstructRequest(methodName, uri, false);
		}

		[Test(Description = "Ensure that WithArgument appends the query arguments to the request message and does not incorrectly alter request state.")]
		[TestCase("DELETE", "keyA", "24", "keyB", "42")]
		[TestCase("GET", "keyA", "24", "keyB", "42")]
		[TestCase("HEAD", "keyA", "24", "keyB", "42")]
		[TestCase("PUT", "keyA", "24", "keyB", "42")]
		[TestCase("OPTIONS", "keyA", "24", "keyB", "42")]
		[TestCase("POST", "keyA", "24", "keyB", "42")]
		[TestCase("TRACE", "keyA", "24", "keyB", "42")]
		public void WithArgument(string methodName, string keyA, string valueA, string keyB, string valueB)
		{
			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithArgument(keyA, valueA)
				.WithArgument(keyB, valueB);

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			NameValueCollection arguments = request.Message.RequestUri.ParseQueryString();
			Assert.That(arguments[keyA], Is.Not.Null.And.EqualTo(valueA), "The first key=>value pair is invalid.");
			Assert.That(arguments[keyB], Is.Not.Null.And.EqualTo(valueB), "The second key=>value pair is invalid.");
		}

		[Test(Description = "Ensure that WithBodyContent sets the request body and does not incorrectly alter request state.")]
		[TestCase("DELETE", "body value")]
		[TestCase("GET", "body value")]
		[TestCase("HEAD", "body value")]
		[TestCase("PUT", "body value")]
		[TestCase("OPTIONS", "body value")]
		[TestCase("POST", "body value")]
		[TestCase("TRACE", "body value")]
		public void WithBodyContent(string methodName, object body)
		{
			// set up
			HttpContent content = new ObjectContent(typeof(string), body, new JsonMediaTypeFormatter());

			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithBodyContent(content);

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
		}

		[Test(Description = "Ensure that WithBody sets the request body and does not incorrectly alter request state.")]
		[TestCase("DELETE", "body value")]
		[TestCase("GET", "body value")]
		[TestCase("HEAD", "body value")]
		[TestCase("PUT", "body value")]
		[TestCase("OPTIONS", "body value")]
		[TestCase("POST", "body value")]
		[TestCase("TRACE", "body value")]
		public void WithBody(string methodName, object body)
		{
			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithBody(body);

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
		}

		[Test(Description = "Ensure that WithBody sets the request body and does not incorrectly alter request state.")]
		[TestCase("DELETE", "body value")]
		[TestCase("GET", "body value")]
		[TestCase("HEAD", "body value")]
		[TestCase("PUT", "body value")]
		[TestCase("OPTIONS", "body value")]
		[TestCase("POST", "body value")]
		[TestCase("TRACE", "body value")]
		public void WithBody_AndFormatter(string methodName, object body)
		{
			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithBody(body, new JsonMediaTypeFormatter());

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
		}

		[Test(Description = "Ensure that WithCustom persists the custom changes and does not incorrectly alter request state.")]
		[TestCase("DELETE", "body value")]
		[TestCase("GET", "body value")]
		[TestCase("HEAD", "body value")]
		[TestCase("PUT", "body value")]
		[TestCase("OPTIONS", "body value")]
		[TestCase("POST", "body value")]
		[TestCase("TRACE", "body value")]
		public void WithCustom(string methodName, string customBody)
		{
			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithCustom(r => r.Content = new ObjectContent<string>(customBody, new JsonMediaTypeFormatter()));

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + customBody + '"'), "The customized message body is invalid.");
		}

		[Test(Description = "Ensure that WithHeader sets the expected header and does not incorrectly alter request state.")]
		[TestCase("DELETE", "VIA", "header value")]
		[TestCase("GET", "VIA", "header value")]
		[TestCase("HEAD", "VIA", "header value")]
		[TestCase("PUT", "VIA", "header value")]
		[TestCase("OPTIONS", "VIA", "header value")]
		[TestCase("POST", "VIA", "header value")]
		[TestCase("TRACE", "VIA", "header value")]
		public void WithHeader(string methodName, string key, string value)
		{
			// execute
			IRequest request = this
				.ConstructRequest(methodName)
				.WithHeader(key, value);
			var header = request.Message.Headers.FirstOrDefault(p => p.Key == key);

			// verify
			this.AssertEqual(request.Message, methodName, ignoreArguments: true);
			Assert.That(header, Is.Not.Null, "The header is invalid.");
			Assert.That(header.Value, Is.Not.Null.Or.Empty, "The header value is invalid.");
			Assert.That(header.Value.First(), Is.EqualTo(value), "The header value is invalid.");
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an <see cref="IRequest"/> instance and assert that its initial state is valid.</summary>
		/// <param name="methodName">The expected HTTP method.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
		/// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
		/// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
		protected IRequest ConstructRequest(string methodName, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
		{
			try
			{
				// set up
				HttpMethod method = new HttpMethod(methodName);
				HttpRequestMessage message = new HttpRequestMessage(method, uri);

				// execute
				IRequest request = new Request(message, new MediaTypeFormatterCollection(), r => new Task<HttpResponseMessage>(() => null));

				// verify
				this.AssertEqual(request.Message, method, uri);

				return request;
			}
			catch (AssertionException exc)
			{
				if (inconclusiveOnFailure)
					Assert.Inconclusive("The client could not be constructed: {0}", exc.Message);
				throw;
			}
		}

		/// <summary>Assert that an HTTP request's state matches the expected values.</summary>
		/// <param name="request">The HTTP request message to verify.</param>
		/// <param name="method">The expected HTTP method.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
		protected void AssertEqual(HttpRequestMessage request, HttpMethod method, string uri = "http://example.org/", bool ignoreArguments = false)
		{
			Assert.That(request, Is.Not.Null, "The request message is null.");
			Assert.That(request.Method, Is.EqualTo(method), "The request method is invalid.");
			Assert.That(ignoreArguments ? request.RequestUri.GetLeftPart(UriPartial.Path) : request.RequestUri.ToString(), Is.EqualTo(uri), "The request URI is invalid.");
		}

		/// <summary>Assert that an HTTP request's state matches the expected values.</summary>
		/// <param name="request">The HTTP request message to verify.</param>
		/// <param name="method">The expected HTTP method.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
		protected void AssertEqual(HttpRequestMessage request, string method, string uri = "http://example.org/", bool ignoreArguments = false)
		{
			this.AssertEqual(request, new HttpMethod(method), uri, ignoreArguments);
		}
	}
}
