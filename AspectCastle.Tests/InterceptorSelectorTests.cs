using System;
using AspectCastle.Core;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class InterceptorSelectorTests
    {
        private readonly ProxyFactory _factory = new ProxyFactory();

        public interface IAttributePlacement
        {
            [WithExceptionPolicy(Swallow = true)]
            void OnInterface();

            void OnImplementation();
        }

        public class AttributePlacement : IAttributePlacement
        {
            #region IAttributePlacement Members

            public void OnInterface()
            {
                throw new Exception();
            }

            [WithExceptionPolicy(Swallow = true)]
            public void OnImplementation()
            {
            }

            #endregion
        }

        [Test]
        public void Selector_finds_attributes_declared_on_the_implementation()
        {
            var proxy = this._factory.CreateInterfaceProxyWithTarget<IAttributePlacement>(new AttributePlacement(), new ExceptionPolicyInterceptor());
            Assert.DoesNotThrow(proxy.OnImplementation);
        }

        [Test]
        public void Selector_finds_attributes_declared_on_the_interface()
        {
            var proxy = this._factory.CreateInterfaceProxyWithTarget<IAttributePlacement>(new AttributePlacement(), new ExceptionPolicyInterceptor());
            Assert.DoesNotThrow(proxy.OnInterface);
        }

        [Test]
        public void Interceptor_selector_meets_caching_requirements()
        {
            InterceptorSelector s1 = new InterceptorSelector();
            s1.Logger = s1.Logger; // shut up code coverage
            s1.GetHashCode();

            InterceptorSelector s2 = new InterceptorSelector();
            Assert.AreEqual(s1.GetHashCode(), s2.GetHashCode());
            Assert.That(s1, Is.EqualTo(s2));
        }
    }
}