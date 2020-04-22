using System;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;

namespace Pathoschild.Http.Tests.Client
{
    /// <summary>Integration tests verifying that the default <see cref="Pathoschild.Http.Client"/> correctly creates and alters the underlying objects.</summary>
    [TestFixture]
    public class ClientTests
    {
        /*********
        ** Unit tests
        *********/
        [Test(Description = "Ensure that the client is constructed with the expected initial state.")]
        [TestCase("http://base-url/")]
        public void Construct(string uri)
        {
            this.ConstructClient(uri, false);
        }

        [Test(Description = "Ensure that the fluent client disposes its own HTTP client.")]
        [TestCase("http://base-url/")]
        public void Dispose_DisposesOwnClient(string uri)
        {
            // act
            IClient client = this.ConstructClient(uri);
            client.Dispose();

            // assert
            Assert.Throws<ObjectDisposedException>(() => client.BaseClient.GetAsync(""));
        }

        [Test(Description = "Ensure that the fluent client does not dispose the HTTP client if it was passed in.")]
        [TestCase("http://base-url/")]
        public void Dispose_DoesNotDisposeInjectedClient(string uri)
        {
            // act
            using HttpClient httpClient = new HttpClient();
            IClient fluentClient = this.ConstructClient(uri, httpClient: httpClient);
            fluentClient.Dispose();

            // assert
            try
            {
                httpClient.GetAsync("");
            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("The HTTP client was incorrectly disposed by the client.");
            }
        }

        [Test(Description = "Ensure the user agent header is populated by default.")]
        public void SetUserAgent_HasDefaultValue()
        {
            // act
            IClient client = this.ConstructClient();

            // assert
            string userAgent = client.BaseClient.DefaultRequestHeaders.UserAgent.ToString();
            Console.WriteLine("user agent: " + userAgent);
            if (string.IsNullOrWhiteSpace(userAgent))
                Assert.Fail("The client has no default user agent.");
        }

        [Test(Description = "Ensure the user agent header is populated with the given value.")]
        public void SetUserAgent_UsesValue()
        {
            // arrange
            const string sampleValue = "example user agent";

            // act
            IClient client = this.ConstructClient();
            client.SetUserAgent(sampleValue);

            // assert
            string userAgent = client.BaseClient.DefaultRequestHeaders.UserAgent.ToString();
            Assert.AreEqual(sampleValue, userAgent);
        }

        [Test(Description = "Ensure that all specified defaults are correctly applied.")]
        public void AddDefault()
        {
            // arrange
            const string expectedUserAgent = "boop";

            // act
            IClient client = this.ConstructClient()
                .AddDefault(req => req.WithHeader("User-Agent", expectedUserAgent))
                .AddDefault(req => req.WithArgument("boop", 1));
            IRequest request = client.GetAsync("example");

            // assert
            string userAgent = request.Message.Headers.UserAgent.ToString();
            Assert.AreEqual(expectedUserAgent, userAgent, "The user agent header does not match the specified default.");
            Assert.AreEqual("/example?boop=1", request.Message.RequestUri.PathAndQuery, "The URL arguments don't match the specified default.");
        }

