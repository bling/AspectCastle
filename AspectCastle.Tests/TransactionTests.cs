using System.Threading;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class TransactionTests
    {
        private static readonly ProxyFactory _factory = new ProxyFactory();

        [WithTransaction]
        public class TransactionTester
        {
            private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            public virtual void CauseError()
            {
                this._rwlock.EnterWriteLock();
                this._rwlock.EnterWriteLock();
            }

            public virtual void DoNothing()
            {
            }
        }

        private static T Generate<T>()
        {
            var proxy = _factory.CreateClassProxy(typeof(T), new TransactionInterceptor { Logger = TestUtil.CreateLogger() });
            return (T)proxy;
        }

        [Test]
        public void Transaction_interceptor_completes_successfully_when_doing_nothing()
        {
            var tester = Generate<TransactionTester>();
            Assert.DoesNotThrow(tester.DoNothing);
        }

        [Test]
        public void Transaction_interceptor_rethrows_exceptions()
        {
            var tester = Generate<TransactionTester>();
            Assert.Throws<LockRecursionException>(tester.CauseError);
        }
    }
}