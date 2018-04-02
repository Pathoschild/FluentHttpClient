using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
#if NETCOREAPP2_0
using Microsoft.AspNetCore.WebUtilities;
#else
using Microsoft.AspNet.WebUtilities;
#endif
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using RichardSzalay.MockHttp;

namespace Pathoschild.Http.Tests.Client
{
    /// <summary>Integration tests verifying that the default <see cref="Request"/> correctly creates and alters the underlying objects.</summary>
    [TestFixture]
    public class RequestTests
    {
        /*********
        ** Unit tests
        *********/
        /**
        ** Request configuration
        ***/
        [Test(Description = "Ensure that the request builder is constructed with the expected initial state.")]
        [TestCase("DELETE", "resource")]
        [TestCase("GET", "resource")]
        [TestCase("HEAD", "resource")]
        [TestCase("PUT", "resource")]
        [TestCase("OPTIONS", "resource")]
        [TestCase("POST", "resource")]
        [TestCase("TRACE", "resource")]
        public void Construct(string methodName, string uri)
        {
            this.ConstructRequest(methodName, uri, false);
        }

        [Test(Description = "Ensure that WithArgument appends the query arguments to the request message and does not incorrectly alter request state.")]
        [TestCase("DELETE", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("GET", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("HEAD", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("PUT", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("OPTIONS", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("POST", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("TRACE", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        public void WithArgument(string methodName, string keyA, string valueA, string keyB, string valueB)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArgument(keyA, valueA)
                .WithArgument(keyB, valueB);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[keyA], Is.Not.Null.And.EqualTo(valueA), "The first argument doesn't match the input.");
            Assert.That(arguments[keyB], Is.Not.Null.And.EqualTo(valueB), "The second argument doesn't match the input.");
        }

        [Test(Description = "Ensure that WithArgument correctly allows duplicate keys.")]
        [TestCase("DELETE", "keyA", "value A", "value B")]
        [TestCase("GET", "keyA", "value A", "value B")]
        [TestCase("HEAD", "keyA", "value A", "value B")]
        [TestCase("PUT", "keyA", "value A", "value B")]
        [TestCase("OPTIONS", "keyA", "value A", "value B")]
        [TestCase("POST", "keyA", "value A", "value B")]
        [TestCase("TRACE", "keyA", "value A", "value B")]
        public void WithArgument_AllowsDuplicateKeys(string methodName, string keyA, string valueA, string valueB)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArgument(keyA, valueA)
                .WithArgument(keyA, valueB);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[keyA], Is.Not.Null.And.EqualTo(new[] { valueA, valueB }), "The values don't match.");
        }

        [Test(Description = "Ensure that WithArgument accepts arguments of various types.")]
        [TestCase("GET", "param", 42L)]
        [TestCase("GET", "param", 42d)]
        [TestCase("GET", "param", 42u)]
        [TestCase("GET", "param", "value")]
        public void WithArgument_AcceptsVariousValueTypes(string methodName, string key, object value)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArgument(key, value);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[key], Is.Not.Null.And.EqualTo(value.ToString()), "The arguments don't match the input.");
        }

        [Test(Description = "Ensure that WithArgument ignores null arguments.")]
        [TestCase("GET", "param", "aaa", true)]
        [TestCase("GET", "param", null, true)]
        [TestCase("GET", "param", "bbb", false)]
        [TestCase("GET", "param", null, false)]
        public void WithArgument_IgnoresArgumentWithNullValue(string methodName, string key, string value, bool ignoreNullArguments)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithOptions(new RequestOptions { IgnoreNullArguments = ignoreNullArguments })
                .WithArgument(key, value);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);

            this.AssertQuerystringArgument(arguments, key, value, ignoreNullArguments);
        }

