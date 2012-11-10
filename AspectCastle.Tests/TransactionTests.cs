using System.Threading;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class TransactionTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        [WithTransaction]
        public class TransactionTester
        {
            private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            public virtual void CauseError()
            {
                this.rwlock.EnterWriteLock();
                this.rwlock.EnterWriteLock();
            }

            public virtual void DoNothing()
            {
            }
        }

        private static T Generate<T>()
        {
            var proxy = Factory.CreateClassProxy(typeof(T), new TransactionInterceptor { Logger = TestUtil.CreateLogger() });
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