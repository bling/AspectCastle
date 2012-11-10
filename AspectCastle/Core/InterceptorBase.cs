using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;
using Castle.DynamicProxy;

namespace AspectCastle.Core
{
    /// <summary>
    /// Base class for all interceptors.
    /// </summary>
    /// <typeparam name="TMarker">A marker attribute to determine whether a method should be intercepted or not.</typeparam>
    public abstract class InterceptorBase<TMarker> : IInterceptor, IInterceptorMarkerProvider
        where TMarker : MarkerBaseAttribute, ICloneable, new()
    {
        public const string DefaultMarkerInstanceKey = "Mmx.Infrastructure.interception.defaultmarkerinstance";

        private readonly Hashtable cache = new Hashtable(); // hashtable used instead of dictionary for lock-free performance
        private ILogger log = NullLogger.Instance;

        protected InterceptorBase()
        {
            this.MarkerType = typeof(TMarker);
        }

        /// <summary>Gets a copy of all markers in the internal collection.</summary>
        public IDictionary<MethodInfo, TMarker> GetMarkers()
        {
            lock (this.cache.SyncRoot)
            {
                var result = new Dictionary<MethodInfo, TMarker>();
                foreach (DictionaryEntry entry in this.cache)
                    result[(MethodInfo)entry.Key] = (TMarker)entry.Value;

                return result;
            }
        }

        /// <summary>Gets or sets the marker instance to use if none is found on the class or the method.</summary>
        public virtual TMarker DefaultMarkerInstance { get; set; }

        [DebuggerStepThrough]
        void IInterceptor.Intercept(IInvocation invocation)
        {
            var key = invocation.MethodInvocationTarget;
            var marker = this.cache[key] as TMarker;
            if (marker == null)
            {
                lock (this.cache.SyncRoot)
                {
// ReSharper disable ExpressionIsAlwaysNull
                    marker = this.cache[key] as TMarker;
// ReSharper restore ExpressionIsAlwaysNull
// ReSharper disable ConditionIsAlwaysTrueOrFalse
                    if (marker == null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                    {
                        try
                        {
                            if (Accept(invocation))
                            {
                                // method gets priority, then implementation, then interface
                                var markers = new List<TMarker>();
                                AddMarkers(markers, invocation.Method);
                                AddMarkers(markers, invocation.MethodInvocationTarget);
                                AddMarkers(markers, invocation.TargetType);

                                marker = markers.FirstOrDefault() ??
                                         (this.DefaultMarkerInstance == null ? new TMarker() : (TMarker)this.DefaultMarkerInstance.Clone());

                                if (!marker.Intercept)
                                    marker = null;
                            }
                            this.cache[key] = marker;
                        }
                        catch (Exception e)
                        {
                            this.Logger.Error(string.Format("Unable to parse or generate a marker for the invocation method {0}.", invocation.Method), e);
                            this.cache[key] = null;
                        }
                    }
                }
            }
            if (marker != null)
            {
                Intercept(invocation, marker);
            }
            else
            {
                invocation.Proceed();
            }
        }

        /// <summary>The attribute type that is used to mark whether to intercept a method or not.</summary>
        public Type MarkerType { get; private set; }

        /// <summary>Gets or sets the logger for this instance.</summary>
        public ILogger Logger
        {
            get { return this.log; }
            set { this.log = value ?? NullLogger.Instance; }
        }

        ///// <remarks>For windsor integration, this method is invoked after the component model is constructed.</remarks>
        //void IOnBehalfAware.SetInterceptedComponentModel(ComponentModel target)
        //{
        //    var options = ProxyUtil.ObtainProxyOptions(target, true);
        //    options.Selector = options.Selector ?? new InstanceReference<IInterceptorSelector>(new InterceptorSelector());

        //    TMarker overrideMarker = target.CustomDependencies[DefaultMarkerInstanceKey] as TMarker;
        //    if (overrideMarker != null)
        //        this.DefaultMarkerInstance = overrideMarker;
        //}

        protected static void AddMarkers(List<TMarker> markers, ICustomAttributeProvider attributeProvider)
        {
            if (attributeProvider != null)
                markers.AddRange(attributeProvider.GetCustomAttributes(typeof(TMarker), true).Cast<TMarker>());
        }

        protected abstract void Intercept(IInvocation invocation, TMarker marker);

        /// <summary>
        /// Used to specify that particular invocation should be included/excluded from the implementation interceptor.
        /// This can be useful for things like filtering out getter/setter properties.
        /// </summary>
        protected virtual bool Accept(IInvocation invocation)
        {
            return true;
        }

        protected void Log(LoggerLevel level, Func<string> message)
        {
            if (!IsLoggerEnabled(level))
                return;

            switch (level)
            {
                case LoggerLevel.Fatal:
                    this.log.Fatal(message());
                    break;
                case LoggerLevel.Error:
                    this.log.Error(message());
                    break;
                case LoggerLevel.Warn:
                    this.log.Warn(message());
                    break;
                case LoggerLevel.Info:
                    this.log.Info(message());
                    break;
                case LoggerLevel.Debug:
                    this.log.Debug(message());
                    break;
            }
        }

        protected bool IsLoggerEnabled(LoggerLevel level)
        {
            switch (level)
            {
                case LoggerLevel.Fatal:
                    return this.log.IsFatalEnabled;
                case LoggerLevel.Error:
                    return this.log.IsErrorEnabled;
                case LoggerLevel.Warn:
                    return this.log.IsWarnEnabled;
                case LoggerLevel.Info:
                    return this.log.IsInfoEnabled;
                case LoggerLevel.Debug:
                    return this.log.IsDebugEnabled;
            }
            return false;
        }
    }

    /// <summary>Interface which provides the type of a marker attribute.</summary>
    public interface IInterceptorMarkerProvider
    {
        /// <summary>The attribute type that is used to mark whether to intercept a method or not.</summary>
        Type MarkerType { get; }
    }

    /// <summary>Specifies a marker so that implementations of <see cref="InterceptorBase{TMarker}"/> know to intercept a specific method.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public abstract class MarkerBaseAttribute : Attribute, ICloneable
    {
        protected MarkerBaseAttribute()
        {
            this.LoggerLevel = LoggerLevel.Debug;
            this.Intercept = true;
        }

        /// <summary>
        /// Gets or sets the order in which to apply the interceptor.  The order of interception will ascending.  All values of <see cref="Int32"/> are valid.
        /// </summary>
        public int Order { get; set; }

        /// <summary>The logger level to use under <c>normal</c> use of an interceptor.  Exceptional cases may use a different level.  The default value is Debug.</summary>
        public LoggerLevel LoggerLevel { get; set; }

        /// <summary>Gets or sets whether to intercept or not.</summary>
        public bool Intercept { get; set; }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}