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
	/// <summary>Unit tests verifying that the <see cref="DelegatingRequest"/> correctly delegates calls.</summary>
	[TestFixture]
	class RequestTests : DecoratorTestsBase<IRequest, TestDelegatingRequest>
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

		[Test(Description = "Ensure that the ThrowError property is delegated.")]
		public void ThrowError()
		{
			this.VerifyGet(p => p.ThrowError);
		}

		/***
		** Request builder methods
		***/
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

		/***
		** Response tests
		***/
		[Test(Description = "Ensure that the As method is delegated.")]
		public void As()
		{
			this.VerifyMethod(p => p.As<object>());
		}

		[Test(Description = "Ensure that the AsByteArray method is delegated.")]
		public void AsByteArray()
		{
			this.VerifyMethod(p => p.AsByteArray());
		}

		[Test(Description = "Ensure that the AsList method is delegated.")]
		public void AsList()
		{
			this.VerifyMethod(p => p.AsList<object>());
		}

		[Test(Description = "Ensure that the AsMessage method is delegated.")]
		public void AsMessage()
		{
			this.VerifyMethod(p => p.AsMessage());
		}

		[Test(Description = "Ensure that the AsStream method is delegated.")]
		public void AsStream()
		{
			this.VerifyMethod(p => p.AsStream());
		}

		[Test(Description = "Ensure that the AsString method is delegated.")]
		public void AsString()
		{
			this.VerifyMethod(p => p.AsString());
		}

		/*********
		** Protected methods
		*********/
		public RequestTests()
			: base(mock => new TestDelegatingRequest(mock)) { }
	}
}
