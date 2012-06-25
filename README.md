**Pathoschild.FluentHttpClient** is a fluent wrapper over the [.NET 4.5 HttpClient](http://code.msdn.microsoft.com/Introduction-to-HttpClient-4a2d9cee) for creating strongly-typed easy-to-use API clients.

## Installation
The fluent client is available as a set of self-contained NuGet packages:
* [Pathoschild.Http.FluentClient](https://nuget.org/packages/Pathoschild.Http.FluentClient) includes `Pathoschild.Http.Client`;
* [Pathoschild.Http.Formatters.Core](https://nuget.org/packages/Pathoschild.Http.Formatters.Core) includes `Pathoschild.Http.Formatters`;
* [Pathoschild.Http.Formatters.JsonNet](https://nuget.org/packages/Pathoschild.Http.Formatters.JsonNet) includes `Pathoschild.Http.Formatters.JsonNet` and `Pathoschild.Http.Formatters`.

## Usage
The client is a fluent wrapper over `HttpClient`. You start by creating a client, and chain methods to configure your request and response.

```c#
     IClient client = new FluentClient("http://example.org/api/");
```

The most common usage is a synchronous HTTP GET, with the response deserialized into a class instance:

```c#
     Idea idea = client
        .Get("ideas/14")
        .RetrieveAs<Idea>();
```

You can fluently customize the request and even directly alter the [`HttpRequestMessage`](http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx):

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

### Reference
The code documentation provides more details on usage: see [`IClient`](https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6), [`IRequestBuilder`](https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequestBuilder.cs#L10), and [`IResponse`](https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IResponse.cs#L10).

## Extending the client
### Media type formatters
The client uses the abstract [`MediaTypeFormatter`](http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx) for serializing and deserializing models for HTTP messages. This is the same type used by the underlying `HttpClient` and the .NET Web API, so there are many implementations already available. You can also create your own implementation ([`MediaTypeFormatterBase`](https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters/MediaTypeFormatterBase.cs#L10) might help).

For example, to replace the default `DataContractJsonSerializer` with the [JSON.NET serializer](https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetFormatter.cs#L10):

```c#
     IClient client = new FluentClient("http://example.org/api/");
     client.Formatters.Remove(client.Formatters.JsonFormatter);
     client.Formatters.Add(new JsonNetFormatter());
```