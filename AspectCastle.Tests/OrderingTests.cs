using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class OrderingTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        public class NonVirtualTester
        {
            [WithLock(Timeout = 1)]
            public void NonVirtual()
            {
            }
        }

        [WithLock(Timeout = 1)]
        public class InterceptorCountTester
        {
            [WithExceptionPolicy]
            [WithMethodInvocation]
            [WithTransaction]
            public virtual void Should_have_4_interceptors()
            {
                StackTrace st = new StackTrace();
                var frames = st.GetFrames();
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(TransactionInterceptor)));
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(MethodInvocationInterceptor)));
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(TransactionInterceptor)));
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(SynchronizedInterceptor)));
            }

            [WithMethodInvocation]
            public virtual void Should_have_2_interceptors()
            {
                StackTrace st = new StackTrace();
                var frames = st.GetFrames();
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(SynchronizedInterceptor)));
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(MethodInvocationInterceptor)));
            }

            public virtual void Should_have_1_interceptor()
            {
                StackTrace st = new StackTrace();
                var frames = st.GetFrames();
                Assert.AreEqual(1, frames.Count(frame => frame.GetMethod().DeclaringType == typeof(SynchronizedInterceptor)));
            }
        }

        [WithLock(Order = int.MinValue)]
        public class InterceptorOrderTester
        {
            [WithExceptionPolicy(Order = 0)]
            public virtual void Lock_then_exception()
            {
                StackTrace st = new StackTrace();
                var frames = st.GetFrames();
                int sync = Array.IndexOf(frames, frames.First(f => f.GetMethod().DeclaringType == typeof(SynchronizedInterceptor)));
                int exception = Array.IndexOf(frames, frames.First(f => f.GetMethod().DeclaringType == typeof(ExceptionPolicyInterceptor)));
                Assert.Greater(sync, exception);
            }

            [WithExceptionPolicy(Order = 2)]
            [WithMethodInvocation(Order = 0)]
            public virtual void Lock_then_method_then_exception()
            {
                StackTrace st = new StackTrace();
                var frames = st.GetFrames();
                int sync = Array.IndexOf(frames, frames.First(f => f.GetMethod().DeclaringType == typeof(SynchronizedInterceptor)));
                int method = Array.IndexOf(frames, frames.First(f => f.GetMethod().DeclaringType == typeof(MethodInvocationInterceptor)));
                int exception = Array.IndexOf(frames, frames.First(f => f.GetMethod().DeclaringType == typeof(ExceptionPolicyInterceptor)));
                Assert.Greater(sync, method);
                Assert.Greater(method, exception);
            }
        }

        [WithExceptionPolicy(Swallow = true)]
        public class DoNotInterceptTester
        {
            private bool _propertyEnabled;
            public virtual bool PropertyEnabled
            {
                get { return this._propertyEnabled; }
                set
                {
                    this._propertyEnabled = value;
                    throw new Exception();
                }
            }

            private bool _propertyDisabled;
            public virtual bool PropertyDisabled
            {
                get { return this._propertyDisabled; }
                [WithExceptionPolicy(Intercept = false)]
                set
                {
                    this._propertyDisabled = value;
                    throw new Exception();
                }
            }

            [WithExceptionPolicy(Intercept = false)]
            public virtual void ThrowBlocked()
            {
                throw new Exception();
            }

            public virtual void ThrowRegular()
            {
                throw new Exception();
            }
        }

        private static T Generate<T>()
        {
            var proxy = Factory.CreateClassProxy(typeof(T),
                                                  new MethodInvocationInterceptor(),
                                                  new TransactionInterceptor(),
                                                  new ExceptionPolicyInterceptor(),
                                                  new SynchronizedInterceptor());
            return (T)proxy;
        }

        [Test]
        public void Interception_module_intercepts_with_the_correct_number_of_interceptors()
        {
            var tester = Generate<InterceptorCountTester>();
            tester.Should_have_1_interceptor();
            tester.Should_have_2_interceptors();
            tester.Should_have_4_interceptors();
        }

        [Test]
        public void Interception_module_sorts_by_the_order_property()
        {
            var tester = Generate<InterceptorOrderTester>();
            tester.Lock_then_exception();
            tester.Lock_then_method_then_exception();
        }

        //[Test]
        //public void Ordering_works_with_Windsor_integration()
        //{
        //    IWindsorContainer c = new WindsorContainer();
        //    c.Register(Component.For<MethodInvocationInterceptor>());
        //    c.Register(Component.For<TransactionInterceptor>());
        //    c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(new ExceptionPolicyInterceptor()));
        //    c.Register(Component.For<SynchronizedInterceptor>().Instance(new SynchronizedInterceptor()));
        //    c.Register(Component.For<InterceptorOrderTester>()
        //                   .Interceptors(new InterceptorReference(typeof(MethodInvocationInterceptor)),
        //                                 new InterceptorReference(typeof(TransactionInterceptor)),
        //                                 new InterceptorReference(typeof(ExceptionPolicyInterceptor)),
        //                                 new InterceptorReference(typeof(SynchronizedInterceptor))).Anywhere);

        //    var tester = c.Resolve<InterceptorOrderTester>();
        //    tester.Lock_then_exception();
        //    tester.Lock_then_method_then_exception();
        //}

        [Test]
        public void Do_not_intercept_attribute_turns_off_all_interception()
        {
            var tester = Generate<DoNotInterceptTester>();
            Assert.Throws<Exception>(tester.ThrowBlocked);
            Assert.DoesNotThrow(tester.ThrowRegular);
        }

        [Test]
        public void Do_not_intercept_attribute_is_applicable_to_properties()
        {
            var tester = Generate<DoNotInterceptTester>();
            Assert.DoesNotThrow(() => tester.PropertyEnabled.ToString());
            Assert.Throws<Exception>(() => tester.PropertyDisabled = true);
        }
    }
}