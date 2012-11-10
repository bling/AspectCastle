using System;
using System.Diagnostics;
using System.Threading;
using Castle.Core.Logging;
using NUnit.Framework;

namespace AspectCastle.Tests
{
    [TestFixture]
    public class MetricsInterceptorTests
    {
        private static readonly ProxyFactory Factory = new ProxyFactory();

        public class Tester
        {
            public virtual int Number { get; set; }

            public virtual void Hello()
            {
            }

            public virtual void Sleep250Milliseconds()
            {
                Thread.Sleep(250);
            }

            public virtual void ThrowException()
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public class TesterLogger : LevelFilteredLogger
        {
            public TesterLogger()
                : base("testlog", LoggerLevel.Debug)
            {
            }

            public string MostRecentMessage { get; private set; }

            public override ILogger CreateChildLogger(string loggerName)
            {
                throw new NotImplementedException();
            }

            protected override void Log(LoggerLevel loggerLevel, string loggerName, string message, Exception exception)
            {
                this.MostRecentMessage = message;
            }
        }

        private struct Holder
        {
            public Tester Tester;
            public MetricsInterceptor Interceptor;
            public TesterLogger Logger;
        }

        private static void Generate(out Holder holder, WithMetricsAttribute ma)
        {
            holder = new Holder { Logger = new TesterLogger() };
            holder.Interceptor = new MetricsInterceptor { DefaultMarkerInstance = ma, Logger = holder.Logger };
            holder.Tester = (Tester)Factory.CreateClassProxy(typeof(Tester), holder.Interceptor);
        }

