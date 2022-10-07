namespace Pathoschild.Http.Client.Extensibility
{
    /// <summary>An HTTP filter which detects failed HTTP requests and throws an exception.</summary>
    public class DefaultErrorFilter : IHttpFilter
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public void OnRequest(IRequest request) { }

        /// <inheritdoc />
        public void OnResponse(IResponse response, bool httpErrorAsException)
        {
            if (httpErrorAsException && !response.Message.IsSuccessStatusCode)
                throw new ApiException(response, $"The API query failed with status code {response.Message.StatusCode}: {response.Message.ReasonPhrase}");
        }
    }
}
