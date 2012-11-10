using System;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>Intercepts methods and caches return values for a certain period of time.</summary>
    public class CacheInterceptor : InterceptorBase<WithCacheAttribute>
    {
        /// <summary>Gets or sets the marker instance to use if none is found on the class or the method.</summary>
        public override WithCacheAttribute DefaultMarkerInstance
        {
            get { return base.DefaultMarkerInstance; }
            set
            {
                if (value != null)
                    Logger.WarnFormat("The DefaultMarkerInstance is being set on a {0}.", GetType().Name);

                base.DefaultMarkerInstance = value;
            }
        }

        protected override void Intercept(IInvocation invocation, WithCacheAttribute marker)
        {
            CacheEntry entry = marker.CacheEntry;
            if (entry != null)
            {
                if ((DateTime.UtcNow - entry.Timestamp).TotalMilliseconds > marker.CacheMilliseconds)
                {
                    invocation.Proceed();
                    entry.Instance = invocation.ReturnValue;
                    entry.Timestamp = DateTime.UtcNow;
                }
                else
                {
                    invocation.ReturnValue = entry.Instance;
                }
            }
            else
            {
                invocation.Proceed();
                marker.CacheEntry = new CacheEntry { Instance = invocation.ReturnValue, Timestamp = DateTime.UtcNow };
            }
        }
    }

    /// <summary>Marks a method to have its return value cached for a certain period of time.</summary>
    public sealed class WithCacheAttribute : MarkerBaseAttribute
    {
        /// <summary>Initializes a new instance of <see cref="WithCacheAttribute"/>.</summary>
        public WithCacheAttribute()
        {
            this.CacheMilliseconds = 1000 * 60 * 60; // 1 hour
        }

        /// <summary>Gets or sets the number of milliseconds for how long a return value should be cached before it needs to hit the concrete method.</summary>
        public int CacheMilliseconds { get; set; }

        internal CacheEntry CacheEntry { get; set; }
    }

    internal class CacheEntry
    {
        public object Instance;
        public DateTime Timestamp;
    }
}