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

## Contents
* [Get started](#get-started)
  * [Install](#install)
  * [Basic usage](#basic-usage)
  * [URL arguments](#url-arguments)
  * [Body](#body)
  * [Headers](#headers)
  * [Read the response](#read-the-response)
  * [Handle errors](#handle-errors)
* [Advanced features](#advanced-features)
  * [Response metadata](#response-metadata)
  * [Simple retry policy](#simple-retry-policy)
  * [Cancellation tokens](#cancellation-tokens)
  * [Custom requests](#custom-requests)
  * [Synchronous use](#synchronous-use)
* [Extensibility](#extensibility)
  * [Custom formats](#custom-formats)
  * [Custom filters](#custom-filters)
  * [Custom retry/coordination policy](#custom-retrycoordination-policy)
  * [Custom HTTP](#custom-http)
  * [Mocks for unit testing](#mocks-for-unit-testing)

## Get started
### Install
Install it [from NuGet][Pathoschild.Http.FluentClient]:
> Install-Package Pathoschild.Http.FluentClient

The client works on most platforms (including Linux, Mac, and Windows):

| platform                    | min version |
| :-------------------------- | :---------- |
| .NET Core                   | 1.0         |
| .NET Framework              | 4.5         |
| [.NET Standard][]           | 1.3         |
| Mono                        | 4.6         |
| Unity                       | 2018.1      |
| Universal Windows Platform  | 10.0        |
| Xamarin.Android             | 7.0         |
| Xamarin.iOS                 | 10.0        |
| Xamarin.Mac                 | 3.0         |

### Basic usage
Just create the client and chain methods to set up the request/response. For example, this
sends a `GET` request and deserializes the response into a custom `Item` class based on content
negotiation:
```c#
Item item = await new FluentClient()
   .GetAsync("https://example.org/api/items/14")
   .As<Item>();
```

You can also reuse the client for many requests (which improves performance using the built-in
connection pool), and set a base URL in the constructor:
```c#
using var client = new FluentClient("https://example.org/api");

Item item = await client
   .GetAsync("items/14")
   .As<Item>();
```

The client provides methods for `DELETE`, `GET`, `POST`, `PUT`, and `PATCH` out of the box. You can
also use `SendAsync` to craft a custom HTTP request.

### URL arguments
You can add any number of arguments to the request URL with an anonymous object:
```c#
await client
   .PostAsync("items/14")
   .WithArguments(new { page = 1, search = "some search text" });
```

Or with a dictionary:
```c#
await client
   .PostAsync("items/14")
   .WithArguments(new Dictionary<string, object> { … });
```

Or individually:
```c#
await client
   .PostAsync("items/14")
   .WithArgument("page", 1)
   .WithArgument("search", "some search text");
```

### Body
You can add a model body directly in a POST or PUT:
```c#
await client.PostAsync("search", new SearchOptions(…));
```

Or add it to any request:
```c#
await client
   .GetAsync("search")
   .WithBody(new SearchOptions(…));
```

Or provide it in various formats:

format           | example
:--------------- | :------
serialized model | `WithBody(new ExampleModel())`
form URL encoded | `WithBody(p => p.FormUrlEncoded(values))`
file upload      | `WithBody(p => p.FileUpload(files))`
[`HttpContent`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.content#remarks) | `WithBody(httpContent)`

### Headers
You can add any number of headers:
```c#
await client
   .PostAsync("items/14")
   .WithHeader("User-Agent", "Some Bot/1.0.0")
   .WithHeader("Content-Type", "application/json");
```

Or use methods for common headers like `WithAuthentication`, `WithBasicAuthentication`,
`WithBearerAuthentication`, and `SetUserAgent`.

(Basic headers like `Content-Type` and `User-Agent` will be added automatically if you omit them.)

### Read the response
You can parse the response by awaiting an `As*` method:
```c#
await client
   .GetAsync("items")
   .AsArray<Item>();
```

Here are the available formats:

type      | method
--------- | ------
`Item`    | `As<Item>()`
`Item[]`  | `AsArray<Item>()`
`byte[]`  | `AsByteArray()`
`string`  | `AsString()`
`Stream`  | `AsStream()`
`JToken`  | `AsRawJson()`
`JObject` | `AsRawJsonObject()`
`JArray`  | `AsRawJsonArray()`

The `AsRawJson` method can also return `dynamic` to avoid needing a model class:
```c#
dynamic item = await client
   .GetAsync("items/14")
   .AsRawJsonObject();

string author = item.Author.Name;
```

If you don't need the content, you can just await the request:
```c#
await client.PostAsync("items", new Item(…));
```

### Handle errors
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

If you don't want that, you can...
* disable it for one request:

  ```c#
  IResponse response = await client
     .GetAsync("items")
     .WithOptions(ignoreHttpErrors: true);
  ```

* disable it for all requests:

  ```c#
  client.SetOptions(ignoreHttpErrors: true);
  ```

* [use your own error filter](#custom-filters).

## Advanced features
### Response metadata
The previous examples parse the response directly, but sometimes you want to peek at the HTTP
metadata:

```c#
IResponse response = await client.GetAsync("messages/latest");
if (response.IsSuccessStatusCode || response.Status == HttpStatusCode.Found)
   return response.AsArray<T>();
```

### Simple retry policy
The client won't retry failed requests by default, but that's easy to configure:
```c#
client
   .SetRequestCoordinator(
      maxRetries: 3,
      shouldRetry: request => request.StatusCode != HttpStatusCode.OK,
      getDelay: (attempt, response) => TimeSpan.FromSeconds(attempt * 5) // wait 5, 10, and 15 seconds
   );
```

### Chained retry policies
You can also wrap retry logic into `IRetryConfig` implementations:

```c#
/// <summary>A retry policy which retries with incremental backoff.</summary>
public class RetryWithBackoffConfig : IRetryConfig
{
    /// <summary>The maximum number of times to retry a request before failing.</summary>
    public int MaxRetries => 3;

    /// <summary>Get whether a request should be retried.</summary>
    /// <param name="response">The last HTTP response received.</param>
    public bool ShouldRetry(HttpResponseMessage response)
    {
        return request.StatusCode != HttpStatusCode.OK;
    }

    /// <summary>Get the time to wait until the next retry.</summary>
    /// <param name="attempt">The retry index (starting at 1).</param>
    /// <param name="response">The last HTTP response received.</param>
    public TimeSpan GetDelay(int attempt, HttpResponseMessage response)
    {
        return TimeSpan.FromSeconds(attempt * 5); // wait 5, 10, and 15 seconds
    }
}
```

Then you can add one or more retry policies, and they'll each be given the opportunity to retry
a request:

```c#
client
   .SetRequestCoordinator(new[]
   {
      new TokenExpiredRetryConfig(),
      new DatabaseTimeoutRetryConfig(),
      new RetryWithBackoffConfig()
   });
```

Note that there's one retry count across all retry policies. For example, if
`TokenExpiredRetryConfig` retries once before falling back to `RetryWithBackoffConfig`, the latter
will receive `2` as its first retry count. If you need more granular control, see [_custom
retry/coordination policy_](#custom-retrycoordination-policy).

### Cancellation tokens
The client fully supports [.NET cancellation tokens](https://msdn.microsoft.com/en-us/library/dd997364.aspx)
if you need to abort requests:

```c#
var tokenSource = new CancellationTokenSource();
await client
   .PostAsync(…)
   .WithCancellationToken(tokenSource.Token);
tokenSource.Cancel();
```

### Custom requests
You can make changes directly to the HTTP request before it's sent:
```c#
client
   .GetAsync("items")
   .WithCustom(request =>
   {
      request.Method = HttpMethod.Post;
      request.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(30) };
   });
```

### Synchronous use
The client is built around the `async` and `await` keywords, but you can use the client
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

## Extensibility
### Custom formats
The client supports JSON and XML out of the box. If you need more, you can...

* Add any of the existing [media type formatters][]:

  ```c#
  client.Formatters.Add(new YamlFormatter());
  ```

* Create your own by subclassing `MediaTypeFormatter` (optionally using the included
  `MediaTypeFormatterBase` class).

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

### Custom retry/coordination policy
You can implement `IRequestCoordinator` to control how requests are dispatched. For example, here's
a retry coordinator using [Polly](https://github.com/App-vNext/Polly):
```c#
/// <summary>A request coordinator which retries failed requests with a delay between each attempt.</summary>
public class RetryCoordinator : IRequestCoordinator
{
   /// <summary>Dispatch an HTTP request.</summary>
   /// <param name="request">The response message to validate.</param>
   /// <param name="send">Dispatcher that executes the request.</param>
   /// <returns>The final HTTP response.</returns>
   public Task<HttpResponseMessage> ExecuteAsync(IRequest request, Func<IRequest, Task<HttpResponseMessage>> send)
   {
      HttpStatusCode[] retryCodes = { HttpStatusCode.GatewayTimeout, HttpStatusCode.RequestTimeout };
      return Policy
         .HandleResult<HttpResponseMessage>(request => retryCodes.Contains(request.StatusCode)) // should we retry?
         .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt)) // up to 3 retries with increasing delay
         .ExecuteAsync(() => send(request)); // begin handling request
   }
}
```

...and here's how you'd set it:
```c#
client.SetRequestCoordinator(new RetryCoordinator());
```

(You can only have one request coordinator on the client; you should use [HTTP filters](#custom-filters)
instead for most overrides.)

### Custom HTTP
For advanced scenarios, you can customise the underlying [HttpClient][] and
[HttpClientHandler][]. See the next section for an example.

### Mocks for unit testing
Here's how to create mock requests for unit testing using
[RichardSzalay.MockHttp](https://www.nuget.org/packages/RichardSzalay.MockHttp/):
```c#
// create mock
var mockHandler = new MockHttpMessageHandler();
mockHandler.When(HttpMethod.Get, "https://example.org/api/items").Respond(HttpStatusCode.OK, testRequest => new StringContent("[]"));

// create client
var client = new FluentClient("https://example.org/api", new HttpClient(mockHandler));
```

[.NET Standard]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!]: https://blogs.msdn.microsoft.com/pfxteam/2011/01/13/await-and-ui-and-deadlocks-oh-my/
[Don't Block on Async Code]: https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
[media type formatters]: https://www.nuget.org/packages?q=MediaTypeFormatter

[AggregateException]: https://docs.microsoft.com/en-us/dotnet/api/system.aggregateexception
[HttpClient]: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient
[HttpClientHandler]: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler
[MediaTypeFormatter]: https://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: https://www.newtonsoft.com/json
[JSON]: https://en.wikipedia.org/wiki/JSON

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
