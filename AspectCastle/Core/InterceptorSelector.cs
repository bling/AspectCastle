using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;
using Castle.DynamicProxy;

namespace AspectCastle.Core
{
    /// <summary>
    /// Used for the proxy generation to filter which methods needs to be intercepted.
    /// </summary>
    public sealed class InterceptorSelector : IInterceptorSelector
    {
        private ILogger logger = NullLogger.Instance;

        /// <summary>Gets or sets the logger for this instance.</summary>
        public ILogger Logger { get { return this.logger; } set { this.logger = value; } }

        IInterceptor[] IInterceptorSelector.SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            if (interceptors.Length == 0)
                return interceptors;

            var markers = new List<MarkerBaseAttribute>();
            if (type != null)
                markers.AddRange(type.GetCustomAttributes(typeof(MarkerBaseAttribute), true).Cast<MarkerBaseAttribute>());

            if (method != null)
                markers.AddRange(method.GetCustomAttributes(typeof(MarkerBaseAttribute), true).Cast<MarkerBaseAttribute>());

            if (markers.Count == 0) // no marker attributes found, no ordering required
                return interceptors;

            markers.Sort((a, b) => a.Order.CompareTo(b.Order));

            var sorted = new List<IInterceptor>();
            for (int i = 0; i < markers.Count; ++i)
            {
                var providers = interceptors.OfType<IInterceptorMarkerProvider>();
                var markerType = markers[i].GetType();
                var matchingInterceptor = providers.FirstOrDefault(x => x.MarkerType == markerType) as IInterceptor;
                if (matchingInterceptor != null)
                    sorted.Add(matchingInterceptor);
            }
            return sorted.ToArray();
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetType() == typeof(InterceptorSelector);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
    }
}