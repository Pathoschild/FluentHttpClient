using System.Net.Http;
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

        [Test(Description = "Ensure that the HTTP DELETE method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Delete(string resource)
        {
            // execute
            IRequest request = this.ConstructClient().DeleteAsync("resource");

            // verify
            this.AssertEqual(request, HttpMethod.Delete, resource);
        }

        [Test(Description = "Ensure that the HTTP GET method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Get(string resource)
        {
            // execute
            IRequest request = this.ConstructClient().GetAsync("resource");

            // verify
            this.AssertEqual(request, HttpMethod.Get, resource);
        }

        [Test(Description = "Ensure that the HTTP POST method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Post(string resource)
        {
            // execute
            IRequest request = this.ConstructClient().PostAsync("resource");

            // verify
            this.AssertEqual(request, HttpMethod.Post, resource);
        }

        [Test(Description = "Ensure that the HTTP POST method with a body constructs a request message with the expected initial state.")]
        [TestCase("resource", "value")]
        public void Post_WithBody(string resource, string value)
        {
            // execute
            IRequest request = this.ConstructClient().PostAsync("resource", value);

            // verify
            this.AssertEqual(request, HttpMethod.Post, resource);
            Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + value + '"'), "The message request body is invalid.");
        }

        [Test(Description = "Ensure that the HTTP PUT method constructs a request message with the expected initial state.")]
        [TestCase("resource")]
        public void Put(string resource)
        {
            // execute
            IRequest request = this.ConstructClient().PutAsync("resource");

            // verify
            this.AssertEqual(request, HttpMethod.Put, resource);
        }

        [Test(Description = "Ensure that the HTTP PUT method with a body constructs a request message with the expected initial state.")]
        [TestCase("resource", "value")]
        public void Put_WithBody(string resource, string value)
        {
            // execute
            IRequest request = this.ConstructClient().PutAsync("resource", value);

            // verify
            this.AssertEqual(request, HttpMethod.Put, resource);
            Assert.That(request.Message.Content.ReadAsStringAsync().Result, Is.EqualTo('"' + value + '"'), "The message request body is invalid.");
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
            // set up
            HttpMethod method = this.ConstructMethod(methodName);

            // execute
            IRequest request = this.ConstructClient().SendAsync(method, "resource");

            // verify
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
            // set up
            HttpMethod method = this.ConstructMethod(methodName);
            HttpRequestMessage message = new HttpRequestMessage(method, resource);

            // execute
            IRequest request = this.ConstructClient().SendAsync(message);

            // verify
            this.AssertEqual(request, method, resource, baseUri: "");
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an HTTP method for the method name.</summary>
        /// <param name="methodName">The name of the method.</param>
        /// <remarks><see cref="HttpMethod"/> is not an enumeration, so it cannot be used as a unit test input parameter.</remarks>
        protected HttpMethod ConstructMethod(string methodName)
        {
            return new HttpMethod(methodName);
        }

        /// <summary>Construct an <see cref="IClient"/> instance and assert that its initial state is valid.</summary>
        /// <param name="baseUri">The base URI prepended to relative request URIs.</param>
        /// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        protected IClient ConstructClient(string baseUri = "http://example.com/", bool inconclusiveOnFailure = true)
        {
            try
            {
                // execute
                IClient client = new FluentClient(baseUri);

                // verify
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
        protected void AssertEqual(IRequest request, HttpMethod method, string resource, string baseUri = "http://example.com/")
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
