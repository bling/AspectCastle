using AspectCastle.Core;
using Castle.Core.Logging;
using Castle.DynamicProxy;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class ProxyGeneratorTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        public interface ITester
        {
            int ReturnOne();
        }

        public class Tester : ITester
        {
            public readonly int Value;

            public Tester()
            {
            }

            public Tester(int value)
            {
                this.Value = value;
            }

            #region ITester Members

            [WithLock]
            public virtual int ReturnOne()
            {
                return 1;
            }

            #endregion
        }

        public class NullLoggerTester : InterceptorBase<WithLockAttribute>
        {
            protected override void Intercept(IInvocation invocation, WithLockAttribute marker)
            {
            }
        }

        [Test]
        public void By_default_all_loggers_are_null_loggers()
        {
            var tester = new NullLoggerTester();
            Assert.AreSame(NullLogger.Instance, tester.Logger);
        }

        [Test]
        public void Can_generate_interface_proxies_with_target()
        {
            object proxy = Factory.CreateInterfaceProxyWithTarget(new Tester(), typeof(ITester), new SynchronizedInterceptor());
            Assert.IsNotNull(proxy);

            ITester t = Factory.CreateInterfaceProxyWithTarget<ITester>(new Tester(), new SynchronizedInterceptor());
            Assert.AreEqual(1, t.ReturnOne());
        }

        [Test]
        public void Can_generate_interface_proxies_without_target()
        {
            object proxy = Factory.CreateInterfaceProxyWithoutTarget(typeof(ITester), new SynchronizedInterceptor());
            Assert.IsNotNull(proxy);

            ITester t = Factory.CreateInterfaceProxyWithoutTarget<ITester>(new SynchronizedInterceptor());
            Assert.IsNotNull(t);
        }

        [Test]
        public void Can_generate_proxy_classes()
        {
            object proxy = Factory.CreateClassProxy(typeof(Tester), new SynchronizedInterceptor());
            Assert.IsNotNull(proxy);

            Tester t = Factory.CreateClassProxy<Tester>(new SynchronizedInterceptor());
            Assert.AreEqual(1, t.ReturnOne());
        }

        [Test]
        public void Can_generate_proxy_classes_with_constructor_arguments()
        {
            object proxy = Factory.CreateClassProxy(new object[] { 1 }, typeof(Tester), new SynchronizedInterceptor());
            Assert.IsNotNull(proxy);

            Tester t = Factory.CreateClassProxy<Tester>(new object[] { 2 }, new SynchronizedInterceptor());
            Assert.AreEqual(2, t.Value);
        }

        [Test]
        public void Non_matching_marker_attributes_wont_puke()
        {
            // tester only has synchronized interceptor markers
            var tester = Factory.CreateClassProxy<Tester>(new MethodInvocationInterceptor());
            Assert.DoesNotThrow(() => tester.ReturnOne());
        }
    }
}