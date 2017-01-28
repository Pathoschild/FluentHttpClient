**FluentHttpClient** is an easy asynchronous HTTP client for REST APIs. It provides a fluent
interface that lets you create an HTTP request, dispatch and wait for it, and parse the response.
The client takes care of the gritty details for you (like deserialisation, [content negotiation][],
optional retry logic, and URL encoding), and it's easy to extend and customise.

## Installing
The fluent client is [available on NuGet][Pathoschild.Http.FluentClient]:
> Install-Package Pathoschild.Http.FluentClient

You can use the client with .NET Framework 4.5.2+ or [.NET Standard][] 1.3+. That
includes:

| platform                    | min version |
| :-------------------------- | :---------- |
| .NET Framework              | 4.5.2       |
| .NET Core                   | 1.0         |
| Universal Windows Platform  | 10          |

## Using the client
### Basic usage
You start by creating a client:

```c#
IClient client = new FluentClient("https://example.org/api/");
```

Then you can chain methods to configure the request and parsing. For example, here's a simple GET
request with the response parsed into an `Item` model:
```c#
Item item = await client
    .GetAsync("items/14")
    .As<Item>();
```

You can parse the response as a model, list of models, byte array, string, or stream:
```c#
string json = await client
    .GetAsync("items/14")
    .AsString();
```

If you don't need the response, you can just wait for the request to complete.
```c#
await client.PostAsync("items", new Item(..));
```

You can configure some pretty complex requests using the fluent interface (the client will take
care of the details like input sanitisation and URL encoding):
```c#
Item item = await client
    .GetAsync("items")
    .WithHeader("Content-Type", "application/json")
    .WithArguments(new { id = 14, tenant = "tenant-name" })
    .WithBasicAuthentication(username, password)
    .As<Item>();
```

The client has many more methods you can use to configure the request. These are all fully
documented for IntelliSense, so you can use code completion as you type to find the ones you need.

### Get response metadata
You can read the response data (e.g. status code or headers) by awaiting the `IRequest`. Reading
the response content won't re-execute the request.

```c#
IResponse response = await client.GetAsync("weird-api");
if (response.Headers.Contains("X-Array-Response"))
    return response.AsArray<T>();
else
    return new T[] { response.As<T>() };
```

### Handling errors
The client will throw an `ApiException` for non-success HTTP responses like HTTP 404, with the
response metadata on the exception object:
```c#
try
{
    await client.Get("items");
}
catch(ApiException ex)
{
    string responseText = await ex.Response.AsString();
    throw new Exception($"The API responded with HTTP {ex.Response.Status}: {responseText}");
}
```

You can disable HTTP errors as exceptions for one request to handle the response directly:
```c#
IResponse response = await client.GetAsync("items").WithHttpErrorAsException(false);
if (response.Status == HttpStatusCode.OK)
    return response.As<Item>();
```

...or disable it for all requests:
```c#
client.SetHttpErrorsAsExceptions(false);
```

You can optionally create your own error-handling logic; see _customising the client_ below.

### Retry logic
The client lets you configure retry logic out of the box:
```c#
IClient client = new FluentClient("https://example.org")
    .SetRequestCoordinator(
        maxRetries: 3,
        shouldRetry: request => request.StatusCode != HttpStatusCode.OK,
        getDelay: (attempt, response) => return attempt // 1, 2, and 3 seconds
    );
```

