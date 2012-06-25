using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Default;

namespace Pathoschild.Http.Tests.Default
{
	/// <summary>Integration tests verifying that the default <see cref="RequestBuilder"/> correctly creates and alters the underlying objects.</summary>
	[TestFixture]
	public class ResponseTests
	{
		/*********
		** Objects
		*********/
		/// <summary>An example type which can be serialized into and deserialized from HTTP message bodies.</summary>
		/// <typeparam name="T">The example property value type.</typeparam>
		public class Model<T>
		{
			/*********
			** Accessors
			*********/
			/// <summary>An example property value.</summary>
			public T Value { get; set; }


			/*********
			** Public methods
			*********/
			/// <summary>Construct an instance.</summary>
			public Model() { }

			/// <summary>Construct an instance.</summary>
			/// <param name="value">An example property value.</param>
			public Model(T value)
			{
				this.Value = value;
			}
		}


		/*********
		** Unit tests
		*********/
		/***
		** Construct
		***/
		[Test(Description = "Ensure that the response is constructed with the expected initial state.")]
		[TestCase("DELETE", "object content")]
		[TestCase("GET", "object content")]
		[TestCase("HEAD", "object content")]
		[TestCase("PUT", "object content")]
		[TestCase("OPTIONS", "object content")]
		[TestCase("POST", "object content")]
		[TestCase("TRACE", "object content")]
		public void Construct(string methodName, string content)
		{
			// execute & test
			this
				.ConstructResponse(content, method: methodName, inconclusiveOnFailure: false);
		}

		[Test(Description = "The response is constructed with the expected initial state when the content is a model.")]
		[TestCase("DELETE", "object content")]
		[TestCase("GET", "object content")]
		[TestCase("HEAD", "object content")]
		[TestCase("PUT", "object content")]
		[TestCase("OPTIONS", "object content")]
		[TestCase("POST", "object content")]
		[TestCase("TRACE", "object content")]
		public void Construct_WithModel(string methodName, string content)
		{
			// execute & test
			this
				.ConstructResponseForModel(content, method: methodName, inconclusiveOnFailure: false);
		}

		/***
		** AsMessage
		***/
		[Test(Description = "The response can block the current thread without altering the underlying HttpResponseMessage.")]
		[TestCase("model value")]
		public void Wait(string content)
		{
			// set up
			HttpResponseMessage message;

			// execute
			HttpResponseMessage actual = this
				.ConstructResponse(content, out message)
				.Wait()
				.AsMessage();

			// test
			Assert.That(actual, Is.Not.Null, "response message");
			Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
		}

		[Test(Description = "The response can return the underlying HttpResponseMessage.")]
		[TestCase("model value")]
		public void AsMessage(string content)
		{
			// set up
			HttpResponseMessage message;

			// execute
			HttpResponseMessage actual = this
				.ConstructResponse(content, out message)
				.AsMessage();

			// test
			Assert.That(actual, Is.Not.Null, "response message");
			Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
		}

		[Test(Description = "The response can return the underlying HttpResponseMessage when the response content is a model.")]
		[TestCase("model value")]
		public void AsMessage_OfModel(string content)
		{
			// set up
			HttpResponseMessage message;

			// execute
			HttpResponseMessage actual = this
				.ConstructResponseForModel(content, out message)
				.AsMessage();

			// test
			Assert.That(actual, Is.Not.Null, "response message");
			Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
		}

		[Test(Description = "The response can asynchronously return the underlying HttpResponseMessage.")]
		[TestCase("model value")]
		public void AsMessageAsync(string content)
		{
			// set up
			HttpResponseMessage message;

			// execute
			HttpResponseMessage actual = this
				.ConstructResponse(content, out message)
				.AsMessage();

			// test
			Assert.That(actual, Is.Not.Null, "response message");
			Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
		}

		[Test(Description = "The response can asynchronously return the underlying HttpResponseMessage when the response content is a model.")]
		[TestCase("model value")]
		public void AsMessageAsync_OfModel(string content)
		{
			// set up
			HttpResponseMessage message;

			// execute
			HttpResponseMessage actual = this
				.ConstructResponseForModel(content, out message)
				.AsMessageAsync()
				.VerifyTaskResult();

			// test
			Assert.That(actual, Is.Not.Null, "response message");
			Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
		}

		/***
		** As
		***/
		[Test(Description = "The response can be read as a deserialized model.")]
		[TestCase("model value")]
		public void As(string content)
		{
			// execute
			Model<string> actual = this
				.ConstructResponseForModel(content)
				.As<Model<string>>();

			// test
			Assert.That(actual, Is.Not.Null, "deserialized model");
			Assert.That(actual.Value, Is.EqualTo(content), "deserialized model property");
		}

