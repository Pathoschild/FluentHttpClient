using System;
using System.Net.Http;

namespace Pathoschild.Http.Client.Extensibility
{
    /// <summary>An HTTP filter which detects failed HTTP requests and throws an exception.</summary>
    public class DefaultErrorFilter : IHttpFilter
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Method invoked just before the HTTP request is submitted. This method can modify the outgoing HTTP request.</summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="requestMessage">The underlying HTTP request message.</param>
        public void OnRequest(IRequest request, HttpRequestMessage requestMessage) { }

        /// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="responseMessage">The underlying HTTP response message.</param>
        public void OnResponse(IResponse response, HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
                return;

            throw new ApiException(response, responseMessage, String.Format("The API query failed with status code {0}: {1}", responseMessage.StatusCode, responseMessage.ReasonPhrase));
        }
    }
}
