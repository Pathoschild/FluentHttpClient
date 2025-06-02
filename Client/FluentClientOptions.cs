using System.Net.Http;

namespace Pathoschild.Http.Client;

/// <summary>Options for the fluent client.</summary>
public class FluentClientOptions
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether null arguments in the request body and URL query string should be ignored (<c>true</c>) or sent as-is (<c>false</c>). Default <c>true</c>.</summary>
    public bool? IgnoreNullArguments { get; set; }

    /// <summary>Whether HTTP error responses like HTTP 404 should be ignored (<c>true</c>) or raised as exceptions (<c>false</c>). Default <c>false</c>.</summary>
    public bool? IgnoreHttpErrors { get; set; }

    /// <summary>
    ///   <para>When we should stop waiting for the response. For example, setting this to <see cref="HttpCompletionOption.ResponseHeadersRead"/> will let you handle the response as soon as the headers are received, before the full response body has been fetched. This only affects getting the <see cref="IResponse"/>; reading the response body (e.g. using a method like <see cref="IResponse.As{T}"/>) will still wait for the request body to be fetched as usual.</para>
    ///   <para>Default <see cref="HttpCompletionOption.ResponseContentRead"/> if not specified.</para>
    /// </summary>
    public HttpCompletionOption? CompleteWhen { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get the equivalent request options.</summary>
    internal RequestOptions ToRequestOptions()
    {
        return new RequestOptions
        {
            IgnoreHttpErrors = this.IgnoreHttpErrors,
            IgnoreNullArguments = this.IgnoreNullArguments,
            CompleteWhen = this.CompleteWhen,
        };
    }

    /// <summary>Copy the non-null values from the given options.</summary>
    /// <param name="options">The options to copy.</param>
    internal void MergeFrom(FluentClientOptions? options)
    {
        this.IgnoreNullArguments = options?.IgnoreNullArguments ?? this.IgnoreNullArguments;
        this.IgnoreHttpErrors = options?.IgnoreHttpErrors ?? this.IgnoreHttpErrors;
        this.CompleteWhen = options?.CompleteWhen ?? this.CompleteWhen;
    }
}