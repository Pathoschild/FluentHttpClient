using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;

namespace Pathoschild.Http.Tests.Client
{
    /// <summary>Integration tests verifying that the default <see cref="Request"/> correctly creates and alters the underlying objects.</summary>
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
            this.ConstructResponse(content, method: methodName, inconclusiveOnFailure: false);
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
            this.ConstructResponseForModel(content, method: methodName, inconclusiveOnFailure: false);
        }

        /***
        ** Infrastructure
        ***/
        [Test(Description = "An appropriate exception is thrown when the task faults or aborts. This is regardless of configuration.")]
        [TestCase(true, typeof(NotSupportedException))]
        [TestCase(false, typeof(NotSupportedException))]
        public void Task_Async_FaultHandled(bool throwError, Type exceptionType)
        {
            // arrange
            IResponse response = this.ConstructResponseFromTask(() => { throw (Exception)Activator.CreateInstance(exceptionType); });

            // act
            Assert.ThrowsAsync<NotSupportedException>(async () => await response.AsString());
        }

        [Test(Description = "The asynchronous methods really are asynchronous.")]
        public void Task_Async_IsAsync()
        {
            // arrange
            IRequest request = this.ConstructResponseFromTask(() =>
            {
                Thread.Sleep(5000);
                Assert.Fail("The response was not invoked asynchronously.");
                return null;
            });

            // act
            Task<HttpResponseMessage> result = request.AsMessage();

            // assert
            Assert.AreNotEqual(result.Status, TaskStatus.Created);
            Assert.False(result.IsCompleted, "The response was not invoked asynchronously.");
        }

        [Test(Description = "The response succeeds when passed a HTTP request that is in progress.")]
        public void Task_Async()
        {
            // arrange
            IRequest request = this.ConstructResponseFromTask(() => new HttpResponseMessage(HttpStatusCode.OK));

            // act
            HttpResponseMessage result = request.AsMessage().Result;

            // assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessStatusCode);
        }


        /*********
        ** Retrieval
        *********/
        [Test(Description = "The response can asynchronously return the underlying HttpResponseMessage.")]
        [TestCase("model value")]
        public void AsMessage(string content)
        {
            // arrange
            HttpResponseMessage message;
            IRequest request = this.ConstructResponse(content, out message);

            // act
            HttpResponseMessage actual = request
                .AsMessage()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.Not.Null, "response message");
            Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
        }

        [Test(Description = "The response can asynchronously return the underlying HttpResponseMessage when the response content is a model.")]
        [TestCase("model value")]
        public void AsMessage_OfModel(string content)
        {
            // arrange
            HttpResponseMessage message;
            IRequest response = this.ConstructResponseForModel(content, out message);

            // act
            HttpResponseMessage actual = response
                .AsMessage()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.Not.Null, "response message");
            Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
        }

        [Test(Description = "The response can be asynchronously read as a deserialized model.")]
        [TestCase("model value", ExpectedResult = "model value")]
        public string As(string content)
        {
            // arrange
            IRequest response = this.ConstructResponseForModel(content);

            // act
            Model<string> actual = response
                .As<Model<string>>()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.Not.Null, "deserialized model");
            return actual.Value;
        }

        [Test(Description = "The response can be asynchronously read as a byte array.")]
        [TestCase("model value", ExpectedResult = "\"model value\"")]
        public string AsByteArray(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(content);

            // act
            byte[] actual = response
                .AsByteArray()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
            return Encoding.UTF8.GetString(actual);
        }

        [Test(Description = "The response can be asynchronously read as a byte array when the content is a model.")]
        [TestCase("model value", ExpectedResult = "{\"Value\":\"model value\"}")]
        public string AsByteArray_OfModel(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(new Model<string>(content));

            // act
            byte[] actual = response
                .AsByteArray()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.Not.Null.Or.Empty, "byte array");
            return Encoding.UTF8.GetString(actual);
        }

        [Test(Description = "The response can be read as a deserialized list of models.")]
        [TestCase("model value A", "model value B")]
        public void AsList(string contentA, string contentB)
        {
            // arrange
            List<Model<string>> expected = new List<Model<string>> { new Model<string>(contentA), new Model<string>(contentB) };
            IRequest response = this.ConstructResponse(expected);

            // act
            List<Model<string>> actual = response
                .AsList<Model<string>>()
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
            // arrange
            IRequest response = this.ConstructResponse(content);
            string actual;

            // act
            using (Stream stream = response.AsStream().VerifyTaskResult())
            using (StreamReader reader = new StreamReader(stream))
                actual = reader.ReadToEnd();

            // assert
            Assert.That(actual, Is.EqualTo(String.Format("\"{0}\"", content)));
        }

        [Test(Description = "The response can be read as a stream when the content is a model.")]
        [TestCase("stream content")]
        public void AsStream_OfModel(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(new Model<string>(content));
            string actual;

            // act
            using (Stream stream = response.AsStream().VerifyTaskResult())
            using (StreamReader reader = new StreamReader(stream))
                actual = reader.ReadToEnd();

            // assert
            Assert.That(actual, Is.EqualTo(String.Format("{{\"Value\":\"{0}\"}}", content)));
        }

        [Test(Description = "The response can be asynchronously read as a string.")]
        [TestCase("stream content")]
        public void AsString(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(content);

            // act
            string actual = response
                .AsString()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.EqualTo('"' + content + '"'));
        }

        [Test(Description = "The response can be asynchronously read as a string multiple times.")]
        [TestCase("stream content")]
        public void AsString_MultipleReads(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(content);

            // act
            string actualA = response
                .AsString()
                .VerifyTaskResult();
            string actualB = response
                .AsString()
                .VerifyTaskResult();

            // assert
            Assert.That(actualA, Is.EqualTo('"' + content + '"'), "The content is not equal to the input.");
            Assert.That(actualA, Is.EqualTo(actualB), "The second read returned a different result.");
        }

        [Test(Description = "The response can be asynchronously read as a string when the content is a model.")]
        [TestCase("stream content")]
        public void AsString_OfModel(string content)
        {
            // arrange
            IRequest response = this.ConstructResponse(new Model<string>(content));

            // act
            string actual = response
                .AsString()
                .VerifyTaskResult();

            // assert
            Assert.That(actual, Is.EqualTo("{\"Value\":\"stream content\"}"));
        }

        /***
        ** RaiseErrors
        ***/
        [Test(Description = "No error is thrown when the response is successful.")]
        [TestCase(false, HttpStatusCode.OK)]
        [TestCase(false, HttpStatusCode.Accepted)]
        [TestCase(false, HttpStatusCode.NoContent)]
        [TestCase(false, HttpStatusCode.NotFound)]
        [TestCase(true, HttpStatusCode.OK)]
        [TestCase(true, HttpStatusCode.Accepted)]
        [TestCase(true, HttpStatusCode.NoContent)]
        public void OnSuccess_NoErrorThrown(bool throwError, HttpStatusCode status)
        {
            // arrange
            IResponse response = this.ConstructResponse("", throwApiError: throwError, status: status);

            // act
            response.AsString().Wait();
        }

        [Test(Description = "An error is thrown when the response contains an error.")]
        [TestCase(true, HttpStatusCode.NotFound)]
        public void OnError_ErrorThrown(bool throwError, HttpStatusCode status)
        {
            // arrange
            IResponse response = this.ConstructResponse("", throwApiError: throwError, status: status);

            // act
            Assert.ThrowsAsync<ApiException>(async () => await response.AsString());
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an <see cref="IResponse"/> instance around an asynchronous task.</summary>
        /// <remarks>The asynchronous task to wrap.</remarks>
        protected IRequest ConstructResponseFromTask(Task<HttpResponseMessage> task)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/");
            return new Request(request, new MediaTypeFormatterCollection(), p => task, new IHttpFilter[0]);
        }

        /// <summary>Construct an <see cref="IResponse"/> instance around an asynchronous task.</summary>
        /// <remarks>The work to start in a new asynchronous task.</remarks>
        protected IRequest ConstructResponseFromTask(Func<HttpResponseMessage> task)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/");
            return new Request(request, new MediaTypeFormatterCollection(), p => Task<HttpResponseMessage>.Factory.StartNew(task), new IHttpFilter[0]);
        }

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
        protected IRequest ConstructResponse<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool throwApiError = true, bool inconclusiveOnFailure = true)
        {
            try
            {
                // construct message
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(method), uri);
                responseMessage = requestMessage.CreateResponse(status);
                responseMessage.Content = new ObjectContent<T>(content, new JsonMediaTypeFormatter());

                // construct request
                var message = responseMessage;
                Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() => message);
                IHttpFilter[] filters = throwApiError
                    ? new IHttpFilter[] { new DefaultErrorFilter() }
                    : new IHttpFilter[0];
                IRequest request = new Request(requestMessage, new MediaTypeFormatterCollection(), p => task, filters);

                // verify
                this.AssertEqual(request.Message, method, uri);
                return request;
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
        protected IRequest ConstructResponse<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool throwApiError = true, bool inconclusiveOnFailure = true)
        {
            HttpResponseMessage message;
            return this.ConstructResponse(content, out message, method, status, uri, throwApiError, inconclusiveOnFailure);
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
        protected IRequest ConstructResponseForModel<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            Model<T> model = new Model<T>(content);
            return this.ConstructResponse(model, out responseMessage, method, status, uri, inconclusiveOnFailure);
        }

        /// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
        /// <param name="method">The HTTP request method.</param>
        /// <param name="content">The HTTP response body content.</param>
        /// <param name="status">The HTTP response status code.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        protected IRequest ConstructResponseForModel<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            HttpResponseMessage message;
            return this.ConstructResponseForModel(content, out message, method, status, uri, inconclusiveOnFailure);
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
