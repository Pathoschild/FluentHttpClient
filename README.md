**FluentHttpClient** is a modern async HTTP client for REST APIs. Its fluent interface lets
you send an HTTP request and parse the response in one go — hiding away the gritty
details like deserialisation, content negotiation, optional retry logic, and URL encoding:

```c#
Blog result = await new FluentClient("https://example.org/api")
   .GetAsync("blogs")
   .WithArgument("id", 15)
   .WithBearerAuthentication(token)
   .As<Blog>();
```

Designed with discoverability and extensibility as core principles, just autocomplete to see which
methods are available at each step.

## Install
Install it [from NuGet][Pathoschild.Http.FluentClient]:
> Install-Package Pathoschild.Http.FluentClient

The client works on any modern platform (including Linux, Mac, and Windows):

| platform                    | min version |
| :-------------------------- | :---------- |
| .NET Framework              | 4.5.2       |
| .NET Core                   | 1.0         |
| [.NET Standard][]           | 1.3         |
| Universal Windows Platform  | 10          |

## Use
### Basic usage
You start by creating a client for an API. You can use this client for one request, or reuse it for
many requests for improved performance using its built-in connection pool.

```c#
IClient client = new FluentClient("https://example.org/api/");
```

Then you just chain methods to set up the request and get the response. For example, here's a GET
request which returns an `Item` model (automatically deserialised based on content negotiation):
```c#
Item item = await client
    .GetAsync("items/14")
    .As<Item>();
```

You can get the response as a model, array of models, bytes, string, or stream:
```c#
string content = await client
    .GetAsync("items/14")
    .AsString();
```

If you don't need the response content, you can just wait for the request to complete.
```c#
await client.PostAsync("items", new Item(…));
```

### Request options
You can quickly configure complex requests with the fluent methods. Here's a more complicated
example:

```c#
Message[] messages = await client
    .GetAsync("messages/latest")
    .WithHeader("Content-Type", "application/json")
    .WithArguments(new { id = 14, tenant = "acme" })
    .WithBearerAuthentication(token)
    .AsArray<Message>();
```

### Get response data
The above examples all parse the response directly, but sometimes you want to peek at the HTTP
metadata:

```c#
IResponse response = await client.GetAsync("messages/latest");
if (response.Status == HttpStatusCode.OK)
   return response.AsArray<T>();
```

### Error handling
By default the client will throw `ApiException` if the server returns an error code:
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

If you don't want that, you can easily:
* disable it for one request:

  ```c#
  IResponse response = await client
      .GetAsync("items")
      .WithHttpErrorAsException(false);
  ```

* disable it for all requests:

  ```c#
  client.SetHttpErrorAsException(false);
  ```

* [use your own error filter](#custom-filters).

## Advanced features
### Custom formats
The client supports JSON and XML out of the box. If you need more, you can...

* Add any of the existing [media type formatters][]:

  ```c#
  client.Formatters.Add(new YamlFormatter());
  ```

* Create your own by subclassing `MediaTypeFormatter` (optionally using the included
  `MediaTypeFormatterBase` class).

### Custom retry / coordination
The client won't retry failed requests by default, but that's easy to configure:
```c#
client
    .SetRequestCoordinator(
        maxRetries: 3,
        shouldRetry: request => request.StatusCode != HttpStatusCode.OK,
        getDelay: (attempt, response) => return TimeSpan.FromSeconds(attempt), // 1, 2, and 3 seconds
        retryOnTimeout: true
    );
```

If that's not enough, implementing `IRequestCoordinator` lets you control how the client
dispatches requests. (You can only have one request coordinator on the client; you should use
[HTTP filters](#custom-behaviour) instead for most overrides.)

For example, here's a simple retry coordinator using [Polly](https://github.com/App-vNext/Polly):
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

...and here's how you'd set it:
```c#
client.SetRequestCoordinator(new RetryCoordinator());
```


### Custom filters
You can read and change the underlying HTTP requests and responses by creating `IHttpFilter`
implementations. They can be useful for automating custom authentication or error-handling.

For example, the default error-handling is just a filter:
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

...which you can replace with your own:
```c#
client.Filters.Remove<DefaultErrorFilter>();
client.Filters.Add(new YourErrorFilter());
```

You can do much more with HTTP filters by editing the requests before they're sent or the responses
before they're parsed:

```c#
/// <summary>Method invoked just before the HTTP request is submitted. This method can modify the outgoing HTTP request.</summary>
/// <param name="request">The HTTP request.</param>
public void OnRequest(IRequest request)
{
    // example only — you'd normally use a method like client.SetAuthentication(…) instead.
    request.Message.Headers.Authorization = new AuthenticationHeaderValue("token", "…");
}
```

### Custom HTTP
For advanced scenarios, you can customise the underlying [HttpClient][] and
[HttpClientHandler][]. For example, here's how to create mock requests for unit testing using
[RichardSzalay.MockHttp](https://www.nuget.org/packages/RichardSzalay.MockHttp/):
```c#
// create mock
var mockHandler = new MockHttpMessageHandler();
mockHandler.When(HttpMethod.Get, "https://example.org/api/items").Respond(HttpStatusCode.OK, testRequest => new StringContent("[]"));

// create client
var client = new FluentClient("https://example.org/api", new HttpClient(mockHandler));
```

### Cancellation token support
The client fully supports [.NET cancellation tokens](https://msdn.microsoft.com/en-us/library/dd997364.aspx)
if you need to abort requests:

```c#
var tokenSource = new CancellationTokenSource();
await client
    .PostAsync(…)
    .WithCancellationToken(tokenSource.Token);
tokenSource.Cancel();
```


### Synchronous use
The client is build around the `async` and `await` keywords, but you can use the client
synchronously. That's not recommended — it complicates error-handling (e.g. errors get wrapped
into [AggregateException][]), and it's very easy to cause thread deadlocks when you do this (see
_[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!][]_ and
_[Don't Block on Async Code][])._

If you really need to use it synchronously, you can just call the `Result` property:
```c#
Item item = client.GetAsync("items/14").Result;
```

Or if you don't need the response:

```c#
client.PostAsync("items", new Item(…)).AsResponse().Wait();
```

[.NET Standard]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!]: http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115163.aspx
[Don't Block on Async Code]: http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
[media type formatters]: https://www.nuget.org/packages?q=MediaTypeFormatter
[circuit breaker]: https://msdn.microsoft.com/en-us/library/dn589784.aspx

[AggregateException]: http://msdn.microsoft.com/en-us/library/system.aggregateexception.aspx
[HttpClient]: https://msdn.microsoft.com/en-us/library/system.net.http.httpclient.aspx
[HttpClientHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclienthandler.aspx
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[JSON]: https://en.wikipedia.org/wiki/JSON

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequest]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequest.cs#L12

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