		[Test(Description = "The response can be asynchronously read as a deserialized model.")]
		[TestCase("model value", Result = "model value")]
		public string AsAsync(string content)
		{
			// execute
			Model<string> actual = this
				.ConstructResponseForModel(content)
				.AsAsync<Model<string>>()
				.VerifyTaskResult();

			// test
			Assert.That(actual, Is.Not.Null, "deserialized model");
			return actual.Value;
		}

		/***
		** AsByteArray
		***/
		[Test(Description = "The response can be read as a byte array.")]
		[TestCase("model value", Result = "\"model value\"")]
		public string AsByteArray(string content)
		{
			// execute
			byte[] actual = this
				.ConstructResponse(content)
				.AsByteArray();

			// test
			Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
			return Encoding.UTF8.GetString(actual);
		}

		[Test(Description = "The response can be read as a byte array when the content is a model.")]
		[TestCase("model value", Result = "{\"Value\":\"model value\"}")]
		public string AsByteArray_OfModel(string content)
		{
			// execute
			byte[] actual = this
				.ConstructResponseForModel(content)
				.AsByteArray();

			// test
			Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
			return Encoding.UTF8.GetString(actual);
		}

		[Test(Description = "The response can be asynchronously read as a byte array.")]
		[TestCase("model value", Result = "\"model value\"")]
		public string AsByteArrayAsync(string content)
		{
			// execute
			byte[] actual = this
				.ConstructResponse(content)
				.AsByteArrayAsync()
				.VerifyTaskResult();

			// test
			Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
			return Encoding.UTF8.GetString(actual);
		}

		[Test(Description = "The response can be asynchronously read as a byte array when the content is a model.")]
		[TestCase("model value", Result = "{\"Value\":\"model value\"}")]
		public string AsByteArrayAsync_OfModel(string content)
		{
			// execute
			byte[] actual = this
				.ConstructResponseForModel(content)
				.AsByteArrayAsync()
				.VerifyTaskResult();

			// test
			Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
			return Encoding.UTF8.GetString(actual);
		}

		/***
		** AsList
		***/
		[Test(Description = "The response can be read as a deserialized list of models.")]
		[TestCase("model value A", "model value B")]
		public void AsList(string contentA, string contentB)
		{
			// set up
			List<Model<string>> expected = new List<Model<string>> { new Model<string>(contentA), new Model<string>(contentB) };

			// execute
			List<Model<string>> actual = this
				.ConstructResponse(expected)
				.AsList<Model<string>>();

			// assert
			Assert.That(actual, Is.EquivalentTo(expected));
		}

		[Test(Description = "The response can be read as a deserialized list of models.")]
		[TestCase("model value A", "model value B")]
		public void AsListAsync(string contentA, string contentB)
		{
			// set up
			List<Model<string>> expected = new List<Model<string>> { new Model<string>(contentA), new Model<string>(contentB) };

			// execute
			List<Model<string>> actual = this
				.ConstructResponse(expected)
				.AsListAsync<Model<string>>()
				.VerifyTaskResult();

			// assert
			Assert.That(actual, Is.EquivalentTo(expected));
		}

		/***
		** AsStream
		***/
		[Test(Description = "The response can be read as a stream.")]
		[TestCase("stream content")]
		public void AsStream(string content)
		{
			// execute
			string actual;
			using (Stream stream = this.ConstructResponse(content).AsStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				actual = reader.ReadToEnd();
				reader.Close();
				reader.Dispose();
			}

			// assert
			Assert.That(actual, Is.EqualTo(String.Format("\"{0}\"", content)));
		}

		[Test(Description = "The response can be read as a stream when the content is a model.")]
		[TestCase("stream content")]
		public void AsStream_OfModel(string content)
		{
			// execute
			string actual;
			using (Stream stream = this.ConstructResponseForModel(content).AsStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				actual = reader.ReadToEnd();
			}

			// assert
			Assert.That(actual, Is.EqualTo(String.Format("{{\"Value\":\"{0}\"}}", content)));
		}

		/*********
		** AsString
		*********/
		[Test(Description = "The response can be read as a string.")]
		[TestCase("stream content")]
		public void AsString(string content)
		{
			// execute
			string actual = this
				.ConstructResponse(content)
				.AsString();

			// assert
			Assert.That(actual, Is.EqualTo('"' + content + '"'));
		}

		[Test(Description = "The response can be read as a string when the content is a model.")]
		[TestCase("stream content")]
		public void AsString_OfModel(string content)
		{
			// execute
			string actual = this
				.ConstructResponseForModel(content)
				.AsString();

			// assert
			Assert.That(actual, Is.EqualTo("{\"Value\":\"stream content\"}"));
		}