        [Test]
        public void Invocation_count_is_correctly_calculated()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute { SampleInterval = -1, MinimumThreshold = 0 });
            holder.Tester.Hello();
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("1"));
            holder.Tester.Hello();
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("2"));
            holder.Tester.Hello();
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("3"));
        }

        [Test]
        public void Execution_average_time_calculation_adheres_attribute_value()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute { SampleInterval = 0, MinimumThreshold = 0 });

            Metrics metrics = null;
            holder.Interceptor.MetricsUpdated += (method, m) => metrics = m.Marker.Metrics;

            holder.Tester.Sleep250Milliseconds();
            Assert.That(metrics.AvgTime, Is.InRange(TimeSpan.FromMilliseconds(230), TimeSpan.FromMilliseconds(270)));
        }

        [Test]
        public void Exception_counter()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute { SampleInterval = 0, MinimumThreshold = 0 });

            Metrics metrics = null;
            holder.Interceptor.MetricsUpdated += (method, m) => metrics = m.Marker.Metrics;

            Assert.Throws<ArgumentOutOfRangeException>(() => holder.Tester.ThrowException());
            Assert.That(metrics.Exceptions, Is.EqualTo(1));
        }

        [Test]
        public void Metrics_tracks_all_properties_and_methods_without_marker_attributes()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute { MinimumThreshold = 0 });
            holder.Tester.Hello();
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("1"));
            holder.Tester.Number = 1;
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("1"));
            holder.Tester.Number.ToString();
            Assert.That(holder.Logger.MostRecentMessage, Is.StringContaining("1"));
        }

        [Test]
        public void Bad_event_listeners_are_handled()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute());
            holder.Interceptor.MetricsUpdated += (method, m) => { throw new Exception(); };
            Assert.DoesNotThrow(() => holder.Tester.Hello());
        }

        [Test]
        public void Metrics_are_reset_when_reset_interval_elapses()
        {
            Holder holder;
            Generate(out holder, new WithMetricsAttribute { SampleInterval = -1, ResetInterval = -1, MinimumThreshold = 0 });

            Metrics metrics = null;
            holder.Interceptor.MetricsUpdated += (method, m) => metrics = m.Marker.Metrics;

            // reset after every call
            holder.Tester.Hello();
            Assert.That(metrics.Invocations, Is.EqualTo(0));
            holder.Tester.Hello();
            Assert.That(metrics.Invocations, Is.EqualTo(0));
        }

        [Test]
        public void No_exceptions_are_thrown_when_incrementing_time_for_a_year()
        {
            Metrics metrics = new Metrics(new WithMetricsAttribute());
            for (int i = 0; i < 365; i++)
            {
                Assert.DoesNotThrow(() => metrics.Increment(TimeSpan.FromDays(1)));
            }

            Assert.That(metrics.AvgTime, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(metrics.MinTime, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(metrics.MaxTime, Is.EqualTo(TimeSpan.FromDays(1)));

            // again with random values so standard deviation has to be calculated
            Random r = new Random();
            for (int i = 0; i < 365; i++)
            {
                Assert.DoesNotThrow(() => metrics.Increment(TimeSpan.FromSeconds(r.Next(1, 100000))));
            }
            metrics.Variance.ToString();
            metrics.StandardDeviation.ToString();
        }

        [Test]
        public void Standard_deviation_is_correctly_estimated()
        {
            Metrics metrics = new Metrics(new WithMetricsAttribute { IsVarianceEnabled = true });
            metrics.Increment(TimeSpan.FromSeconds(2));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(5));
            metrics.Increment(TimeSpan.FromSeconds(5));
            metrics.Increment(TimeSpan.FromSeconds(7));
            metrics.Increment(TimeSpan.FromSeconds(9));
            Assert.That(metrics.AvgTime, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(metrics.StandardDeviation, Is.EqualTo(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void Standard_deviation_threshold_is_hit_for_positive_and_negative_from_the_mean()
        {
            Metrics metrics = new Metrics(new WithMetricsAttribute
            {
                IsVarianceEnabled = true,
                StandardDeviationThreshold = 1 // override default
            });
            metrics.Increment(TimeSpan.FromSeconds(2));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(4));
            metrics.Increment(TimeSpan.FromSeconds(5));
            metrics.Increment(TimeSpan.FromSeconds(5));
            metrics.Increment(TimeSpan.FromSeconds(7));
            metrics.Increment(TimeSpan.FromSeconds(9));

            // more than 2 seconds away from average 5 should trigger
            Assert.That(metrics.Increment(TimeSpan.FromSeconds(7.1)), Is.EqualTo(MetricsUpdateEventReason.StandardDeviationThreshold));
            Assert.That(metrics.Increment(TimeSpan.FromSeconds(2.9)), Is.EqualTo(MetricsUpdateEventReason.StandardDeviationThreshold));
            Assert.That(metrics.Increment(TimeSpan.FromSeconds(5)), Is.EqualTo(MetricsUpdateEventReason.None));
        }

        [Test]
        public void Minimum_threshold_can_filter_out_noise()
        {
            var attribute = new WithMetricsAttribute
            {
                IsVarianceEnabled = true,
                StandardDeviationThreshold = 1, // override default
                MinimumThreshold = 1000
            };
            Metrics metrics = new Metrics(attribute);
            metrics.Increment(TimeSpan.FromSeconds(10));
            metrics.Increment(TimeSpan.FromSeconds(10));
            Assert.That(metrics.Increment(TimeSpan.FromSeconds(0.1)), Is.EqualTo(MetricsUpdateEventReason.None));

            attribute.MinimumThreshold = 0;
            Assert.That(metrics.Increment(TimeSpan.FromSeconds(0.1)), Is.EqualTo(MetricsUpdateEventReason.StandardDeviationThreshold));
        }

        private static void DoNothing(TimeSpan temp)
        {
        }

        [TestCase(10000000)]
        [Explicit]
        public void Performance_test(int iterations)
        {
            var sw = Stopwatch.StartNew();
            Metrics m = new Metrics(new WithMetricsAttribute());
            for (int i = 1; i <= iterations; i++)
            {
                DoNothing(TimeSpan.FromSeconds(i));
            }
            Console.WriteLine(sw.Elapsed);

            sw = Stopwatch.StartNew();
            m = new Metrics(new WithMetricsAttribute());
            for (int i = 1; i <= iterations; i++)
            {
                m.Increment(TimeSpan.FromSeconds(i));
            }
            Console.WriteLine(sw.Elapsed);

            sw = Stopwatch.StartNew();
            m = new Metrics(new WithMetricsAttribute { IsVarianceEnabled = true });
            for (int i = 1; i <= iterations; i++)
            {
                m.Increment(TimeSpan.FromSeconds(i));
            }
            Console.WriteLine(sw.Elapsed);
        }
    }
}