using System;
using System.Threading;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>Wraps the invocation target with a lock if the class implements <see cref="ISyncRootInstanceProvider"/>.</summary>
    public sealed class SynchronizedInterceptor : InterceptorBase<WithLockAttribute>
    {
        protected override void Intercept(IInvocation invocation, WithLockAttribute marker)
        {
            var provider = invocation.InvocationTarget as ISyncRootInstanceProvider;
            object sync;
            if (provider != null && (sync = provider.SyncRoot) != null)
            {
                if (Monitor.TryEnter(sync, marker.Timeout))
                {
                    try
                    {
                        invocation.Proceed();
                    }
                    finally
                    {
                        Monitor.Exit(sync);
                    }
                }
                else
                {
                    Log(marker.LoggerLevel, () => string.Format("A lock cannot be acquired for {0}.", invocation.Method));
                }
            }
            else
            {
                Logger.Warn(string.Format("The instance of type {0} does not implement ISyncRootInstanceProvider.", invocation.TargetType));
                invocation.Proceed();
            }
        }
    }

    /// <summary>Marks a class/method to intercept with the <see cref="SynchronizedInterceptor"/>.</summary>
    public sealed class WithLockAttribute : MarkerBaseAttribute
    {
        private int timeout;

        /// <summary>Initializes a new instance of the <see cref="WithLockAttribute"/> class.</summary>
        public WithLockAttribute()
        {
            this.Timeout = 5000;
        }

        /// <summary>The amount of time in milliseconds to wait for lock acquisition.  If failed, it is reported to the log.</summary>
        public int Timeout { get { return this.timeout; } set { this.timeout = Math.Max(value, System.Threading.Timeout.Infinite); } }
    }

    /// <summary>An interface that provides an instance of an object to lock on.</summary>
    public interface ISyncRootInstanceProvider
    {
        /// <summary>An object to lock.</summary>
        object SyncRoot { get; }
    }
}