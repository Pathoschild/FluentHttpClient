using System.Net.Http;
using Moq;
using NUnit.Framework;
using Pathoschild.Http.Client;
using Pathoschild.Http.Client.Delegating;
using Pathoschild.Http.Tests.Delegating.Objects;

namespace Pathoschild.Http.Tests.Delegating
{
    /// <summary>Unit tests verifying that the <see cref="DelegatingFluentClient"/> correctly delegates calls.</summary>
    [TestFixture]
    public class ClientTests : DecoratorTestsBase<IClient, TestDelegatingClient>
    {
        /*********
        ** Unit tests
        *********/
        /***
        ** Properties
        ***/
        [Test(Description = "Ensure that the BaseClient property is delegated.")]
        public void BaseClient()
        {
            this.VerifyGet(p => p.BaseClient);
        }

        [Test(Description = "Ensure that the MessageHandler property is delegated.")]
        public void MessageHandler()
        {
            this.VerifyGet(p => p.MessageHandler);
        }

        [Test(Description = "Ensure that the Formatters property is delegated.")]
        public void Formatters()
        {
            this.VerifyGet(p => p.Formatters);
        }


        /***
        ** Methods
        ***/
        [Test(Description = "Ensure that the DeleteAsync method is delegated.")]
        public void Delete()
        {
            this.VerifyMethod(p => p.DeleteAsync(It.IsAny<string>()));
        }

        [Test(Description = "Ensure that the GetAsync method is delegated.")]
        public void Get()
        {
            this.VerifyMethod(p => p.GetAsync(It.IsAny<string>()));
        }

        [Test(Description = "Ensure that the PostAsync method is delegated.")]
        public void Post()
        {
            this.VerifyMethod(p => p.PostAsync(It.IsAny<string>()));
        }

        [Test(Description = "Ensure that the PostAsync method  (when passed an HTTP body content) is delegated.")]
        public void Post_WithBody()
        {
            this.VerifyMethod(p => p.PostAsync(It.IsAny<string>(), It.IsAny<object>()));
        }

        [Test(Description = "Ensure that the PutAsync method is delegated.")]
        public void Put()
        {
            this.VerifyMethod(p => p.PutAsync(It.IsAny<string>()));
        }

        [Test(Description = "Ensure that the PutAsync method (when passed an HTTP body content) is delegated.")]
        public void Put_WithBody()
        {
            this.VerifyMethod(p => p.PutAsync(It.IsAny<string>(), It.IsAny<object>()));
        }

        [Test(Description = "Ensure that the SendAsync method is delegated.")]
        public void Send()
        {
            this.VerifyMethod(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>()));
        }

        [Test(Description = "Ensure that the SendAsync method (when passed an HTTP request message) is delegated.")]
        public void Send_WithMessage()
        {
            this.VerifyMethod(p => p.SendAsync(It.IsAny<HttpRequestMessage>()));
        }


        /*********
        ** Protected methods
        *********/
        public ClientTests()
            : base(mock => new TestDelegatingClient(mock)) { }
    }
}
