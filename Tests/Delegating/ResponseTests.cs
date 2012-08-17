using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;
using Pathoschild.Http.Tests.Delegating.Objects;

namespace Pathoschild.Http.Tests.Delegating
{
	/// <summary>Unit tests verifying that the <see cref="DelegatingResponse"/> correctly delegates calls.</summary>
	[TestFixture]
	public class ResponseTests : DecoratorTestsBase<IResponse, TestDelegatingResponse>
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

		[Test(Description = "Ensure that the Request property is delegated.")]
		public void Request()
		{
			this.VerifyGet(p => p.Request);
		}

		[Test(Description = "Ensure that the ThrowError property is delegated.")]
		public void ThrowError()
		{
			this.VerifyGet(p => p.ThrowError);
		}

		/***
		** Methods
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
		public ResponseTests()
			: base(mock => new TestDelegatingResponse(mock)) { }
	}
}
