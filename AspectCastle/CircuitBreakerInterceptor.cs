using System;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    public class CircuitBreakerInterceptor : InterceptorBase<WithCircuitBreakerAttribute>
    {
        private DateTime _lastTrip = DateTime.MinValue;

        protected override void Intercept(IInvocation invocation, WithCircuitBreakerAttribute marker)
        {
            if (marker.State == CircuitState.Open && (DateTime.UtcNow - this._lastTrip) > TimeSpan.FromMilliseconds(marker.TimeoutMilliseconds))
                marker.State = CircuitState.HalfOpen;

            if (marker.State == CircuitState.Open)
                throw new CircuitIsOpenException();

            try
            {
                invocation.Proceed();
                marker.State = CircuitState.Closed;
            }
            catch
            {
                // if it fails in half open, reset the timeout instead of waiting for the threshold
                if (marker.Failures == 0 && marker.State == CircuitState.HalfOpen)
                {
                    OpenCircuit(marker);
                    throw new CircuitIsOpenException();
                }

                // meeting threshold should trip as well
                if (++marker.Failures >= marker.FailureThreshold)
                {
                    OpenCircuit(marker);
                }
                throw;
            }
        }

        private void OpenCircuit(WithCircuitBreakerAttribute marker)
        {
            this._lastTrip = DateTime.UtcNow;
            marker.State = CircuitState.Open;
            marker.Failures = 0;
        }
    }

    /// <summary>An attempt to execute a method in an open circuit state was attempted.</summary>
    public class CircuitIsOpenException : Exception
    {
    }

    internal enum CircuitState
    {
        Closed,
        HalfOpen,
        Open,
    }

    /// <summary>Marks a class/method to be wrapped with a circuit breaker.</summary>
    public class WithCircuitBreakerAttribute : MarkerBaseAttribute
    {
        /// <summary>The number of times the invocation can fail before the circuit is tripped open.</summary>
        public int FailureThreshold { get; set; }

        /// <summary>The period in seconds that the circuit will remain open after the failure threshold has been met.</summary>
        public int TimeoutMilliseconds { get; set; }

        internal int Failures { get; set; }
        internal CircuitState State { get; set; }
    }
}