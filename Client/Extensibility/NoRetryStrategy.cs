using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Extensibility
{
	/// <summary>
	/// A retry strategy that never retries.
	/// </summary>
	public class NoRetryStrategy : IRetryStrategy
	{
		/// <summary>
		/// The maximum number of attempts to be carried out before giving up
		/// </summary>
		public int MaxRetriesCount
		{
			get { return 0; }
		}

		/// <summary>
		/// Checks if we should retry an operation.
		/// </summary>
		/// <param name="response">The Http response of the previous request</param>
		/// <returns>
		///   <c>true</c> if another attempt should be made; otherwise, <c>false</c>.
		/// </returns>
		public bool ShouldRetry(HttpResponseMessage response)
		{
			return false;
		}

		/// <summary>
		/// Gets a TimeSpan value which defines how long to wait before trying again after an unsuccessful attempt
		/// </summary>
		/// <param name="attempt">The number of attempts carried out so far. That is, after the first attempt (for
		/// the first retry), attempt will be set to 1, after the second attempt it is set to 2, and so on.</param>
		/// A TimeSpan value which defines how long to wait before the next attempt.
		/// </returns>
		public TimeSpan GetNextDelay(int attempt)
		{
			return TimeSpan.Zero;
		}
	}
}