        [Test(Description = "Ensure that the HTTP DELETE method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Delete(string resource)
        {
            // act
            IRequest request = this.ConstructClient().DeleteAsync("resource");

            // assert
            this.AssertEqual(request, HttpMethod.Delete, resource);
        }

        [Test(Description = "Ensure that the HTTP GET method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Get(string resource)
        {
            // act
            IRequest request = this.ConstructClient().GetAsync("resource");

            // assert
            this.AssertEqual(request, HttpMethod.Get, resource);
        }

        [Test(Description = "Ensure that the HTTP POST method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Post(string resource)
        {
            // act
            IRequest request = this.ConstructClient().PostAsync("resource");

            // assert
            this.AssertEqual(request, HttpMethod.Post, resource);
        }

        [Test(Description = "Ensure that the HTTP POST method with a body constructs a request message with the expected initial state.")]
        [TestCase("resource", "value")]
        public async Task Post_WithBody(string resource, string value)
        {
            // act
            IRequest request = this.ConstructClient().PostAsync("resource", value);

            // assert
            this.AssertEqual(request, HttpMethod.Post, resource);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + value + '"'), "The message request body is invalid.");
        }

        [Test(Description = "Ensure that the HTTP PUT method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Put(string resource)
        {
            // act
            IRequest request = this.ConstructClient().PutAsync("resource");

            // assert
            this.AssertEqual(request, HttpMethod.Put, resource);
        }

        [Test(Description = "Ensure that the HTTP PUT method with a body constructs a request message with the expected initial state.")]
        [TestCase("resource", "value")]
        public async Task Put_WithBody(string resource, string value)
        {
            // act
            IRequest request = this.ConstructClient().PutAsync("resource", value);

            // assert
            this.AssertEqual(request, HttpMethod.Put, resource);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + value + '"'), "The message request body is invalid.");
        }

        [Test(Description = "Ensure that an arbitrary HTTP request message is passed on with the expected initial state.")]
        [TestCase("DELETE", "resource")]
        [TestCase("GET", "resource")]
        [TestCase("HEAD", "resource")]
        [TestCase("PUT", "resource")]
        [TestCase("OPTIONS", "resource")]
        [TestCase("POST", "resource")]
        [TestCase("TRACE", "resource")]
        public void Send(string methodName, string resource)
        {
            // arrange
            HttpMethod method = this.ConstructMethod(methodName);

            // act
            IRequest request = this.ConstructClient().SendAsync(method, "resource");

            // assert
            this.AssertEqual(request, method, resource);
        }

        [Test(Description = "Ensure that an arbitrary HTTP request message is passed on with the expected initial state.")]
        [TestCase("DELETE", "resource")]
        [TestCase("GET", "resource")]
        [TestCase("HEAD", "resource")]
        [TestCase("PUT", "resource")]
        [TestCase("OPTIONS", "resource")]
        [TestCase("POST", "resource")]
        [TestCase("TRACE", "resource")]
        public void Send_WithMessage(string methodName, string resource)
        {
            // arrange
            HttpMethod method = this.ConstructMethod(methodName);
            HttpRequestMessage message = new HttpRequestMessage(method, resource);

            // act
            IRequest request = this.ConstructClient().SendAsync(message);

            // assert
            this.AssertEqual(request, method, resource, baseUri: "");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Construct an HTTP method for the method name.</summary>
        /// <param name="methodName">The name of the method.</param>
        /// <remarks><see cref="HttpMethod"/> is not an enumeration, so it cannot be used as a unit test input parameter.</remarks>
        private HttpMethod ConstructMethod(string methodName)
        {
            return new HttpMethod(methodName);
        }

        /// <summary>Construct an <see cref="IClient"/> instance and assert that its initial state is valid.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
        /// <param name="httpClient">The underlying HTTP client.</param>
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        private IClient ConstructClient(string baseUri = "http://example.com/", bool inconclusiveOnFailure = true, HttpClient? httpClient = null)
        {
            try
            {
                // act
                IClient client = new FluentClient(baseUri, httpClient);

                // assert
                Assert.NotNull(client.BaseClient, "The base client is null.");
                Assert.AreEqual(baseUri, client.BaseClient.BaseAddress.ToString(), "The base path is invalid.");

                return client;
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
        /// <param name="resource">The expected relative URI.</param>
        /// <param name="baseUri">The expected base URI of the request.</param>
        private void AssertEqual(IRequest request, HttpMethod method, string resource, string baseUri = "http://example.com/")
        {
            // not null
            Assert.That(request, Is.Not.Null, "The request is null.");
            Assert.That(request.Message, Is.Not.Null, "The request message is null.");

            // message state
            HttpRequestMessage message = request.Message;
            Assert.That(message.Method, Is.EqualTo(method), "The request method is invalid.");
            Assert.That(message.RequestUri.ToString(), Is.EqualTo(baseUri + resource), "The message URI is invalid.");
        }
    }
}
