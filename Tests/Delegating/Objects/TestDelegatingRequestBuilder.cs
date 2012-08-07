using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;

namespace Pathoschild.Http.Tests.Delegating.Objects
{
	/// <summary>A minimal test implementation of the <see cref="DelegatingRequestBuilder"/>.</summary>
	public class TestDelegatingRequestBuilder : DelegatingRequestBuilder
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="requestBuilder">The wrapped request builder implementation.</param>
		public TestDelegatingRequestBuilder(IRequestBuilder requestBuilder)
			: base(requestBuilder) { }
	}
}
