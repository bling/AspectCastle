using System;
using System.Collections.Generic;
using System.Diagnostics;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    public class PerfCounterInterceptor : InterceptorBase<WithPerfCounterAttribute>
    {
        public const string PerformanceCounterCategoryPrefixName = "CAKE:Interception:";

        private class Pair
        {
            public PerformanceCounter Perf, PerfBase;
        }

        private static readonly Dictionary<WithPerfCounterAttribute, Pair> _cache = new Dictionary<WithPerfCounterAttribute, Pair>();

        protected override void Intercept(IInvocation invocation, WithPerfCounterAttribute marker)
        {
            if (marker.State == WithPerfCounterAttribute.MarkerState.None)
            {
                lock (this)
                {
                    if (marker.State == WithPerfCounterAttribute.MarkerState.None)
                    {
                        try
                        {
                            Init(marker);
                            marker.State = WithPerfCounterAttribute.MarkerState.Initialized;
                        }
                        catch (Exception e)
                        {
                            marker.State = WithPerfCounterAttribute.MarkerState.Error;
                            Logger.Error("Cannot initialize performance counters.", e);
                        }
                    }
                }
            }

            if (marker.State == WithPerfCounterAttribute.MarkerState.Initialized)
            {
                PreProceed(marker);
                try
                {
                    invocation.Proceed();
                }
                finally
                {
                    PostProceed(marker);
                }
            }
            else
            {
                invocation.Proceed();
            }
        }

        protected virtual void PreProceed(WithPerfCounterAttribute marker)
        {
            if (marker.Increment)
                marker.PerfCounter.Increment();
        }

        protected virtual void PostProceed(WithPerfCounterAttribute marker)
        {
            if (marker.Decrement)
                marker.PerfCounter.Decrement();
        }

        protected virtual void Init(WithPerfCounterAttribute marker)
        {
            Pair pair;
            if (_cache.TryGetValue(marker, out pair))
            {
                marker.PerfCounter = pair.Perf;
                marker.PerfCounterBase = pair.PerfBase;
                return;
            }

            string categoryName = PerformanceCounterCategoryPrefixName + marker.Name;

            if (marker.AlwaysRecreate)
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                    PerformanceCounterCategory.Delete(categoryName);
            }

            if (!PerformanceCounterCategory.Exists(categoryName))
                Create(categoryName, marker.CounterHelp, marker.CounterType);

            Pair p = new Pair();
            p.Perf = new PerformanceCounter(categoryName, "Value", false);
            switch (marker.CounterType)
            {
                case PerformanceCounterType.AverageCount64:
                case PerformanceCounterType.AverageTimer32:
                case PerformanceCounterType.CounterMultiTimer:
                case PerformanceCounterType.CounterMultiTimerInverse:
                case PerformanceCounterType.CounterMultiTimer100Ns:
                case PerformanceCounterType.CounterMultiTimer100NsInverse:
                case PerformanceCounterType.RawFraction:
                case PerformanceCounterType.SampleFraction:
                    p.PerfBase = new PerformanceCounter(categoryName, "ValueBase", false);
                    break;
            }

            _cache[marker] = p;
            marker.PerfCounter = p.Perf;
            marker.PerfCounterBase = p.PerfBase;
        }

        private static void Create(string categoryName, string counterHelp, PerformanceCounterType counterType)
        {
            CounterCreationDataCollection cd = new CounterCreationDataCollection { new CounterCreationData("Value", counterHelp, counterType) };
            switch (counterType)
            {
                case PerformanceCounterType.AverageCount64:
                case PerformanceCounterType.AverageTimer32:
                    cd.Add(new CounterCreationData("ValueBase", counterHelp, PerformanceCounterType.AverageBase));
                    break;
                case PerformanceCounterType.CounterMultiTimer:
                case PerformanceCounterType.CounterMultiTimerInverse:
                case PerformanceCounterType.CounterMultiTimer100Ns:
                case PerformanceCounterType.CounterMultiTimer100NsInverse:
                    cd.Add(new CounterCreationData("ValueBase", counterHelp, PerformanceCounterType.CounterMultiBase));
                    break;
                case PerformanceCounterType.RawFraction:
                    cd.Add(new CounterCreationData("ValueBase", counterHelp, PerformanceCounterType.RawBase));
                    break;
                case PerformanceCounterType.SampleFraction:
                    cd.Add(new CounterCreationData("ValueBase", counterHelp, PerformanceCounterType.SampleBase));
                    break;
            }
            PerformanceCounterCategory.Create(categoryName, counterHelp, PerformanceCounterCategoryType.SingleInstance, cd);
        }
    }

    /// <summary>
    /// Marks the method intercepted to automatically increment/decrement a performance counter.
    /// </summary>
    public sealed class WithPerfCounterAttribute : MarkerBaseAttribute
    {
        public WithPerfCounterAttribute()
        {
            this.Increment = true;
            this.CounterHelp = string.Empty;
        }

        /// <summary>Gets or sets the name that is appended to the performance counter category.</summary>
        /// <remarks>The actual counter will be the category CAKE:Interceptor:{Name}, with the counter name Value.</remarks>
        public string Name { get; set; }

        /// <summary>Gets or sets a help description for the counter.</summary>
        public string CounterHelp { get; set; }

        /// <summary>Gets or sets the type of performance counter.</summary>
        public PerformanceCounterType CounterType { get; set; }

        /// <summary>Gets or sets whether to increment the performance counter prior to invocation.</summary>
        public bool Increment { get; set; }

        /// <summary>Gets or sets whether to decrement the performance counter after invocation.</summary>
        public bool Decrement { get; set; }

        /// <summary>Gets or sets whether the performance counter category should always be deleted/recreated during initialization.</summary>
        public bool AlwaysRecreate { get; set; }

        /// <summary>Gets the instance of the <see cref="PerformanceCounter"/> associated with this marker's properties.</summary>
        public PerformanceCounter PerfCounter { get; internal set; }

        /// <summary>Gets the instance of the base (if applicable) <see cref="PerformanceCounter"/> associated with this marker's properties.</summary>
        public PerformanceCounter PerfCounterBase { get; internal set; }

        internal MarkerState State;

        internal enum MarkerState
        {
            None,
            Initialized,
            Error,
        }
    }
}