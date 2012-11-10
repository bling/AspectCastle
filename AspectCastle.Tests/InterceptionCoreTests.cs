using System.Linq;
using Castle.Core.Logging;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class InterceptionCoreTests
    {
        public interface ITester
        {
            void Method1();
        }

        public class Tester : ITester
        {
            public virtual void Method1()
            {
            }

            public virtual void Method2()
            {
            }

            [WithMethodInvocation(LoggerLevel = LoggerLevel.Fatal)]
            public virtual void Method3()
            {
            }
        }

        public class Tester2 : ITester
        {
            public virtual void Method1()
            {
            }
        }

        private static void AssertNotAllSame(WithMethodInvocationAttribute[] values)
        {
            Assert.AreNotSame(values[0], values[1]);
            Assert.AreNotSame(values[0], values[2]);
            Assert.AreNotSame(values[1], values[2]);
        }

        private readonly ProxyFactory factory = new ProxyFactory();

        [Test]
        public void Each_marker_is_transient_per_method()
        {
            var interceptor = new MethodInvocationInterceptor();
            var tester = this.factory.CreateClassProxy<Tester>(interceptor);
            tester.Method1();
            tester.Method2();
            tester.Method3();

            var markers = interceptor.GetMarkers();
            Assert.That(markers.Count, Is.EqualTo(3));
            Assert.That(markers.Count(x => x.Value.LoggerLevel == LoggerLevel.Fatal), Is.EqualTo(1));

            AssertNotAllSame(markers.Select(x => x.Value).ToArray());
        }

        [Test]
        public void DefaultMarkerInstance_is_used_to_clone_new_instances()
        {
            var interceptor = new MethodInvocationInterceptor
            {
                DefaultMarkerInstance = new WithMethodInvocationAttribute
                {
                    LoggerLevel = LoggerLevel.Fatal
                }
            };
            var tester = this.factory.CreateClassProxy<Tester>(interceptor);
            tester.Method1();
            tester.Method2();
            tester.Method3();
            var markers = interceptor.GetMarkers();
            Assert.That(markers.Count, Is.EqualTo(3));
            Assert.That(markers.Count(x => x.Value.LoggerLevel == LoggerLevel.Fatal), Is.EqualTo(3));

            AssertNotAllSame(markers.Select(x => x.Value).ToArray());
        }

        [Test]
        public void Markers_are_transient_per_implementation_method()
        {
            var interceptor = new MethodInvocationInterceptor();
            var a = this.factory.CreateInterfaceProxyWithTarget<ITester>(new Tester(), interceptor);
            var b = this.factory.CreateInterfaceProxyWithTarget<ITester>(new Tester2(), interceptor);
            a.Method1();
            b.Method1();

            Assert.That(interceptor.GetMarkers().Count, Is.EqualTo(2));
        }
    }
}