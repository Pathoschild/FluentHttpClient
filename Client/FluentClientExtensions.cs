using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client
{
	/// <summary>Provides extensions on <see cref="IClient"/> and its fluent interface.</summary>
	public static class FluentClientExtensions
	{
		/// <summary>Get an object that waits for the completion of the request and its response. This enables support for the <c>await</c> keyword.</summary>
		/// <param name="request">The request to await.</param>
		public static TaskAwaiter GetAwaiter(this IRequestBuilder request)
		{
			Func<Task> waiter = (async () => await request.RetrieveAsync());
			return waiter().GetAwaiter();
		}

		/// <summary>Get an object that waits for the completion of the response. This enables support for the <c>await</c> keyword.</summary>
		/// <param name="response">The response to await.</param>
		public static TaskAwaiter GetAwaiter(this IResponse response)
		{
			Func<Task> waiter = async () => await response.AsMessage();
			return waiter().GetAwaiter();
		}
	}
}
