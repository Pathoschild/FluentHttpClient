using System;
using System.Linq.Expressions;
using Moq;

namespace Pathoschild.Http.Tests.Delegating.Objects
{
    /// <summary>Provides methods for unit testing an object (the decorator) which delegates work to an inner implementation.</summary>
    /// <typeparam name="TInterface">The interface implemented by the decorator and its inner implementation.</typeparam>
    /// <typeparam name="TDecorator">The delegating object (decorator).</typeparam>
    public abstract class DecoratorTestsBase<TInterface, TDecorator>
        where TInterface : class
        where TDecorator : TInterface
    {
        /*********
        ** Properties
        *********/
        /// <summary>Constructs a new decorator.</summary>
        protected Func<TInterface, TDecorator> GetDecorator;


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="getDecorator">Constructs a new decorator.</param>
        protected DecoratorTestsBase(Func<TInterface, TDecorator> getDecorator)
        {
            this.GetDecorator = getDecorator;
        }

        /// <summary>Assert that a method is correctly delegated.</summary>
        /// <typeparam name="TReturn">The expected return value.</typeparam>
        /// <param name="method">The method to verify.</param>
        protected void VerifyMethod<TReturn>(Expression<Func<TInterface, TReturn>> method)
        {
            // arrange
            Mock<TInterface> mock = new Mock<TInterface>(MockBehavior.Strict);
            mock.Setup(method).Returns(default(TReturn));

            // execute
            TInterface decorator = this.GetDecorator(mock.Object);
            method.Compile().Invoke(decorator);

            // verify
            mock.Verify(method, Times.Once());
        }

        /// <summary>Assert that a property getter is correctly delegated.</summary>
        /// <typeparam name="TReturn">The expected return value.</typeparam>
        /// <param name="property">The property to verify.</param>
        protected void VerifyGet<TReturn>(Expression<Func<TInterface, TReturn>> property)
        {
            // arrange
            Mock<TInterface> mock = new Mock<TInterface>(MockBehavior.Strict);
            mock.SetupGet(property).Returns(default(TReturn));

            // execute
            TInterface decorator = this.GetDecorator(mock.Object);
            property.Compile().Invoke(decorator);

            // verify
            mock.Verify(property, Times.Once());
        }
    }
}
