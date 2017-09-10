using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Retry;
using RichardSzalay.MockHttp;

namespace Pathoschild.Http.Tests.Client
{
    /// <summary>Unit tests verifying that HTTP request are retried when appropriate.</summary>
    [TestFixture]
    public class RetryCoordinatorTests
    {
        /*********
        ** Unit tests
        *********/
        [Test(Description = "Ensure that the retry coordinator retries failed requests.")]
        public async Task RetriesFailedRequests()
        {
            // configure
            const string domain = "https://example.org";
            const int maxAttempts = 2;

            // set up
            int attempts = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, domain).With(req => ++attempts == maxAttempts).Respond(HttpStatusCode.OK); // succeed on last attempt
            mockHttp.When(HttpMethod.Get, domain).Respond(HttpStatusCode.NotFound);
            var client = new FluentClient(domain, new HttpClient(mockHttp))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1));

            // execute
            IResponse response = await client.GetAsync("");

            // verify
            Assert.AreEqual(maxAttempts, attempts, "The client did not retry the expected number of times.");
            Assert.AreEqual(response.Status, HttpStatusCode.OK, "The response is unexpectedly not successful.");
        }

        [Test(Description = "Ensure that the retry coordinator retries (or does not retry) timed-out requests.")]
        public async Task RetriesOnTimeout([Values(true, false)] bool retryOnTimeout)
        {
            // configure
            const string domain = "https://example.org";
            const int maxAttempts = 2;

            // set up
            int attempts = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, domain).With(req => ++attempts == maxAttempts).Respond(HttpStatusCode.OK); // succeed on last attempt
            mockHttp
                .When(HttpMethod.Get, domain)
                .Respond(async request =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    throw new InvalidOperationException("The request unexpectedly didn't time out.");
                });

            IClient client = new FluentClient(domain, new HttpClient(mockHttp))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1, retryOnTimeout));
            client.BaseClient.Timeout = TimeSpan.FromMilliseconds(500);

            // execute & verify
            if (retryOnTimeout)
            {
                IResponse response = await client.GetAsync("");
                Assert.AreEqual(maxAttempts, attempts, "The client did not retry the expected number of times.");
                Assert.AreEqual(response.Status, HttpStatusCode.OK, "The response is unexpectedly not successful.");
            }
            else
            {
                Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetAsync(""));
                Assert.AreEqual(1, attempts, "The client unexpectedly retried.");
            }
        }

        [Test(Description = "Ensure that the retry coordinator gives up after too many failed requests.")]
        public void AbandonsOnTooManyFailures()
        {
            // configure
            const string domain = "https://example.org";
            const int maxAttempts = 2;

            // set up
            var mockHttp = new MockHttpMessageHandler();
            var mockRequest = mockHttp.When(HttpMethod.Get, domain).Respond(HttpStatusCode.NotFound);
            var client = new FluentClient(domain, new HttpClient(mockHttp))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1));

            // execute & assert
            Assert.ThrowsAsync<ApiException>(async () => await client.GetAsync(""));
            Assert.AreEqual(maxAttempts, mockHttp.GetMatchCount(mockRequest), "The client did not retry the expected number of times.");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a retry configuration which retries any non-OK request after 1 millisecond.</summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="retryOnTimeout">Whether to retry on timeout.</param>
        private IRetryConfig GetRetryConfig(int maxRetries, bool retryOnTimeout = true)
        {
            return new RetryConfig(
                maxRetries: maxRetries,
                shouldRetry: res => res.StatusCode != HttpStatusCode.OK,
                getDelay: (attempt, res) => TimeSpan.FromMilliseconds(1),
                retryOnTimeout: retryOnTimeout
            );
        }
    }
}
