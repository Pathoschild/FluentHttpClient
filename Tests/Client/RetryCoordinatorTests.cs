using System;
using System.Net;
using System.Net.Http;
using System.Threading;
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
        ** Fields
        *********/
        /// <summary>The status code representing a request timeout.</summary>
        /// <remarks>See remarks on the equivalent <see cref="RetryCoordinator"/> field.</remarks>
        private const HttpStatusCode TimeoutStatusCode = (HttpStatusCode)589;


        /*********
        ** Unit tests
        *********/
        [Test(Description = "Ensure that the retry coordinator retries failed requests.")]
        public async Task RetriesFailedRequests()
        {
            // arrange
            const int maxAttempts = 2;
            int attempts = 0;
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.When(HttpMethod.Get, "*").With(req => ++attempts == maxAttempts).Respond(HttpStatusCode.OK); // succeed on last attempt
            mockHandler.When(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
            var client = new FluentClient(new Uri("https://example.org"), new HttpClient(mockHandler))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1));

            // act
            IResponse response = await client.GetAsync("");

            // assert
            Assert.AreEqual(maxAttempts, attempts, "The client did not retry the expected number of times.");
            Assert.AreEqual(response.Status, HttpStatusCode.OK, "The response is unexpectedly not successful.");
        }

        [Test(Description = "Ensure that the retry coordinator retries (or does not retry) timed-out requests.")]
        public async Task RetriesOnTimeout([Values(true, false)] bool retryOnTimeout)
        {
            // arrange
            const int maxAttempts = 3; // two test requests in non-retry mode
            int attempts = 0;
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.When(HttpMethod.Get, "*").With(req => ++attempts == maxAttempts).Respond(HttpStatusCode.OK); // succeed on last attempt
            mockHandler.When(HttpMethod.Get, "*").Respond(async request =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                Assert.Fail("The request unexpectedly didn't time out.");
                return null;
            });

            IClient client = new FluentClient(new Uri("https://example.org"), new HttpClient(mockHandler))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1, retryOnTimeout));
            client.BaseClient.Timeout = TimeSpan.FromMilliseconds(500);

            // act & assert
            if (retryOnTimeout)
            {
                IResponse response = await client.GetAsync("");
                Assert.AreEqual(maxAttempts, attempts, "The client did not retry the expected number of times.");
                Assert.AreEqual(HttpStatusCode.OK, response.Status, "The response is unexpectedly not successful.");
            }
            else
            {
                // make sure timeout is treated as a normal error
                Assert.ThrowsAsync<ApiException>(async () => await client.GetAsync(""), "The request unexpectedly didn't fail.");
                Assert.AreEqual(1, attempts, "The client unexpectedly retried.");

                // validate response when errors-as-exceptions is disabled
                IResponse response = await client.GetAsync("").WithOptions(ignoreHttpErrors: true);
                Assert.AreEqual(RetryCoordinatorTests.TimeoutStatusCode, response.Status, "The response has an unexpected status code.");
            }
        }

        [Test(Description = "Ensure that the retry coordinator doesn't retry requests aborted by a cancellation token.")]
        public void RespectsTaskCancellationToken()
        {
            // arrange
            const int maxAttempts = 2;
            var mockHandler = new MockHttpMessageHandler();
            var mockRequest = mockHandler.When(HttpMethod.Get, "*").Respond(async request =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Assert.Fail("The request unexpectedly wasn't cancelled.");
                return null;
            });

            IClient client = new FluentClient(new Uri("http://example.org"), new HttpClient(mockHandler))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1));

            // act & assert
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetAsync("").WithCancellationToken(tokenSource.Token));
            Assert.AreEqual(1, mockHandler.GetMatchCount(mockRequest), "The client unexpectedly retried.");
        }


        [Test(Description = "Ensure that the retry coordinator gives up after too many failed requests.")]
        public void AbandonsOnTooManyFailures()
        {
            // arrange
            const int maxAttempts = 2;
            var mockHttp = new MockHttpMessageHandler();
            var mockRequest = mockHttp.When(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
            var client = new FluentClient(new Uri("http://example.org"), new HttpClient(mockHttp))
                .SetRequestCoordinator(this.GetRetryConfig(maxAttempts - 1));

            // act & assert
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
                shouldRetry: res => res.StatusCode != HttpStatusCode.OK && (retryOnTimeout || res.StatusCode != RetryCoordinatorTests.TimeoutStatusCode),
                getDelay: (attempt, res) => TimeSpan.FromMilliseconds(1)
            );
        }
    }
}
