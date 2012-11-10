using System;
using System.Diagnostics;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class PerformanceCounterTests
    {
        private static readonly ProxyFactory _factory = new ProxyFactory();

        public class Tester
        {
            [WithPerfCounter(Name = "Test1", Increment = true, Decrement = true, CounterType = PerformanceCounterType.NumberOfItems32)]
            public virtual int Method1()
            {
                return 1;
            }

            [WithPerfCounter(Name = "Test2", Increment = true, Decrement = true, CounterType = PerformanceCounterType.AverageTimer32)]
            public virtual void Average()
            {
            }

            [WithPerfCounter(Name = "Test3", Increment = true, Decrement = true, CounterType = PerformanceCounterType.CounterMultiTimer)]
            public virtual void Counter()
            {
            }

            [WithPerfCounter(Name = "Test4", Increment = true, Decrement = true, CounterType = PerformanceCounterType.RawFraction)]
            public virtual void Raw()
            {
            }

            [WithPerfCounter(Name = "Test5", Increment = true, Decrement = true, CounterType = PerformanceCounterType.SampleFraction)]
            public virtual void Sample()
            {
            }
        }

        [TestFixtureSetUp, TestFixtureTearDown]
        public void Cleanup()
        {
            for (int i = 1; i <= 5; i++)
            {
                if (PerformanceCounterCategory.Exists("CAKE:Interception:Test" + i))
                    PerformanceCounterCategory.Delete("CAKE:Interception:Test" + i);
            }
        }

        [Test]
        public void Performance_counter_is_automatically_created()
        {
            var tester = (Tester)_factory.CreateClassProxy(typeof(Tester), new PerfCounterInterceptor());
            tester.Method1();
            Assert.That(PerformanceCounterCategory.CounterExists("Value", "CAKE:Interception:Test1"));
            PerformanceCounterCategory.Delete("CAKE:Interception:Test1");
        }

        [Test]
        public void Paired_performance_counters_also_create_the_base()
        {
            var tester1 = (Tester)_factory.CreateClassProxy(typeof(Tester), new PerfCounterInterceptor());
            tester1.Average();
            tester1.Counter();
            tester1.Raw();
            tester1.Sample();

            PerformanceCounterCategory.Delete("CAKE:Interception:Test2");
            PerformanceCounterCategory.Delete("CAKE:Interception:Test3");
            PerformanceCounterCategory.Delete("CAKE:Interception:Test4");
            PerformanceCounterCategory.Delete("CAKE:Interception:Test5");
        }

        private class TestInterceptor : PerfCounterInterceptor
        {
            protected override void Init(WithPerfCounterAttribute marker)
            {
                throw new InvalidOperationException();
            }
        }

        [Test]
        public void Errors_creating_performance_counters_get_ignored_and_get_disabled()
        {
            var tester1 = (Tester)_factory.CreateClassProxy(typeof(Tester), new TestInterceptor());
            Assert.That(tester1.Method1(), Is.EqualTo(1));
        }
    }
}