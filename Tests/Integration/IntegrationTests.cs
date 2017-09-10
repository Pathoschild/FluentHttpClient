using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Pathoschild.Http.Client;

namespace Pathoschild.Http.Tests.Integration
{
    /// <summary>Integration tests verifying that the client package can communicate with real APIs.</summary>
    [TestFixture]
    public class IntegrationTests
    {
        /*********
        ** Properties
        *********/
        /// <summary>The metadata expected from the English Wikipedia.</summary>
        private readonly WikipediaMetadata.WikipediaGeneral EnwikiMetadata = new WikipediaMetadata.WikipediaGeneral
        {
            ArticlePath = "/wiki/$1",
            Base = "https://en.wikipedia.org/wiki/Main_Page",
            Language = "en",
            MainPage = "Main Page",
            MaxUploadSize = 4294967296,
            ScriptPath = "/w",
            Server = "//en.wikipedia.org",
            SiteName = "Wikipedia",
            Time = DateTime.UtcNow,
            VariantArticlePath = "false",
            WikiID = "enwiki"
        };

        /// <summary>The metadata expected from the Chinese Wikipedia.</summary>
        private readonly WikipediaMetadata.WikipediaGeneral ZhwikiMetadata = new WikipediaMetadata.WikipediaGeneral
        {
            ArticlePath = "/wiki/$1",
            Base = "https://zh.wikipedia.org/wiki/Wikipedia:%E9%A6%96%E9%A1%B5",
            Language = "zh",
            MainPage = "Wikipedia:\u9996\u9875",
            MaxUploadSize = 4294967296,
            ScriptPath = "/w",
            Server = "//zh.wikipedia.org",
            SiteName = "Wikipedia",
            Time = DateTime.UtcNow,
            VariantArticlePath = "/$2/$1",
            WikiID = "zhwiki"
        };


        /*********
        ** Unit tests
        *********/
        [Test(Description = "The client can fetch a resource from the English Wikipedia's API.")]
        public async Task EnglishWikipedia()
        {
            // arrange
            IClient client = this.ConstructClient("https://en.wikipedia.org/");

            // act
            WikipediaMetadata response = await client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" })
                .As<WikipediaMetadata>();

            // assert
            this.AssertResponse(response, this.EnwikiMetadata, "First request");
        }

        [Test(Description = "The client can fetch a resource from the Chinese Wikipedia's API, including proper Unicode handling.")]
        public async Task ChineseWikipedia()
        {
            // arrange
            IClient client = this.ConstructClient("https://zh.wikipedia.org/");

            // act
            WikipediaMetadata response = await client
                .GetAsync("w/api.php")
                .WithArguments(new { action = "query", meta = "siteinfo", siprop = "general", format = "json" })
                .As<WikipediaMetadata>();

            // assert
            this.AssertResponse(response, this.ZhwikiMetadata, "First request");
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an HTTP client with the JSON.NET formatter.</summary>
        /// <param name="url">The base URI prepended to relative request URIs.</param>
        protected IClient ConstructClient(string url)
        {
            return new FluentClient(url);
        }

        /// <summary>Performs assertions on the specified Wikimedia metadata.</summary>
        /// <param name="response">The metadata to assert.</param>
        /// <param name="expected">The expected metadata.</param>
        /// <param name="prefix">The property name prefix to use within assertion exceptions.</param>
        protected void AssertResponse(WikipediaMetadata response, WikipediaMetadata.WikipediaGeneral expected, string prefix)
        {
            // assert
            Assert.IsNotNull(response, prefix + " metadata is null");
            Assert.IsNotNull(response.Query, prefix + " metadata.Query is null.");
            Assert.IsNotNull(response.Query.General, prefix + " metadata.Query.General is null.");

            response.Query.General
                .AssertValue(p => p.ArticlePath, expected.ArticlePath)
                .AssertValue(p => p.Base, expected.Base)
                .AssertValue(p => p.Language, expected.Language)
                .AssertValue(p => p.MainPage, expected.MainPage)
                .AssertValue(p => p.MaxUploadSize, expected.MaxUploadSize)
                .AssertValue(p => p.ScriptPath, expected.ScriptPath)
                .AssertValue(p => p.Server, expected.Server)
                .AssertValue(p => p.SiteName, expected.SiteName)
                .AssertValue(p => p.Time.Date, expected.Time.Date)
                .AssertValue(p => p.VariantArticlePath, expected.VariantArticlePath)
                .AssertValue(p => p.WikiID, expected.WikiID);
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
