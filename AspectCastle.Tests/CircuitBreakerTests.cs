using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class CircuitBreakerTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        public class Tester
        {
            public virtual void Invoke(bool @throw)
            {
                if (@throw)
                    throw new InvalidCastException();
            }
        }

        private static Tester CreateProxy(int failureThreshold, int timeout)
        {
            CircuitBreakerInterceptor temp;
            return CreateProxy(failureThreshold, timeout, out temp);
        }

        private static Tester CreateProxy(int failureThreshold, int timeout, out CircuitBreakerInterceptor interceptor)
        {
            interceptor = new CircuitBreakerInterceptor
            {
                DefaultMarkerInstance = new WithCircuitBreakerAttribute
                {
                    FailureThreshold = failureThreshold,
                    TimeoutMilliseconds = timeout,
                }
            };
            return Factory.CreateClassProxy<Tester>(interceptor);
        }

        [Test]
        public void Circuit_opens_after_threshold_has_been_exceeded()
        {
            var tester = CreateProxy(5, 1000);
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<CircuitIsOpenException>(() => tester.Invoke(true));
        }

        [Test]
        public void Circuit_closes_after_elasped_timeout()
        {
            CircuitBreakerInterceptor interceptor;
            var tester = CreateProxy(2, 200, out interceptor);
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<CircuitIsOpenException>(() => tester.Invoke(true));
            Assert.That(interceptor.GetMarkers().Values.First().State, Is.EqualTo(CircuitState.Open));
            Thread.Sleep(250);
            Assert.DoesNotThrow(() => tester.Invoke(false));
            Assert.That(interceptor.GetMarkers().Values.First().State, Is.EqualTo(CircuitState.Closed));

            // should reset
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<CircuitIsOpenException>(() => tester.Invoke(true));
        }

        [Test]
        public void Circuit_goes_to_half_open_state_after_recoving_from_open_state()
        {
            CircuitBreakerInterceptor interceptor;
            var tester = CreateProxy(2, 200, out interceptor);
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<InvalidCastException>(() => tester.Invoke(true));
            Assert.Throws<CircuitIsOpenException>(() => tester.Invoke(true));
            Thread.Sleep(250);
            Assert.Throws<CircuitIsOpenException>(() => tester.Invoke(true)); // should reopen after 1 failure
            Assert.That(interceptor.GetMarkers().Values.First().State, Is.EqualTo(CircuitState.Open));
        }
    }
}