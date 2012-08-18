using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;

namespace Pathoschild.Http.Tests.Delegating.Objects
{
	/// <summary>A minimal test implementation of the <see cref="DelegatingRequest"/>.</summary>
	public class TestDelegatingRequest : DelegatingRequest
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="request">The wrapped request builder implementation.</param>
		public TestDelegatingRequest(IRequest request)
			: base(request) { }
	}
}
