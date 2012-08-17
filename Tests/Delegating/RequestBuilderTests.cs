using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Moq;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;
using Pathoschild.Http.Tests.Delegating.Objects;

namespace Pathoschild.Http.Tests.Delegating
{
	/// <summary>Unit tests verifying that the <see cref="DelegatingRequestBuilder"/> correctly delegates calls.</summary>
	[TestFixture]
	class RequestBuilderTests : DecoratorTestsBase<IRequestBuilder, TestDelegatingRequestBuilder>
	{
		/*********
		** Unit tests
		*********/
		/***
		** Properties
		***/
		[Test(Description = "Ensure that the Formatters property is delegated.")]
		public void Formatters()
		{
			this.VerifyGet(p => p.Formatters);
		}

		[Test(Description = "Ensure that the Message property is delegated.")]
		public void Message()
		{
			this.VerifyGet(p => p.Message);
		}

		/***
		** Methods
		***/
		[Test(Description = "Ensure that the RetrieveAsync method is delegated.")]
		public void RetrieveAsync()
		{
			this.VerifyMethod(p => p.RetrieveAsync(It.IsAny<bool>()));
		}

		[Test(Description = "Ensure that the WithArgument method is delegated.")]
		public void WithArgument()
		{
			this.VerifyMethod(p => p.WithArgument(It.IsAny<string>(), It.IsAny<object>()));
		}

		[Test(Description = "Ensure that the WithBody method is delegated.")]
		public void WithBody()
		{
			this.VerifyMethod(p => p.WithBody(It.IsAny<string>(), It.IsAny<MediaTypeFormatter>(), It.IsAny<string>()));
		}

		[Test(Description = "Ensure that the WithBody method (when passed a MediaTypeHeaderValue) is delegated.")]
		public void WithBody_WithHeaderValue()
		{
			this.VerifyMethod(p => p.WithBody(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>()));
		}

		[Test(Description = "Ensure that the WithBodyContent method is delegated.")]
		public void WithBodyContent()
		{
			this.VerifyMethod(p => p.WithBodyContent(It.IsAny<HttpContent>()));
		}

		[Test(Description = "Ensure that the WithCustom method is delegated.")]
		public void WithCustom()
		{
			this.VerifyMethod(p => p.WithCustom(It.IsAny<Action<HttpRequestMessage>>()));
		}

		[Test(Description = "Ensure that the WithHeader method is delegated.")]
		public void WithHeader()
		{
			this.VerifyMethod(p => p.WithHeader(It.IsAny<string>(), It.IsAny<string>()));
		}


		/*********
		** Protected methods
		*********/
		public RequestBuilderTests()
			: base(mock => new TestDelegatingRequestBuilder(mock)) { }
	}
}
