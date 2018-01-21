namespace Pathoschild.Http.Client
{
    /// <summary>Options for the fluent http client.</summary>
    public class FluentClientOptions
    {
        /// <summary>Indicates whether arguments with null value be ignored when a request is dispatched.</summary>
        public bool IgnoreNullArguments { get; set; } = true;

        /// <summary>Indicates whether HTTP error responses (e.g. HTTP 404) should be ignored or should be raised as exceptions</summary>
        public bool IgnoreHttpErrors { get; set; } = false;
    }
}
