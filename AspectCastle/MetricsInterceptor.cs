using System;
using System.Reflection;
using AspectCastle.Core;
using Castle.Core.Logging;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>Intercepts methods and measures performance characteristics such as max/min execution speed, invocation count, etc.</summary>
    public class MetricsInterceptor : InterceptorBase<WithMetricsAttribute>
    {
        /// <summary>Event triggered when metrics are calculated.</summary>
        public event EventHandler<MetricsUpdatedEventArgs> MetricsUpdated;

        protected override bool Accept(IInvocation invocation)
        {
            if (invocation.Method.IsSpecialName) // property
            {
                if (invocation.Method.Name.StartsWith("get_") || invocation.Method.Name.StartsWith("set_"))
                    return false;
            }
            return true;
        }

        protected override void Intercept(IInvocation invocation, WithMetricsAttribute marker)
        {
            Metrics metrics = marker.Metrics;
            if (metrics == null)
            {
                metrics = new Metrics(marker);
                marker.Metrics = metrics;
            }
            DateTime start = DateTime.UtcNow;
            try
            {
                invocation.Proceed();
            }
            catch
            {
                metrics.IncrementException();
                throw;
            }
            finally
            {
                var elapsed = DateTime.UtcNow - start;
                var reason = metrics.Increment(elapsed);
                if (reason != MetricsUpdateEventReason.None)
                {
                    Log(marker.LoggerLevel,
                        () => string.Format("{0}[{1}] metrics: {2}", invocation.TargetType.FullName, invocation.Method, metrics));
                    if (this.MetricsUpdated != null)
                    {
                        try
                        {
                            this.MetricsUpdated(this, new MetricsUpdatedEventArgs(marker, invocation.Method, elapsed, reason));
                        }
                        catch (Exception e)
                        {
                            Log(LoggerLevel.Error, () => "An event handler for MetricsUpdate threw an exception: " + e);
                        }
                    }
                }
            }
        }
    }

    /// <summary>Provides additional information for when a <see cref="MetricsInterceptor.MetricsUpdated"/> event is triggered.</summary>
    public class MetricsUpdatedEventArgs : EventArgs
    {
        internal MetricsUpdatedEventArgs(WithMetricsAttribute marker, MethodInfo methodInfo, TimeSpan elapsed, MetricsUpdateEventReason reason)
        {
            this.Marker = marker;
            this.Method = methodInfo;
            this.Reason = reason;
            this.Elapsed = elapsed;
        }

        /// <summary>Gets the method that was intercepted.</summary>
        public MethodInfo Method { get; private set; }

        /// <summary>Gets the marker attribute associated with the method.</summary>
        public WithMetricsAttribute Marker { get; private set; }

        /// <summary>Gets the reason for triggering the event.</summary>
        public MetricsUpdateEventReason Reason { get; private set; }

        /// <summary>Gets the elapsed time of the invocation that triggered the event.</summary>
        public TimeSpan Elapsed { get; private set; }
    }

    /// <summary>Specifies the reason for why a <see cref="MetricsInterceptor.MetricsUpdated"/> event is fired.</summary>
    public enum MetricsUpdateEventReason
    {
        /// <summary>No particular reason.</summary>
        None,
        /// <summary>The event fired because it was reset based on the value of <see cref="WithMetricsAttribute.ResetInterval"/>.</summary>
        Reset,
        /// <summary>The event fired because the time elapsed since the last update is more than the value of <see cref="WithMetricsAttribute.SampleInterval"/>.</summary>
        Sample,
        /// <summary>The event fired because the time exceeds the specified <see cref="WithMetricsAttribute.StandardDeviationThreshold"/>.</summary>
        StandardDeviationThreshold,
    }

    public sealed class WithMetricsAttribute : MarkerBaseAttribute
    {
        /// <summary>Initializes a new instances of <see cref="WithMetricsAttribute"/>.</summary>
        public WithMetricsAttribute()
        {
            this.SampleInterval = (int)TimeSpan.FromDays(1).TotalMilliseconds;
            this.ResetInterval = (int)TimeSpan.FromDays(7).TotalMilliseconds;
            this.StandardDeviationThreshold = 3;
            this.MinimumThreshold = 1000;
        }

        /// <summary>Gets or sets the amount of time in milliseconds to elapse before logging metrics.  Default is 1 day.</summary>
        public double SampleInterval { get; set; }

        /// <summary>Gets or sets the amount of time in milliseconds to elapse before metrics are reset.  Default is 1 week.</summary>
        public double ResetInterval { get; set; }

        /// <summary>Gets or sets the minimum amount of time in milliseconds a method must elapse for it to be a part of other threshold calculations.  Default is 1000ms.</summary>
        public double MinimumThreshold { get; set; }

        /// <summary>Get or sets whether to measure variance.  There is a slight performance hit if this is enabled.  Default is false.</summary>
        public bool IsVarianceEnabled { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation threshold.  This value determines whether to write to the log
        /// the moment the interception occurs if the run time for the particular method is X standard deviations
        /// away from the mean.  The default is 3, which corresponds to a 99.7% confidence interval.
        /// </summary>
        public double StandardDeviationThreshold { get; set; }

        /// <summary>Gets the metrics for the intercepted method.</summary>
        public Metrics Metrics { get; internal set; }
    }
}