        [Test(Description = "Ensure that WithArguments (with a dictionary) appends the query arguments to the request message and does not incorrectly alter request state.")]
        [TestCase("DELETE", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("GET", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("HEAD", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("PUT", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("OPTIONS", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("POST", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        [TestCase("TRACE", "keyA", "24", "key:!@#$%^&*()_+-=?'\"", "value:!@#$%^&*()_+-=?'\"")]
        public void WithArguments_Dictionary(string methodName, string keyA, string valueA, string keyB, string valueB)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new Dictionary<string, object> { { keyA, valueA }, { keyB, valueB } });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[keyA], Is.Not.Null.And.EqualTo(valueA), "The first argument doesn't match the input.");
            Assert.That(arguments[keyB], Is.Not.Null.And.EqualTo(valueB), "The second argument doesn't match the input.");
        }

        [Test(Description = "Ensure that WithArguments (with an object) appends the query arguments to the request message and does not incorrectly alter request state.")]
        [TestCase("DELETE", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("GET", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("HEAD", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("PUT", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("OPTIONS", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("POST", "24", "!@#$%^&*()_+-=?'\"")]
        [TestCase("TRACE", "24", "!@#$%^&*()_+-=?'\"")]
        public void WithArguments_Object(string methodName, string valueA, string valueB)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new { keyA = valueA, keyB = valueB });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments["keyA"], Is.Not.Null.And.EqualTo(valueA), "The 'keyA' argument doesn't match the input.");
            Assert.That(arguments["keyB"], Is.Not.Null.And.EqualTo(valueB), "The 'keyB' argument doesn't match the input.");
        }

        [Test(Description = "Ensure that WithArguments correctly allows duplicate keys.")]
        [TestCase("DELETE", "keyA", "value A", "value B")]
        [TestCase("GET", "keyA", "value A", "value B")]
        [TestCase("HEAD", "keyA", "value A", "value B")]
        [TestCase("PUT", "keyA", "value A", "value B")]
        [TestCase("OPTIONS", "keyA", "value A", "value B")]
        [TestCase("POST", "keyA", "value A", "value B")]
        [TestCase("TRACE", "keyA", "value A", "value B")]
        public void WithArguments_AllowsDuplicateKeys(string methodName, string keyA, string valueA, string valueB)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new[]
                {
                    new KeyValuePair<string, object>(keyA, valueA),
                    new KeyValuePair<string, object>(keyA, valueB)
                });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[keyA], Is.Not.Null.And.EqualTo(new[] { valueA, valueB }), "The values don't match.");
        }

        [Test(Description = "Ensure that WithArgument ignores null arguments.")]
        [TestCase("GET", "paramA", "aaa", "paramB", null, true)]
        [TestCase("GET", "paramA", null, "paramB", "bbb", true)]
        [TestCase("GET", "paramA", "aaa", "paramB", null, false)]
        [TestCase("GET", "paramA", null, "paramB", "bbb", false)]
        public void WithArguments_IgnoresArgumentWithNullValue(string methodName, string keyA, string valueA, string keyB, string valueB, bool ignoreNullArguments)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithOptions(new RequestOptions { IgnoreNullArguments = ignoreNullArguments })
                .WithArguments(new[]
                {
                    new KeyValuePair<string, object>(keyA, valueA),
                    new KeyValuePair<string, object>(keyB, valueB)
                });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);

            this.AssertQuerystringArgument(arguments, keyA, valueA, ignoreNullArguments);
            this.AssertQuerystringArgument(arguments, keyB, valueB, ignoreNullArguments);
        }

        [Test(Description = "Ensure that WithArgument ignores null arguments.")]
        [TestCase("GET", "aaa", null, true)]
        [TestCase("GET", null, "bbb", true)]
        [TestCase("GET", "aaa", null, false)]
        [TestCase("GET", null, "bbb", false)]
        public void WithArguments_Object_IgnoresArgumentWithNullValue(string methodName, string valueA, string valueB, bool ignoreNullArguments)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithOptions(new RequestOptions { IgnoreNullArguments = ignoreNullArguments })
                .WithArguments(new { keyA = valueA, keyB = valueB });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);

            this.AssertQuerystringArgument(arguments, "keyA", valueA, ignoreNullArguments);
            this.AssertQuerystringArgument(arguments, "keyB", valueB, ignoreNullArguments);
        }

