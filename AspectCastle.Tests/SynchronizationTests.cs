using System.Threading;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class SynchronizationTests
    {
        private static readonly ProxyFactory _factory = new ProxyFactory();

        public class LockedTester : ISyncRootInstanceProvider
        {
            public bool WasCalled { get; private set; }

            #region ISyncRootInstanceProvider Members

            public object SyncRoot { get; set; }

            #endregion

            [WithLock(Timeout = 1)]
            public virtual void Method()
            {
                this.WasCalled = true;
            }
        }

        private static T Generate<T>()
        {
            var proxy = _factory.CreateClassProxy(typeof(T), new SynchronizedInterceptor { Logger = TestUtil.CreateLogger() });
            return (T)proxy;
        }

        [Test]
        public void Synchronized_interceptor_blocks_concurrent_access()
        {
            var tester = Generate<LockedTester>();
            tester.SyncRoot = new object();
            var t = new Thread(() => Monitor.Enter(tester.SyncRoot));
            t.Start();
            t.Join();
            tester.Method();
            Assert.IsFalse(tester.WasCalled);
        }

        [Test]
        public void Synchronized_interceptor_can_acquire_lock_normally()
        {
            var tester = Generate<LockedTester>();
            tester.SyncRoot = new object();
            tester.Method();
        }

        [Test]
        public void Synchronized_interceptor_ignores_and_logs_when_instances_do_not_provide_sync_objects()
        {
            var tester = Generate<LockedTester>();
            tester.Method();
            Assert.IsTrue(tester.WasCalled);
        }
    }
}