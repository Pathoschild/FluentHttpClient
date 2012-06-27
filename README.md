**Pathoschild.FluentHttpClient** is a fluent wrapper over the .NET 4.5 [HttpClient][] for creating strongly-typed easy-to-use API clients.

## Installation
The fluent client is available as a set of self-contained NuGet packages:
* [Pathoschild.Http.FluentClient][] includes `Pathoschild.Http.Client`;
* [Pathoschild.Http.Formatters.Core][] includes `Pathoschild.Http.Formatters`;
* [Pathoschild.Http.Formatters.JsonNet][] includes `Pathoschild.Http.Formatters.JsonNet` and `Pathoschild.Http.Formatters`.

## Usage
The client is a fluent wrapper over HttpClient. You start by creating a client, and chain methods to configure your request and response.

```c#
     IClient client = new FluentClient("http://example.org/api/");
```

The most common usage is a synchronous HTTP GET, with the response deserialized into a class instance:

```c#
     Idea idea = client
        .Get("ideas/14")
        .RetrieveAs<Idea>();
```

You can fluently customize the request and even directly alter the [HttpRequestMessage][]:

```c#
     Idea idea = client
        .Post("ideas", new Idea())
        .WithHeader("Content-Type", "application/json")
        .WithArgument("tenant", "company-name")
        .WithCustom(message =>
        {
           message.Method = HttpMethod.Get;
           message.RequestUri = new Uri("http://example.org/api2/", "ideas");
        })
        .RetrieveAs<Idea>();
```

The response can also be fluently configured:

```c#
     string jsonIdea = client
        .Get("ideas/14")
        .Retrieve()
        .AsString(); // or As<T>, AsList<T>, AsMessage, AsByteArray, AsStream
```

You also can do everything asynchronously:

```c#
     Task<Idea> query = client
        .Get("ideas/14")
        .Retrieve()
        .AsAsync<Idea>();
```

The code documentation provides more details on usage: see [IClient][], [IRequestBuilder][], and [IResponse][].

## Configurable formatting
The client uses .NET's [MediaTypeFormatter][]s for serializing and deserializing HTTP messages. This is the same type used by the underlying HttpClient and the .NET Web API, and the client uses Microsoft's default formatters out of the box. When creating a client for an ASP.NET Web API, this lets you seamlessly use the same formatters on both sides.

You can use any of the many implementations already available, create your own ([MediaTypeFormatterBase][] might help), or use one of the formatters below. For example, to replace the default JSON formatter with the formatter below:
```c#
     IClient client = new FluentClient("http://example.org/api/");
     client.Formatters.Remove(client.Formatters.JsonFormatter);
     client.Formatters.Add(new JsonNetFormatter());
```

### Json.NET
The [Pathoschild.Http.Formatters.JsonNet][] package provides three formats using [Json.NET][]: [BSON][] (`application/bson`), [JSON][] (`application/json`, `text/json`), and [JSONP][] (`application/javascript`, `application/ecmascript`, `text/javascript`, `text/ecmascript`). JSONP requests can include an optional `callback` query parameter that specifies the JavaScript method name to invoke.
```c#
     client.Formatters.Add(new JsonNetBsonFormatter());
     client.Formatters.Add(new JsonNetFormatter());
```

[HttpClient]: http://code.msdn.microsoft.com/Introduction-to-HttpClient-4a2d9cee
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx
[HttpRequestMessage]: http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[BSON]: https://en.wikipedia.org/wiki/BSON
[JSON]: https://en.wikipedia.org/wiki/JSON
[JSONP]: https://en.wikipedia.org/wiki/JSONP

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequestBuilder]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequestBuilder.cs#L10
[IResponse]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IResponse.cs#L10
[MediaTypeFormatterBase]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters/MediaTypeFormatterBase.cs#L10
[JsonNetBsonFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetBsonFormatter.cs#L11
[JsonNetFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetFormatter.cs#L10

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
[Pathoschild.Http.Formatters.Core]: https://nuget.org/packages/Pathoschild.Http.Formatters.Core
[Pathoschild.Http.Formatters.JsonNet]: https://nuget.org/packages/Pathoschild.Http.Formatters.JsonNet