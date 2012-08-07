using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;

namespace Pathoschild.Http.Tests.Delegating.Objects
{
	/// <summary>A minimal test implementation of the <see cref="DelegatingResponse"/>.</summary>
	public class TestDelegatingResponse : DelegatingResponse
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="response">The wrapped response.</param>
		public TestDelegatingResponse(IResponse response)
			: base(response) { }
	}
}
