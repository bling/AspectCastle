using System;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class ExceptionPolicyTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        private static T Generate<T>()
        {
            var proxy = Factory.CreateClassProxy(typeof(T), new ExceptionPolicyInterceptor { Logger = TestUtil.CreateLogger() });
            return (T)proxy;
        }

        public class ExceptionPolicyTester : ExceptionPolicyInterceptor.IHandler
        {
            #region IHandler Members

            void ExceptionPolicyInterceptor.IHandler.HandleException(string tag, Exception exception)
            {
                if (tag == "throw")
                    throw new InvalidCastException();
            }

            #endregion

            [WithExceptionPolicy(Swallow = true)]
            public virtual void Throws_and_swallows()
            {
                throw new AccessViolationException();
            }

            [WithExceptionPolicy(Tag = "throw")]
            public virtual void Throws_invalid_operation_exception_with_tag()
            {
                throw new InvalidOperationException();
            }

            [WithExceptionPolicy]
            public virtual void Throws_invalid_operation_exception_without_tag()
            {
                throw new InvalidOperationException();
            }

            [WithExceptionPolicy(typeof(ArgumentException), Swallow = true)]
            public virtual void Throw_argument_exception_filter_contains()
            {
                throw new ArgumentException();
            }

            [WithExceptionPolicy(typeof(InvalidOperationException), Swallow = true)]
            public virtual void Throw_argument_exception_filter_does_not_contain()
            {
                throw new ArgumentException();
            }
        }

        public class ExceptionPolicyTesterWithNoHandler
        {
            [WithExceptionPolicy(Swallow = true)]
            public virtual void Hello()
            {
                throw new Exception();
            }
        }

        [Test]
        public void Exception_policy_filters_exception_types()
        {
            var tester = Generate<ExceptionPolicyTester>();
            Assert.DoesNotThrow(tester.Throw_argument_exception_filter_contains);
            Assert.Throws<ArgumentException>(tester.Throw_argument_exception_filter_does_not_contain);
        }

        [Test]
        public void Exception_policy_interceptor_can_swallow_exceptions()
        {
            var tester = Generate<ExceptionPolicyTester>();
            Assert.DoesNotThrow(tester.Throws_and_swallows);
        }

        [Test]
        public void Exception_policy_interceptor_can_throw_new_exception()
        {
            var tester = Generate<ExceptionPolicyTester>();
            Assert.Throws<InvalidCastException>(tester.Throws_invalid_operation_exception_with_tag);
        }

        [Test]
        public void Exception_policy_interceptor_rethrows_if_no_handler_is_set()
        {
            var tester = Generate<ExceptionPolicyTester>();
            Assert.Throws<InvalidOperationException>(tester.Throws_invalid_operation_exception_without_tag);
        }

        [Test]
        public void Exception_policy_interceptor_does_not_require_ihandler_implementation()
        {
            var tester = Generate<ExceptionPolicyTesterWithNoHandler>();
            Assert.DoesNotThrow(tester.Hello);
        }
    }
}