### Synchronous use
The client is designed to take advantage of the `async` and `await` keywords in .NET 4.5, but
you can use the client synchronously. This is *not* recommended — it complicates error-handling
(e.g. errors get wrapped into [AggregateException][]), and it's very easy to cause thread deadlocks
when you do this (see _[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!][]_
and _[Don't Block on Async Code][])._

If you really need to use it synchronously, you can just call the `Result` property:
```c#
Item item = client
    .GetAsync("items/14")
    .As<Item>()
    .Result;
```

Or if you don't need the response:

```c#
client.PostAsync("items", new Item()).AsResponse().Wait();
```

## Customising the client
### Custom formats
By default the client supports JSON and XML. The client recognises
[`MediaTypeFormatter` implementations][MediaTypeFormatter], so you can easily add different formats:
```c#
client.Formatters.Add(new BsonFormatter());
```

You can use one of the many [`MediaTypeFormatter` implementations][], use the included BSON
formatter, or create your own (optionally using the included `MediaTypeFormatterBase` base class).

### Custom behaviour
You can customise the client by adding your own implementations of `IHttpFilter`. Each filter
can read and change the underlying HTTP requests (e.g. for authentication) and responses (e.g. for
error handling). For example, you can easily replace the default error handling (see _Error
handling_ above):
```c#
client.Filters.Remove<DefaultErrorFilter>();
client.Filters.Add(new YourErrorFilter());
```

For reference, the default error filter is essentially this one method:
```c#
/// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
/// <param name="response">The HTTP response.</param>
/// <param name="httpErrorAsException">Whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions.</param>
public void OnResponse(IResponse response, bool httpErrorAsException)
{
    if (httpErrorAsException && !response.Message.IsSuccessStatusCode)
        throw new ApiException(response, $"The API query failed with status code {response.Message.StatusCode}: {response.Message.ReasonPhrase}");
}
```

That's a pretty simple filter, but you can do more advanced things by changing the request and 
response messages. For example, here's a minimal filter that injects an authentication token into
every HTTP request:
```c#
/// <summary>Method invoked just before the HTTP request is submitted. This method can modify the outgoing HTTP request.</summary>
/// <param name="request">The HTTP request.</param>
public void OnRequest(IRequest request)
{
    request.Message.Headers.Authorization = new AuthenticationHeaderValue("token", "...");
}
```

You can even rewrite HTTP responses from the server before they're parsed if you want to.

### Custom request coordinator
Implementing `IRequestCoordinator` lets you control how the client executes requests, usually to
implement fault tolerance (e.g. retry logic). You can only have one request coordinator on the
client; you should use [HTTP filters](#custom-behaviour) instead for most customisations.

For example, here's a simple implementation of a retry coordinator using [Polly](https://github.com/App-vNext/Polly):
```c#
/// <summary>A request coordinator which retries failed requests with a delay between each attempt.</summary>
public class RetryCoordinator : IRequestCoordinator
{
    /// <summary>Dispatch an HTTP request.</summary>
    /// <param name="request">The response message to validate.</param>
    /// <param name="dispatcher">Dispatcher that executes the request.</param>
    /// <returns>The final HTTP response.</returns>
    public async Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
    {
        int[] retryCodes = { 408, 500, 502, 503, 504 };
        return Policy
            .HandleResult<HttpResponseMessage>(request => retryCodes.Contains((int)request.StatusCode))
            .Retry(3, async () => await send(request));
    }
}
```

### Custom HTTP client
For really advanced scenarios, you can customise the underlying [HttpClient][] and
[HttpClientHandler][]:
```c#
// create custom HTTP handler
var handler = new HttpClientHandler()
{
    Credentials = new NetworkCredential("username", "password"),
    Proxy = new WebProxy(...)
};
handler.CookieContainer.Add(new Cookie(...));

// create client
var client = new FluentClient("http://example.org/api/", new HttpClient(handler));
```

[.NET Standard]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!]: http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115163.aspx
[Don't Block on Async Code]: http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
[`MediaTypeFormatter` implementations]: https://www.nuget.org/packages?q=MediaTypeFormatter

[AggregateException]: http://msdn.microsoft.com/en-us/library/system.aggregateexception.aspx
[HttpClient]: https://msdn.microsoft.com/en-us/library/system.net.http.httpclient.aspx
[HttpClientHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclienthandler.aspx
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[BSON]: https://en.wikipedia.org/wiki/BSON
[content negotiation]: http://en.wikipedia.org/wiki/Content_negotiation
[JSON]: https://en.wikipedia.org/wiki/JSON
[JSONP]: https://en.wikipedia.org/wiki/JSONP

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequest]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequest.cs#L12

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
