using System;

namespace AspectCastle
{
    public class Metrics
    {
        private readonly WithMetricsAttribute _attribute;
        private DateTime _start;
        private DateTime _lastExpireTime = DateTime.MinValue;
        private double _totalTime;
        private double _totalSquaredTime;

        internal Metrics(WithMetricsAttribute attribute)
        {
            this._attribute = attribute;
            Reset();
        }

        private void Reset()
        {
            this._start = DateTime.UtcNow;
            this._totalTime = 0;
            this._totalSquaredTime = 0;
            this.MaxTime = TimeSpan.MinValue;
            this.MinTime = TimeSpan.MaxValue;
            this.Invocations = 0;
            this.Exceptions = 0;
        }

        /// <summary>Gets the number of times the method was invoked.</summary>
        public ulong Invocations { get; private set; }

        /// <summary>Gets the number of times the method threw an uncaught exception.</summary>
        public ulong Exceptions { get; private set; }

        /// <summary>Gets the maximum execution time.</summary>
        public TimeSpan MaxTime { get; private set; }

        /// <summary>Gets the minimum execution time.</summary>
        public TimeSpan MinTime { get; private set; }

        /// <summary>Gets the average execution time.</summary>
        public TimeSpan AvgTime { get { return this.Invocations > 0 ? TimeSpan.FromMilliseconds(this._totalTime / this.Invocations) : TimeSpan.Zero; } }

        /// <summary>Gets the average number of times the method is called per second.</summary>
        public double FrequencyPerSecond { get { return this.Invocations / (DateTime.UtcNow - this._start).TotalSeconds; } }

        /// <summary>Gets the average number of times the method is called per minute.</summary>
        public double FrequencyPerMinute { get { return this.Invocations / (DateTime.UtcNow - this._start).TotalMinutes; } }

        /// <summary>Gets the average number of times the method is called per hour.</summary>
        public double FrequencyPerHour { get { return this.Invocations / (DateTime.UtcNow - this._start).TotalHours; } }

        /// <summary>Gets the current population variance of all measured values.</summary>
        public TimeSpan Variance { get { return TimeSpan.FromMilliseconds(CalculateVariance()); } }

        /// <summary>Gets the current population standard deviation of all measured values.</summary>
        public TimeSpan StandardDeviation { get { return TimeSpan.FromMilliseconds(Math.Sqrt(CalculateVariance())); } }

        private double CalculateVariance()
        {
            if (this._totalSquaredTime > 0 && this.Invocations > 0)
            {
                double variance = (this._totalSquaredTime - (Math.Pow(this._totalTime, 2) / this.Invocations)) / this.Invocations;
                if (variance > 0)
                    return variance;
            }
            return 0;
        }

        /// <summary>Increments internal counters.</summary>
        /// <param name="span">The duration of the method execution.</param>
        internal MetricsUpdateEventReason Increment(TimeSpan span)
        {
            lock (this)
            {
                MetricsUpdateEventReason reason = MetricsUpdateEventReason.None;

                if (span > this.MaxTime)
                    this.MaxTime = span;

                if (span < this.MinTime)
                    this.MinTime = span;

                double milliseconds = span.TotalMilliseconds;
                unchecked
                {
                    if (this._attribute.IsVarianceEnabled)
                    {
                        double stddev = Math.Sqrt(CalculateVariance());
                        double average = this.AvgTime.TotalMilliseconds;
                        double positive = average + (stddev * this._attribute.StandardDeviationThreshold);
                        double negative = average - (stddev * this._attribute.StandardDeviationThreshold);
                        if (milliseconds > positive || milliseconds < negative)
                            reason = MetricsUpdateEventReason.StandardDeviationThreshold;

                        this._totalSquaredTime += Math.Pow(milliseconds, 2);
                    }

                    this._totalTime += milliseconds;
                    ++this.Invocations;
                }

                if ((DateTime.UtcNow - this._lastExpireTime).TotalMilliseconds > this._attribute.SampleInterval)
                {
                    this._lastExpireTime = DateTime.UtcNow;
                    reason = MetricsUpdateEventReason.Sample;
                }

                if ((DateTime.UtcNow - this._start).TotalMilliseconds > this._attribute.ResetInterval)
                {
                    Reset();
                    reason = MetricsUpdateEventReason.Reset;
                }

                if (milliseconds < this._attribute.MinimumThreshold)
                {
                    reason = MetricsUpdateEventReason.None;
                }
                return reason;
            }
        }

        /// <summary>Increments the # of exceptions thrown.</summary>
        internal void IncrementException()
        {
            unchecked
            {
                ++this.Exceptions;
            }
        }

        /// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</summary>
        public override string ToString()
        {
            return String.Format("{0}Invocations={1}{0}AvgTime={2}{0}MinTime={3}{0}MaxTime={4}{0}StdDev={5}{0}#/s={6}{0}#/m={7}{0}#/h={8}{0}Exceptions={9}",
                                 Environment.NewLine + "\t\t",
                                 this.Invocations,
                                 this.AvgTime,
                                 this.MinTime,
                                 this.MaxTime,
                                 this.StandardDeviation,
                                 this.FrequencyPerSecond.ToString("F4"),
                                 this.FrequencyPerMinute.ToString("F4"),
                                 this.FrequencyPerHour.ToString("F4"),
                                 this.Exceptions);
        }
    }
}