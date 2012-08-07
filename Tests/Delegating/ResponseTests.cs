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

		[Test(Description = "Ensure that the AsAsync method is delegated.")]
		public void AsAsync()
		{
			this.VerifyMethod(p => p.AsAsync<object>());
		}

		[Test(Description = "Ensure that the AsByteArray method is delegated.")]
		public void AsByteArray()
		{
			this.VerifyMethod(p => p.AsByteArray());
		}

		[Test(Description = "Ensure that the AsByteArrayAsync method is delegated.")]
		public void AsByteArrayAsync()
		{
			this.VerifyMethod(p => p.AsByteArrayAsync());
		}

		[Test(Description = "Ensure that the AsList method is delegated.")]
		public void AsList()
		{
			this.VerifyMethod(p => p.AsList<object>());
		}

		[Test(Description = "Ensure that the AsList method is delegated.")]
		public void AsListAsync()
		{
			this.VerifyMethod(p => p.AsListAsync<object>());
		}

		[Test(Description = "Ensure that the AsMessage method is delegated.")]
		public void AsMessage()
		{
			this.VerifyMethod(p => p.AsMessage());
		}

		[Test(Description = "Ensure that the AsMessageAsync method is delegated.")]
		public void AsMessageAsync()
		{
			this.VerifyMethod(p => p.AsMessageAsync());
		}

		[Test(Description = "Ensure that the AsStream method is delegated.")]
		public void AsStream()
		{
			this.VerifyMethod(p => p.AsStream());
		}

		[Test(Description = "Ensure that the AsStreamAsync method is delegated.")]
		public void AsStreamAsync()
		{
			this.VerifyMethod(p => p.AsStreamAsync());
		}

		[Test(Description = "Ensure that the AsString method is delegated.")]
		public void AsString()
		{
			this.VerifyMethod(p => p.AsString());
		}

		[Test(Description = "Ensure that the AsStringAsync method is delegated.")]
		public void AsStringAsync()
		{
			this.VerifyMethod(p => p.AsStringAsync());
		}

		[Test(Description = "Ensure that the AsStringAsync method is delegated.")]
		public void Wait()
		{
			this.VerifyMethod(p => p.Wait());
		}


		/*********
		** Protected methods
		*********/
		public ResponseTests()
			: base(mock => new TestDelegatingResponse(mock)) { }
	}
}