		[Test(Description = "The response can be asynchronously read as a string.")]
		[TestCase("stream content")]
		public void AsStringAsync(string content)
		{
			// execute
			string actual = this
				.ConstructResponse(content)
				.AsStringAsync()
				.VerifyTaskResult();

			// assert
			Assert.That(actual, Is.EqualTo('"' + content + '"'));
		}

		[Test(Description = "The response can be asynchronously read as a string when the content is a model.")]
		[TestCase("stream content")]
		public void AsStringAsync_OfModel(string content)
		{
			// execute
			string actual = this
				.ConstructResponseForModel(content)
				.AsStringAsync()
				.VerifyTaskResult();

			// assert
			Assert.That(actual, Is.EqualTo("{\"Value\":\"stream content\"}"));
		}

		/***
		** ThrowError
		***/
		[Test(Description = "An error is thrown when the response contains an error.")]
		[TestCase(false, HttpStatusCode.OK)]
		[TestCase(false, HttpStatusCode.Accepted)]
		[TestCase(false, HttpStatusCode.NoContent)]
		[TestCase(false, HttpStatusCode.NotFound)]
		[TestCase(true, HttpStatusCode.OK)]
		[TestCase(true, HttpStatusCode.Accepted)]
		[TestCase(true, HttpStatusCode.NoContent)]
		[TestCase(true, HttpStatusCode.NotFound, ExpectedException = typeof(ApiException))]
		public void ThrowError(bool throwError, HttpStatusCode status)
		{
			// execute
			IResponse response = this.ConstructResponse("", throwApiError: throwError, status: status);
			response.AsString();
		}

		/*********
		** Protected methods
		*********/
		/// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
		/// <param name="method">The HTTP request method.</param>
		/// <param name="responseMessage">The constructed response message.</param>
		/// <param name="content">The HTTP response body content.</param>
		/// <param name="status">The HTTP response status code.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
		/// <param name="throwApiError">Whether the response should throw an API error if the HTTP response contains an error.</param>
		/// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
		/// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
		protected IResponse ConstructResponse<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool throwApiError = true, bool inconclusiveOnFailure = true)
		{
			try
			{
				// set up
				HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(method), uri);
				responseMessage = requestMessage.CreateResponse(status);
				responseMessage.Content = new ObjectContent<T>(content, new JsonMediaTypeFormatter());

				// execute
				var message = responseMessage;
				Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() => message);
				IResponse response = new Response(requestMessage, task, new MediaTypeFormatterCollection(), throwApiError);

				//// verify
				this.AssertEqual(response.Request, method, uri);
				return response;
			}
			catch (AssertionException exc)
			{
				if (inconclusiveOnFailure)
					Assert.Inconclusive("The response could not be constructed: {0}", exc.Message);
				throw;
			}
		}

		/// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
		/// <param name="method">The HTTP request method.</param>
		/// <param name="content">The HTTP response body content.</param>
		/// <param name="status">The HTTP response status code.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
		/// <param name="throwApiError">Whether the response should throw an API error if the HTTP response contains an error.</param>
		/// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
		/// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
		protected IResponse ConstructResponse<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool throwApiError = true, bool inconclusiveOnFailure = true)
		{
			HttpResponseMessage message;
			return this.ConstructResponse<T>(content, out message, method, status, uri, throwApiError, inconclusiveOnFailure);
		}

		/// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
		/// <param name="method">The HTTP request method.</param>
		/// <param name="responseMessage">The constructed response message.</param>
		/// <param name="content">The HTTP response body content.</param>
		/// <param name="status">The HTTP response status code.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
		/// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
		/// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
		protected IResponse ConstructResponseForModel<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
		{
			Model<T> model = new Model<T>(content);
			return this.ConstructResponse<Model<T>>(model, out responseMessage, method, status, uri, inconclusiveOnFailure);
		}

		/// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
		/// <param name="method">The HTTP request method.</param>
		/// <param name="content">The HTTP response body content.</param>
		/// <param name="status">The HTTP response status code.</param>
		/// <param name="uri">The expected URI.</param>
		/// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
		/// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
		/// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
		protected IResponse ConstructResponseForModel<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
		{
			HttpResponseMessage message;
			return this.ConstructResponseForModel<T>(content, out message, method, status, uri, inconclusiveOnFailure);
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

	public static class TaskExtensions
	{
		public static T VerifyTaskResult<T>(this Task<T> task)
		{
			Assert.That(task, Is.Not.Null, "The asynchronous task is invalid.");
			Assert.That(task.IsCanceled, Is.False, "The asynchronous task was cancelled.");
			Assert.That(task.IsFaulted, Is.False, "The asynchronous task is faulted.");

			task.Wait();
			Assert.That(task.IsCanceled, Is.False, "The asynchronous task was cancelled.");
			Assert.That(task.IsFaulted, Is.False, "The asynchronous task is faulted.");

			return task.Result;
		}
	}
}
