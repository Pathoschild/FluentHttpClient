using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pathoschild.Http.Client.Retry
{
    /// <summary>Defines how the HTTP client should dispatch requests and process responses at a low level, for example to handle transient failures and errors. Only one may be used by the client.</summary>
    public interface IRequestCoordinator
    {
        /*********
        ** Methods
        *********/
        /// <summary>Dispatch an HTTP request.</summary>
        /// <param name="request">The request.</param>
        /// <param name="dispatcher">A method which executes the request.</param>
        /// <returns>The final HTTP response.</returns>
        Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> dispatcher);
    }
}
