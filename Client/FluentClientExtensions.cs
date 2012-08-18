using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client
{
	/// <summary>Provides extensions on <see cref="IClient"/> and <see cref="IRequest"/>.</summary>
	public static class FluentClientExtensions
	{
		/// <summary>Get an object that waits for the completion of the request and its response. This enables support for the <c>await</c> keyword.</summary>
		/// <param name="request">The request to await.</param>
		/// <example>
		/// <code>await client.GetAsync("api/ideas").AsString();</code>
		/// <code>await client.PostAsync("api/ideas", idea);</code>
		/// </example>
		public static TaskAwaiter GetAwaiter(this IRequest request)
		{
			Func<Task> waiter = (async () => await request.AsMessage());
			return waiter().GetAwaiter();
		}
	}
}
