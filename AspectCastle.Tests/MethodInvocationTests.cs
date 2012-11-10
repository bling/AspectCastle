using System;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class MethodInvocationTests
    {
        private static readonly ProxyFactory _factory = new ProxyFactory();

        public class MethodLoggingTester
        {
            [WithMethodInvocation(Instrument = true)]
            public virtual void SomeMethod()
            {
                throw new Exception("Grrr!");
            }

            [WithMethodInvocation]
            public virtual void MethodWithArgs(object a, object b)
            {
            }

            [WithMethodInvocation(EvaluateArguments = true)]
            public virtual void MethodWithArgsAndEvaluate(object a, object b)
            {
            }
        }

        private static T Generate<T>(Action<string> messageInvoked)
        {
            return _factory.CreateClassProxy<T>(new MethodInvocationInterceptor { Logger = TestUtil.CreateLogger(messageInvoked) });
        }

        [Test]
        public void Method_invocation_can_evaluate_null_arguments()
        {
            string message = null;
            var tester = Generate<MethodLoggingTester>(x => message = x);
            Assert.DoesNotThrow(() => tester.MethodWithArgsAndEvaluate(null, "123"));
            StringAssert.Contains("null", message);
            StringAssert.Contains("123", message);
        }

        [Test]
        public void Method_invocation_can_handle_null_arguments()
        {
            string message = null;
            var tester = Generate<MethodLoggingTester>(x => message = x);
            Assert.DoesNotThrow(() => tester.MethodWithArgs(null, 123));
            StringAssert.Contains("null", message);
            StringAssert.Contains(typeof(int).Name, message);
        }

        [Test]
        public void Method_invocation_logger_can_instrument_methods()
        {
            string message = null;
            var tester = Generate<MethodLoggingTester>(x => message = x);
            Assert.Throws<Exception>(tester.SomeMethod);
            StringAssert.Contains("ms", message);
        }
    }
}