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
			IClient client = this.ConstructClient("http://en.wikipedia.org/");

			// act
			WikipediaMetadata response = await client
				.GetAsync("w/api.php")
				.WithArgument("action", "query")
				.WithArgument("meta", "siteinfo")
				.WithArgument("siprop", "general")
				.WithArgument("format", "json")
				.As<WikipediaMetadata>();

			// assert
			Assert.IsNotNull(response, "metadata is null");
			Assert.IsNotNull(response.Query, "metadata.Query is null.");
			Assert.IsNotNull(response.Query.General, "metadata.Query.General is null.");

			response.Query.General
				.AssertValue(p => p.ArticlePath, "/wiki/$1")
				.AssertValue(p => p.Base, "http://en.wikipedia.org/wiki/Main_Page")
				.AssertValue(p => p.Language, "en")
				.AssertValue(p => p.MainPage, "Main Page")
				.AssertValue(p => p.MaxUploadSize, 524288000)
				.AssertValue(p => p.ScriptPath, "/w")
				.AssertValue(p => p.Server, "//en.wikipedia.org")
				.AssertValue(p => p.SiteName, "Wikipedia")
				.AssertValue(p => p.Time.Date, DateTime.UtcNow.Date)
				.AssertValue(p => p.VariantArticlePath, false)
				.AssertValue(p => p.WikiID, "enwiki");
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an HTTP client with the JSON.NET formatter.</summary>
		/// <param name="url">The base URI prepended to relative request URIs.</param>
		protected IClient ConstructClient(string url)
		{
			IClient client = new FluentClient(url);
			client.Formatters.Clear();
			client.Formatters.Add(new JsonNetFormatter());
			return client;
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
