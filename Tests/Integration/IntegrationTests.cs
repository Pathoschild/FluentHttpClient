using System;
using System.Linq.Expressions;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Default;
using Pathoschild.Http.Formatters.JsonNet;

namespace Pathoschild.Http.Tests.Integration
{
    /// <summary>Integration tests verifying that the client package can communicate with real APIs.</summary>
    [TestFixture]
    public class IntegrationTests
    {
        /*********
        ** Unit tests
        *********/
        [Test(Description = "The client can fetch a resource from Wikipedia's API.")]
        public async void Wikipedia()
        {
            // arrange
            IClient client = this.ConstructClient("https://en.wikipedia.org/");

            // act
            WikipediaMetadata response = await client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" })
                .As<WikipediaMetadata>();

            this.AssertResponse(response, "First request");
        }

        [Test(Description = "The client response is null if it performs the same request twice. This matches the behaviour of the underlying HTTP client.")]
        public async void Wikipedia_ResendingRequestSetsResponseToNull()
        {
            // arrange
            IClient client = this.ConstructClient("http://en.wikipedia.org/");
            IRequest request = client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" });

            // act
            this.AssertResponse(await request.As<WikipediaMetadata>(), "First request");

            // assert
            Assert.IsNull(await request.WithArgument("limit", "max").As<WikipediaMetadata>(), null);
        }

        [Test(Description = "The client can resubmit the same request multiple times by cloning it. Normally the HTTP client does not allow a request to be resubmitted.")]
        public async void Wikipedia_MultipleRequests_Clone()
        {
            // arrange
            IClient client = this.ConstructClient("http://en.wikipedia.org/");
            var request = client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" });

            // act
            this.AssertResponse(await request.As<WikipediaMetadata>(), "First request");

            // assert
            this.AssertResponse(await request.Clone().WithArgument("limit", "max").As<WikipediaMetadata>(), "Second request");
        }

        [Test(Description = "The client can fetch a resource from Wikipedia's API and read the response multiple times.")]
        public async void Wikipedia_MultipleReads()
        {
            // arrange
            IClient client = this.ConstructClient("http://en.wikipedia.org/");

            // act
            IRequest request = client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" });

            string valueA = await request.AsString();
            string valueB = await request.AsString();

            // assert
            Assert.IsNotNullOrEmpty(valueA, "response is null or empty");
            Assert.AreEqual(valueA, valueB, "second read got a different result");
        }

        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an HTTP client with the JSON.NET formatter.</summary>
        /// <param name="url">The base URI prepended to relative request URIs.</param>
        protected IClient ConstructClient(string url)
        {
            IClient client = new FluentClient(url);
            client.Formatters.Remove(client.Formatters.JsonFormatter);
            client.Formatters.Add(new JsonNetFormatter());
            return client;
        }

        /// <summary>Performs assertions on the specified Wikimedia metadata.</summary>
        /// <param name="response">The metadata to assert.</param>
        /// <param name="prefix">The property name prefix to use within assertion exceptions.</param>
        protected void AssertResponse(WikipediaMetadata response, string prefix)
        {
            // assert
            Assert.IsNotNull(response, prefix + " metadata is null");
            Assert.IsNotNull(response.Query, prefix + " metadata.Query is null.");
            Assert.IsNotNull(response.Query.General, prefix + " metadata.Query.General is null.");

            response.Query.General
                .AssertValue(p => p.ArticlePath, "/wiki/$1")
                .AssertValue(p => p.Base, "https://en.wikipedia.org/wiki/Main_Page")
                .AssertValue(p => p.Language, "en")
                .AssertValue(p => p.MainPage, "Main Page")
                .AssertValue(p => p.MaxUploadSize, 4294967296)
                .AssertValue(p => p.ScriptPath, "/w")
                .AssertValue(p => p.Server, "//en.wikipedia.org")
                .AssertValue(p => p.SiteName, "Wikipedia")
                .AssertValue(p => p.Time.Date, DateTime.UtcNow.Date)
                .AssertValue(p => p.VariantArticlePath, false)
                .AssertValue(p => p.WikiID, "enwiki");
        }
    }

    /// <summary>Object extensions for the <see cref="IntegrationTests"/>.</summary>
    public static class IntegrationTestsExtensions
    {
        /// <summary>Assert that a property has the expected value.</summary>
        /// <typeparam name="TObject">The type of the object whose property to check.</typeparam>
        /// <typeparam name="TValue">The value type of the property.</typeparam>
        /// <param name="obj">The object whose property to check.</param>
        /// <param name="actual">Selects the property value from the object.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>Returns the <see cref="obj"/> for chaining.</returns>
        public static TObject AssertValue<TObject, TValue>(this TObject obj, Expression<Func<TObject, TValue>> actual, TValue expected)
            where TObject : class
        {
            Assert.AreEqual(expected, actual.Compile().Invoke(obj), "Unexpected value for " + actual.Body);
            return obj;
        }
    }
}
