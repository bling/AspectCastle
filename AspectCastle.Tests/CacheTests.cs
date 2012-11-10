using System.Threading;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class CacheTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        public class CacheTester
        {
            private int count;

            [WithCache(CacheMilliseconds = 100)]
            public virtual int ReturnIncrement()
            {
                return ++this.count;
            }
        }

        private static CacheTester GenerateTester()
        {
            var cacheInterceptor = new CacheInterceptor();
            cacheInterceptor.DefaultMarkerInstance = cacheInterceptor.DefaultMarkerInstance; // shut up code coverage
            var proxy = Factory.CreateClassProxy(typeof(CacheTester), cacheInterceptor);
            return (CacheTester)proxy;
        }

        [Test]
        public void Return_value_is_cached_only_for_a_certain_period_of_time()
        {
            var tester = GenerateTester();
            Assert.AreEqual(1, tester.ReturnIncrement());
            Assert.AreEqual(1, tester.ReturnIncrement());
            Thread.Sleep(150);
            Assert.AreEqual(2, tester.ReturnIncrement());
            Assert.AreEqual(2, tester.ReturnIncrement());
        }
    }
}