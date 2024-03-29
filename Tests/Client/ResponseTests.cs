using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pathoschild.Http.Client;
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
            public T? Value { get; set; }


            /*********
            ** Public methods
            *********/
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


        /*********
        ** Retrieval
        *********/
        /****
        ** AsMessage
        ****/
        [Test(Description = "The response can asynchronously return the underlying HttpResponseMessage.")]
        [TestCase("model value")]
        public void AsMessage(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(content, out HttpResponseMessage message);

            // act
            HttpResponseMessage actual = response.Message;

            // assert
            Assert.That(actual, Is.Not.Null, "response message");
            Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
        }

        [Test(Description = "The response can asynchronously return the underlying HttpResponseMessage when the response content is a model.")]
        [TestCase("model value")]
        public void AsMessage_OfModel(string content)
        {
            // arrange
            IResponse response = this.ConstructResponseForModel(content, out HttpResponseMessage message);

            // act
            HttpResponseMessage actual = response.Message;

            // assert
            Assert.That(actual, Is.Not.Null, "response message");
            Assert.That(actual.ToString(), Is.EqualTo(message.ToString()), "response message");
        }

        /****
        ** As
        ****/
        [Test(Description = "The response can be asynchronously read as a deserialized model.")]
        [TestCase("model value", ExpectedResult = "model value")]
        public async Task<string?> As(string content)
        {
            // arrange
            IResponse response = this.ConstructResponseForModel(content);

            // act
            Model<string> actual = await response
                .As<Model<string>>()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.Not.Null, "deserialized model");
            return actual.Value;
        }

        /****
        ** AsArray
        ****/
        [Test(Description = "The response can be read as a deserialized list of models.")]
        [TestCase("model value A", "model value B")]
        public async Task AsArray(string contentA, string contentB)
        {
            // arrange
            Model<string>[] expected = { new(contentA), new(contentB) };
            IResponse response = this.ConstructResponse(expected);

            // act
            Model<string>[] actual = await response
                .AsArray<Model<string>>()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        /****
        ** AsByteArray
        ****/
        [Test(Description = "The response can be asynchronously read as a byte array.")]
        [TestCase("model value", ExpectedResult = "\"model value\"")]
        public async Task<string> AsByteArray(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(content);

            // act
            byte[] actual = await response
                .AsByteArray()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.Not.Null.And.Not.Empty, "byte array");
            return Encoding.UTF8.GetString(actual);
        }

        [Test(Description = "The response can be asynchronously read as a byte array when the content is a model.")]
        [TestCase("model value", ExpectedResult = "{\"Value\":\"model value\"}")]
        public async Task<string> AsByteArray_OfModel(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            byte[] actual = await response
                .AsByteArray()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.Not.Null.And.Not.Empty, "byte array");
            return Encoding.UTF8.GetString(actual);
        }

        /***
        ** AsStream
        ***/
        [Test(Description = "The response can be read as a stream.")]
        [TestCase("stream content")]
        public async Task AsStream(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(content);
            string actual;

            // act
            using (Stream stream = await response.AsStream().VerifyTaskResultAsync())
            using (StreamReader reader = new(stream))
                actual = await reader.ReadToEndAsync();

            // assert
            Assert.That(actual, Is.EqualTo($"\"{content}\""));
        }

        [Test(Description = "The response can be read as a stream when the content is a model.")]
        [TestCase("stream content")]
        public async Task AsStream_OfModel(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));
            string actual;

            // act
            using (Stream stream = await response.AsStream().VerifyTaskResultAsync())
            using (StreamReader reader = new(stream))
                actual = await reader.ReadToEndAsync();

            // assert
            Assert.That(actual, Is.EqualTo($"{{\"Value\":\"{content}\"}}"));
        }

        /***
        ** AsString
        ***/
        [Test(Description = "The response can be asynchronously read as a string.")]
        [TestCase("stream content")]
        public async Task AsString(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(content);

            // act
            string actual = await response
                .AsString()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.EqualTo($"\"{content}\""));
        }

        [Test(Description = "The response can be asynchronously read as a string multiple times.")]
        [TestCase("stream content")]
        public async Task AsString_MultipleReads(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(content);

            // act
            string actualA = await response
                .AsString()
                .VerifyTaskResultAsync();
            string actualB = await response
                .AsString()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actualA, Is.EqualTo($"\"{content}\""), "The content is not equal to the input.");
            Assert.That(actualA, Is.EqualTo(actualB), "The second read returned a different result.");
        }

        [Test(Description = "The response can be asynchronously read as a string when the content is a model.")]
        [TestCase("stream content")]
        public async Task AsString_OfModel(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            string actual = await response
                .AsString()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(actual, Is.EqualTo($"{{\"Value\":\"{content}\"}}"));
        }

        /***
        ** AsRawJson
        ***/
        [Test(Description = "The response can be read as a raw JSON token.")]
        [TestCase("content")]
        public async Task AsRawJson(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            JToken json = await response
                .AsRawJson()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(json.Value<string>("Value"), Is.EqualTo(content));
        }

        [Test(Description = "The response can be read as a raw JSON token with dynamic.")]
        [TestCase("content")]
        public async Task AsRawJson_Dynamic(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            dynamic json = await response
                .AsRawJsonObject()
                .VerifyTaskResultAsync();

            // assert
            Assert.That((string)json.Value, Is.EqualTo(content));
        }

        /***
        ** AsRawJsonObject
        ***/
        [Test(Description = "The response can be read as a raw JSON object.")]
        [TestCase("content")]
        public async Task AsRawJsonObject(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            JToken json = await response
                .AsRawJson()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(json.Value<string>("Value"), Is.EqualTo(content));
        }

        [Test(Description = "The response can be read as a raw JSON object with dynamic.")]
        [TestCase("content")]
        public async Task AsRawJsonObject_Dynamic(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new Model<string>(content));

            // act
            dynamic json = await response
                .AsRawJsonObject()
                .VerifyTaskResultAsync();

            // assert
            Assert.That((string)json.Value, Is.EqualTo(content));
        }

        /***
        ** AsRawJsonArray
        ***/
        [Test(Description = "The response can be read as a raw JSON array.")]
        [TestCase("content")]
        public async Task AsRawJsonArray(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new[] { new Model<string>(content) });

            // act
            JToken json = await response
                .AsRawJsonArray()
                .VerifyTaskResultAsync();

            // assert
            Assert.That(json.First?.Value<string>("Value"), Is.EqualTo(content));
        }

        [Test(Description = "The response can be read as a raw JSON array with dynamic.")]
        [TestCase("content")]
        public async Task AsRawJsonArray_Dynamic(string content)
        {
            // arrange
            IResponse response = this.ConstructResponse(new[] { new Model<string>(content) });

            // act
            dynamic json = await response
                .AsRawJsonArray()
                .VerifyTaskResultAsync();

            // assert
            Assert.That((string)json.First.Value, Is.EqualTo(content));
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Construct an <see cref="IResponse"/> instance and assert that its initial state is valid.</summary>
        /// <param name="method">The HTTP request method.</param>
        /// <param name="responseMessage">The constructed response message.</param>
        /// <param name="content">The HTTP response body content.</param>
        /// <param name="status">The HTTP response status code.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        private IResponse ConstructResponse<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            try
            {
                // construct response
                HttpRequestMessage requestMessage = new(new HttpMethod(method), uri);
                responseMessage = requestMessage.CreateResponse(status);
                responseMessage.Content = new ObjectContent<T>(content, new JsonMediaTypeFormatter());
                IResponse response = new Response(responseMessage, new MediaTypeFormatterCollection());

                // verify
                this.AssertEqual(responseMessage.RequestMessage, method, uri);
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
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        private IResponse ConstructResponse<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            return this.ConstructResponse(content, out _, method, status, uri, inconclusiveOnFailure);
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
        private IResponse ConstructResponseForModel<T>(T content, out HttpResponseMessage responseMessage, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            Model<T> model = new(content);
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
        private IResponse ConstructResponseForModel<T>(T content, string method = "GET", HttpStatusCode status = HttpStatusCode.OK, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            return this.ConstructResponseForModel(content, out _, method, status, uri, inconclusiveOnFailure);
        }

        /// <summary>Assert that an HTTP request's state matches the expected values.</summary>
        /// <param name="request">The HTTP request message to verify.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
        private void AssertEqual(HttpRequestMessage? request, HttpMethod method, string uri = "http://example.org/", bool ignoreArguments = false)
        {
            Assert.That(request, Is.Not.Null, "The request message is null.");
            Assert.That(request!.Method, Is.EqualTo(method), "The request method is invalid.");
            Assert.That(request.RequestUri, Is.Not.Null, "The request URI is null.");
            Assert.That(ignoreArguments ? $"{request.RequestUri!.Scheme}://{request.RequestUri.Authority}{request.RequestUri.AbsolutePath}" : request.RequestUri!.ToString(), Is.EqualTo(uri), "The request URI is invalid.");
        }

        /// <summary>Assert that an HTTP request's state matches the expected values.</summary>
        /// <param name="request">The HTTP request message to verify.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
        private void AssertEqual(HttpRequestMessage? request, string method, string uri = "http://example.org/", bool ignoreArguments = false)
        {
            this.AssertEqual(request, new HttpMethod(method), uri, ignoreArguments);
        }
    }

    /// <summary>Provides extension methods for response tests.</summary>
    public static class TaskExtensions
    {
        /// <summary>Assert that a task isn't broken and return its result.</summary>
        /// <typeparam name="T">The task result type.</typeparam>
        /// <param name="task">The task to verify.</param>
        public static async Task<T> VerifyTaskResultAsync<T>(this Task<T>? task)
        {
            Assert.That(task, Is.Not.Null, "The asynchronous task is invalid.");
            Assert.That(task!.IsCanceled, Is.False, "The asynchronous task was cancelled.");
            Assert.That(task.IsFaulted, Is.False, "The asynchronous task is faulted.");

            task.Wait();
            Assert.That(task.IsCanceled, Is.False, "The asynchronous task was cancelled.");
            Assert.That(task.IsFaulted, Is.False, "The asynchronous task is faulted.");

            return await task;
        }
    }
}