        [Test(Description = "Ensure that WithArguments accepts a dictionary with object values.")]
        [TestCase("GET", "param", 42)]
        [TestCase("GET", "param", 42d)]
        [TestCase("GET", "param", 42u)]
        [TestCase("GET", "param", "value")]
        public void WithArguments_Dictionary_AcceptsDictionaryOfObject(string methodName, string key, object value)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new Dictionary<string, object> { { key, value } });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[key], Is.Not.Null.And.EqualTo(value.ToString()), "The dictionary values don't match the input.");
        }

        [Test(Description = "Ensure that WithArguments accepts a dictionary with int values.")]
        [TestCase("GET", "param", 42)]
        public void WithArguments_Dictionary_AcceptsDictionaryOfInt(string methodName, string key, int value)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new Dictionary<string, int> { { key, value } });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[key], Is.Not.Null.And.EqualTo(value.ToString()), "The dictionary values don't match the input.");
        }

        [Test(Description = "Ensure that WithArguments accepts key/value pairs with int values.")]
        [TestCase("GET", "param", 42)]
        public void WithArguments_KeyValuePairs_AcceptsIntValues(string methodName, string key, int value)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithArguments(new[]
                {
                    new KeyValuePair<string, int>(key, value )
                });

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            var arguments = QueryHelpers.ParseQuery(request.Message.RequestUri.Query);
            Assert.That(arguments[key], Is.Not.Null.And.EqualTo(value.ToString()), "The dictionary values don't match the input.");
        }

        [Test(Description = "Ensure that WithBodyContent sets the request body and does not incorrectly alter request state.")]
        [TestCase("DELETE", "body value")]
        [TestCase("GET", "body value")]
        [TestCase("HEAD", "body value")]
        [TestCase("PUT", "body value")]
        [TestCase("OPTIONS", "body value")]
        [TestCase("POST", "body value")]
        [TestCase("TRACE", "body value")]
        public async Task WithBodyContent(string methodName, object body)
        {
            // set up
            HttpContent content = new ObjectContent(typeof(string), body, new JsonMediaTypeFormatter());

            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithBodyContent(content);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
        }

        [Test(Description = "Ensure that WithBody sets the request body and does not incorrectly alter request state.")]
        [TestCase("DELETE", "body value")]
        [TestCase("GET", "body value")]
        [TestCase("HEAD", "body value")]
        [TestCase("PUT", "body value")]
        [TestCase("OPTIONS", "body value")]
        [TestCase("POST", "body value")]
        [TestCase("TRACE", "body value")]
        public async Task WithBody(string methodName, object body)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithBody(body);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
        }

        [Test(Description = "Ensure that WithBody sets the request body and does not incorrectly alter request state.")]
        [TestCase("DELETE", "body value")]
        [TestCase("GET", "body value")]
        [TestCase("HEAD", "body value")]
        [TestCase("PUT", "body value")]
        [TestCase("OPTIONS", "body value")]
        [TestCase("POST", "body value")]
        [TestCase("TRACE", "body value")]
        public async Task WithBody_AndFormatter(string methodName, object body)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithBody(body, new JsonMediaTypeFormatter());

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + body.ToString() + '"'), "The message body is invalid.");
        }

        [Test(Description = "Ensure that WithCustom persists the custom changes and does not incorrectly alter request state.")]
        [TestCase("DELETE", "body value")]
        [TestCase("GET", "body value")]
        [TestCase("HEAD", "body value")]
        [TestCase("PUT", "body value")]
        [TestCase("OPTIONS", "body value")]
        [TestCase("POST", "body value")]
        [TestCase("TRACE", "body value")]
        public async Task WithCustom(string methodName, string customBody)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithCustom(r => r.Content = new ObjectContent<string>(customBody, new JsonMediaTypeFormatter()));

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(await request.Message.Content.ReadAsStringAsync(), Is.EqualTo('"' + customBody + '"'), "The customized message body is invalid.");
        }

        [Test(Description = "Ensure that WithHeader sets the expected header and does not incorrectly alter request state.")]
        [TestCase("DELETE", "VIA", "header value")]
        [TestCase("GET", "VIA", "header value")]
        [TestCase("HEAD", "VIA", "header value")]
        [TestCase("PUT", "VIA", "header value")]
        [TestCase("OPTIONS", "VIA", "header value")]
        [TestCase("POST", "VIA", "header value")]
        [TestCase("TRACE", "VIA", "header value")]
        public void WithHeader(string methodName, string key, string value)
        {
            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithHeader(key, value);
            var header = request.Message.Headers.FirstOrDefault(p => p.Key == key);

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(header, Is.Not.Null, "The header is invalid.");
            Assert.That(header.Value, Is.Not.Null.Or.Empty, "The header value is invalid.");
            Assert.That(header.Value.First(), Is.EqualTo(value), "The header value is invalid.");
        }

        [Test(Description = "Ensure that WithHeader for a Cookie header sets the expected header and does not incorrectly alter request state.")]
        [TestCase("DELETE", "cookie key", "value")]
        [TestCase("GET", "cookie key", "value")]
        [TestCase("HEAD", "cookie key", "value")]
        [TestCase("PUT", "cookie key", "value")]
        [TestCase("OPTIONS", "cookie key", "value")]
        [TestCase("POST", "cookie key", "value")]
        [TestCase("TRACE", "cookie key", "value")]
        public void WithHeader_Cookie(string methodName, string key, string value)
        {
            // arrange
            string expectedValue = $"{key}={value}";

            // execute
            IRequest request = this
                .ConstructRequest(methodName)
                .WithHeader("Cookie", expectedValue);
            var header = request.Message.Headers.FirstOrDefault(p => p.Key == "Cookie");

            // verify
            this.AssertEqual(request.Message, methodName, ignoreArguments: true);
            Assert.That(header, Is.Not.Null, "The header is invalid.");
            Assert.That(header.Value, Is.Not.Null.Or.Empty, "The header value is invalid.");
            Assert.That(header.Value.First(), Is.EqualTo(expectedValue), "The header value is invalid.");
        }

        [Test(Description = "Ensure that WithHttpErrorAsException throws an exception by default.")]
        public void WithHttpErrorAsException_ThrowsExceptionByDefault()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "https://example.org").Respond(HttpStatusCode.NotFound);
            var client = new FluentClient("https://example.org", new HttpClient(mockHttp));

            // verify
            ApiException ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetAsync("/"), "The client didn't throw an exception for a non-success code");
            Assert.AreEqual(HttpStatusCode.NotFound, ex.Status, "The HTTP status on the exception doesn't match the response.");
            Assert.NotNull(ex.ResponseMessage, "The HTTP response message on the exception is null.");
            Assert.NotNull(ex.Response, "The HTTP response on the exception is null.");
        }

        [Test(Description = "Ensure that WithOptions can disable HTTP errors as exceptions.")]
        public async Task WithOptions_DisablesException()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "https://example.org").Respond(HttpStatusCode.NotFound);
            var client = new FluentClient("https://example.org", new HttpClient(mockHttp));

            // verify
            IResponse response = await client.GetAsync("/").WithOptions(new RequestOptions { IgnoreHttpErrors = true });
            Assert.NotNull(response, "The HTTP response is null.");
            Assert.NotNull(response.Message, "The HTTP response message is null.");
            Assert.AreEqual(HttpStatusCode.NotFound, response.Status, "The HTTP status doesn't match the response.");
        }

        [Test(Description = "A GET request can be executed multiple times.")]
        public async Task Request_CanResubmit_Get()
        {
            // arrange
            int counter = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "https://api.fictitious-vendor.com/v1/endpoint").Respond(HttpStatusCode.OK, testRequest => new StringContent($"This is request #{++counter}"));

            var httpClient = new HttpClient(mockHttp);
            var fluentClient = new FluentClient("https://api.fictitious-vendor.com/v1/", httpClient);

            // act
            var request = fluentClient.GetAsync("endpoint");
            string valueA = await request.AsString();
            string valueB = await request.AsString();

            // assert
            Assert.AreEqual("This is request #1", valueA, "The first request got an unexpected value.");
            Assert.AreEqual("This is request #2", valueB, "The second request got an unexpected value.");
        }

        [Test(Description = "A POST request can be executed multiple times.")]
        public async Task Request_CanResubmit_Post([Values("string", "stream")] string contentType)
        {
            // arrange
            int counter = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.fictitious-vendor.com/v1/endpoint").Respond(HttpStatusCode.OK, testRequest => new StringContent($"This is request #{++counter}"));

            var httpClient = new HttpClient(mockHttp);
            var fluentClient = new FluentClient("https://api.fictitious-vendor.com/v1/", httpClient);

            // act
            var request = fluentClient.PostAsync("endpoint");
            switch (contentType)
            {
                case "string":
                    request = request.WithBodyContent(new StringContent("example string"));
                    break;

                case "stream":
                    Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("Example stream content"));
                    request = request.WithBodyContent(new StreamContent(stream));
                    break;

                default:
                    throw new NotSupportedException("Unknown content type.");
            }

            string valueA = await request.AsString();
            string valueB = await request.AsString();

            // assert
            Assert.AreEqual("This is request #1", valueA, "The first request got an unexpected value.");
            Assert.AreEqual("This is request #2", valueB, "The second request got an unexpected value.");
        }

        [Test(Description = "A dispatched request is sent to the expected URL.")]
        // empty resource
        [TestCase("https://example.org/index.php", null, ExpectedResult = "https://example.org/index.php")]
        [TestCase("https://example.org/index.php", "", ExpectedResult = "https://example.org/index.php")]
        [TestCase("https://example.org/index.php", "   ", ExpectedResult = "https://example.org/index.php")]

        // path resource
        [TestCase("https://example.org", "api", ExpectedResult = "https://example.org/api")]
        [TestCase("https://example.org/", "api", ExpectedResult = "https://example.org/api")]
        [TestCase("https://example.org/index", "api", ExpectedResult = "https://example.org/index/api")]
        [TestCase("https://example.org/index.php", "api", ExpectedResult = "https://example.org/index.php/api")]
        [TestCase("https://example.org/index.php?x=1", "api", ExpectedResult = "https://example.org/index.php/api")]

        // query resource
        [TestCase("https://example.org/index", "?api", ExpectedResult = "https://example.org/index?api")]
        [TestCase("https://example.org/index?api=1", "&api=2", ExpectedResult = "https://example.org/index?api=1&api=2")]

        // URL resource
        [TestCase("https://example.org/index.php", "https://example.org/api", ExpectedResult = "https://example.org/api")]

        // special case: just combine fragments
        [TestCase("https://example.org/index", "#api", ExpectedResult = "https://example.org/index#api")]
        [TestCase("https://example.org/index.php?x=1", "#api", ExpectedResult = "https://example.org/index.php?x=1#api")]
        [TestCase("https://example.org/api#fragment", "api", ExpectedResult = "https://example.org/api#fragmentapi")]
        [TestCase("https://example.org/api/#fragment", "&x=1", ExpectedResult = "https://example.org/api/#fragment&x=1")]
        public async Task<string> Request_Url(string baseUrl, string url)
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "*").Respond(HttpStatusCode.OK, req => new StringContent(req.RequestUri.ToString()));
            var fluentClient = new FluentClient(baseUrl, new HttpClient(mockHttp));

            // act
            return await fluentClient.GetAsync(url).AsString();
        }

        [Test(Description = "An appropriate exception is thrown when the resource isn't valid for the current base URL.")]
        [TestCase("https://example.org?x=1", "?x=1")]
        [TestCase("https://example.org", "&x=1")]
        public void Request_Url_WhenInvalid(string baseUrl, string url)
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "*").Respond(HttpStatusCode.OK, req => new StringContent(req.RequestUri.ToString()));
            var fluentClient = new FluentClient(baseUrl, new HttpClient(mockHttp));

            // assert
            Assert.ThrowsAsync<FormatException>(async () => await fluentClient.GetAsync(url).AsString());
        }

        /***
        ** Request infrastructure
        ***/
        [Test(Description = "An appropriate exception is thrown when the request task faults or aborts. This is regardless of configuration.")]
        [TestCase(true, typeof(NotSupportedException))]
        [TestCase(false, typeof(NotSupportedException))]
        public void Task_Async_FaultHandled(bool throwError, Type exceptionType)
        {
            // arrange
            IRequest response = this.ConstructResponseFromTask(() => throw (Exception)Activator.CreateInstance(exceptionType));

            // act
            Assert.ThrowsAsync<NotSupportedException>(async () => await response);
        }

        [Test(Description = "The asynchronous methods really are asynchronous.")]
        public void Task_Async_IsAsync()
        {
            // arrange
            IRequest request = this.ConstructResponseFromTask(Task
                .Delay(5000)
                .ContinueWith<HttpResponseMessage>(task =>
                {
                    Assert.Fail("The response was not invoked asynchronously.");
                    return null;
                })
            );

            // act
            Task<HttpResponseMessage> result = request.AsMessage();

            // assert
            Assert.AreNotEqual(result.Status, TaskStatus.Created);
            Assert.False(result.IsCompleted, "The request was not executed asynchronously.");
        }

        [Test(Description = "The request succeeds when passed a HTTP request that is in progress.")]
        public async Task Task_Async()
        {
            // arrange
            IRequest request = this.ConstructResponseFromTask(() => new HttpResponseMessage(HttpStatusCode.OK));

            // act
            HttpResponseMessage result = await request.AsMessage();

            // assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessStatusCode);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Construct an <see cref="IRequest"/> instance and assert that its initial state is valid.</summary>
        /// <param name="methodName">The expected HTTP method.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="inconclusiveOnFailure">Whether to throw an <see cref="InconclusiveException"/> if the initial state is invalid.</param>
        /// <exception cref="InconclusiveException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>true</c>.</exception>
        /// <exception cref="AssertionException">The initial state of the constructed client is invalid, and <paramref name="inconclusiveOnFailure"/> is <c>false</c>.</exception>
        private IRequest ConstructRequest(string methodName, string uri = "http://example.org/", bool inconclusiveOnFailure = true)
        {
            try
            {
                // set up
                HttpMethod method = new HttpMethod(methodName);
                HttpRequestMessage message = new HttpRequestMessage(method, uri);

                // execute
                IRequest request = new Request(message, new MediaTypeFormatterCollection(), r => new Task<HttpResponseMessage>(() => null), new IHttpFilter[0]);

                // verify
                this.AssertEqual(request.Message, method, uri);

                return request;
            }
            catch (AssertionException exc)
            {
                if (inconclusiveOnFailure)
                    Assert.Inconclusive("The client could not be constructed: {0}", exc.Message);
                throw;
            }
        }

        /// <summary>Construct an <see cref="IResponse"/> instance around an asynchronous task.</summary>
        /// <remarks>The asynchronous task to wrap.</remarks>
        private IRequest ConstructResponseFromTask(Task<HttpResponseMessage> task)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/");
            return new Request(request, new MediaTypeFormatterCollection(), p => task, new IHttpFilter[0]);
        }

        /// <summary>Construct an <see cref="IResponse"/> instance around an asynchronous task.</summary>
        /// <remarks>The work to start in a new asynchronous task.</remarks>
        private IRequest ConstructResponseFromTask(Func<HttpResponseMessage> task)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/");
            return new Request(request, new MediaTypeFormatterCollection(), p => Task<HttpResponseMessage>.Factory.StartNew(task), new IHttpFilter[0]);
        }

        /// <summary>Assert that an HTTP request's state matches the expected values.</summary>
        /// <param name="request">The HTTP request message to verify.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
        private void AssertEqual(HttpRequestMessage request, HttpMethod method, string uri = "http://example.org/", bool ignoreArguments = false)
        {
            Assert.That(request, Is.Not.Null, "The request message is null.");
            Assert.That(request.Method, Is.EqualTo(method), "The request method is invalid.");
            Assert.That(ignoreArguments ? $"{request.RequestUri.Scheme}://{request.RequestUri.Authority}{request.RequestUri.AbsolutePath}" : request.RequestUri.ToString(), Is.EqualTo(uri), "The request URI is invalid.");
        }

        /// <summary>Assert that an HTTP request's state matches the expected values.</summary>
        /// <param name="request">The HTTP request message to verify.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="uri">The expected URI.</param>
        /// <param name="ignoreArguments">Whether to ignore query string arguments when validating the request URI.</param>
        private void AssertEqual(HttpRequestMessage request, string method, string uri = "http://example.org/", bool ignoreArguments = false)
        {
            this.AssertEqual(request, new HttpMethod(method), uri, ignoreArguments);
        }

        /// <summary>Assert that a query string is equal to the expected value (or excluded if null and <paramref name="ignoreNullArguments"/> is true).</summary>
        /// <param name="arguments">The request arguments.</param>
        /// <param name="key">The key to assert.</param>
        /// <param name="value">The expected value.</param>
        /// <param name="ignoreNullArguments">Whether null argument values should be ignored.</param>
        private void AssertQuerystringArgument(IDictionary<string, StringValues> arguments, string key, string value, bool ignoreNullArguments)
        {
            if (ignoreNullArguments && value == null)
            {
                Assert.That(arguments.ContainsKey(key), Is.False, $"Argument {key} with null value should have been ignored");
            }
            else
            {
                Assert.That(arguments.ContainsKey(key), Is.True, $"Argument {key} with null value shouldn't have been ignored");
                Assert.That(arguments[key], Is.EqualTo(value ?? ""), $"Argument {key}'s value should be '{value}'.");
            }
        }
    }
}
