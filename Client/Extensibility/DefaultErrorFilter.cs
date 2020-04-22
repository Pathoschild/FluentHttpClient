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
        public void OnRequest(IRequest request) { }

        /// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="httpErrorAsException">Whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions.</param>
        public void OnResponse(IResponse response, bool httpErrorAsException)
        {
            if (httpErrorAsException && !response.Message.IsSuccessStatusCode)
                throw new ApiException(response, $"The API query failed with status code {response.Message.StatusCode}: {response.Message.ReasonPhrase}");
        }
    }
}
