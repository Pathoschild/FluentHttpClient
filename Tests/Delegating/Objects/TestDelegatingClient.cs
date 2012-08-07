using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;

namespace Pathoschild.Http.Tests.Delegating.Objects
{
	/// <summary>A minimal test implementation of the <see cref="DelegatingFluentClient"/>.</summary>
	public class TestDelegatingClient : DelegatingFluentClient
	{
		/// <summary>Construct an instance.</summary>
		/// <param name="client">The wrapped client implementation.</param>
		public TestDelegatingClient(IClient client)
			: base(client) { }
	}
}
