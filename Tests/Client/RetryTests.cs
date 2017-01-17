using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Retry;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;

namespace Pathoschild.Http.Tests.Client
{
    /// <summary>Unit tests verifying that HTTP request are retried when appropriate.</summary>
    [TestFixture]
    public class RetryTests
    {
        /*********
        ** Unit tests
        *********/

        [Test]
        public void GetAsync_HTTP429_retry_success()
        {
            // Arrange the Http client
            var mockHttp = new MockHttpMessageHandler();

            // First attempt, we return HTTP 429 which means TOO MANY REQUESTS
            mockHttp.Expect(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond((HttpStatusCode)429);

            // Second attempt, we return the expected result
            mockHttp.Expect(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond("application/json", "{'name' : 'This is a test'}");

            var httpClient = new HttpClient(mockHttp);

            // Arrange the Request coordinator
            var coordinator = new RetryCoordinator(
                2,
                (response) => response.StatusCode == (HttpStatusCode)429,
                (attempts, response) => TimeSpan.Zero);

            // Arrange the fluent htpp client
            var fluentClient = new FluentClient("https://api.fictitious-vendor.com/v1/", httpClient)
                .SetRequestCoordinator(coordinator);

            // Act
            var result = fluentClient
                .GetAsync("endpoint")
                .As<JObject>()
                .Result;

            // Assert
            mockHttp.VerifyNoOutstandingExpectation();
            mockHttp.VerifyNoOutstandingRequest();
            Assert.That(result.Value<string>("name"), Is.EqualTo("This is a test"));
        }

        [Test]
        public void GetAsync_HTTP429_retry_failure()
        {
            // Arrange the Http client
            var mockHttp = new MockHttpMessageHandler();

            // Three successive HTTP 429 (which mean TOO MANY REQUESTS)
            mockHttp.Expect(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond((HttpStatusCode)429);
            mockHttp.Expect(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond((HttpStatusCode)429);
            mockHttp.Expect(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond((HttpStatusCode)429);

            var httpClient = new HttpClient(mockHttp);

            // Arrange the Request coordinator
            var coordinator = new RetryCoordinator(
                3,
                (response) => response.StatusCode == (HttpStatusCode)429,
                (attempts, response) => TimeSpan.Zero);

            // Arrange the fluent htpp client
            var fluentClient = new FluentClient("https://api.fictitious-vendor.com/v1/", httpClient)
                .SetRequestCoordinator(coordinator);

            // Act
            Assert.ThrowsAsync<ApiException>(async () => await fluentClient.GetAsync("endpoint").As<JObject>());

            // Assert
            mockHttp.VerifyNoOutstandingExpectation();
            mockHttp.VerifyNoOutstandingRequest();
        }
    }
}
