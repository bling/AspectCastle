using System;
using System.Linq;
using AspectCastle.Core;
using Castle.Core.Logging;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>
    /// An exception policy interceptor executes certain policies when exceptions are thrown.  The target object must implement <see cref="IHandler"/>.
    /// </summary>
    public sealed class ExceptionPolicyInterceptor : InterceptorBase<WithExceptionPolicyAttribute>
    {
        protected override void Intercept(IInvocation invocation, WithExceptionPolicyAttribute marker)
        {
            try
            {
                invocation.Proceed();
            }
            catch (Exception e)
            {
                if (marker.FilteredTypes.Length > 0)
                {
                    if (!marker.FilteredTypes.Contains(e.GetType()))
                    {
                        Log(marker.LoggerLevel, () => "Exception not found in filters.  Rethrowing.");
                        throw;
                    }
                }
                
                var handler = invocation.InvocationTarget as IHandler;
                if (handler != null)
                {
                    Log(marker.LoggerLevel, () => "Invoking exception policy handler.");
                    handler.HandleException(marker.Tag ?? invocation.Method.ToString(), e);
                }
                else
                {
                    Log(marker.LoggerLevel, () => "No exception policy handler set.");
                }

                if (!marker.Swallow)
                {
                    Log(marker.LoggerLevel, () => "Exception is rethrown.");
                    throw;
                }
                Log(marker.LoggerLevel, () => "Exception is swallowed as per the exception policy attribute.");
            }
        }

        #region Nested type: IHandler

        /// <summary>If the target instance implements this interface, it will be invoked when the <see cref="ExceptionPolicyInterceptor"/> catches an exception.</summary>
        public interface IHandler
        {
            /// <summary>Handles the caught exception.</summary>
            void HandleException(string tag, Exception exception);
        }

        #endregion
    }

    /// <summary>Marks a class/method to intercept with the <see cref="ExceptionPolicyInterceptor"/>.</summary>
    public sealed class WithExceptionPolicyAttribute : MarkerBaseAttribute
    {
        /// <summary>Initializes a new instance of <see cref="WithExceptionPolicyAttribute"/>.</summary>
        public WithExceptionPolicyAttribute()
            : this(Type.EmptyTypes)
        {
            this.LoggerLevel = LoggerLevel.Error;
        }

        /// <summary>Initializes a new instance of <see cref="WithExceptionPolicyAttribute"/>.</summary>
        /// <param name="filteredTypes">Exception types to filter.</param>
        public WithExceptionPolicyAttribute(params Type[] filteredTypes)
        {
            foreach (var t in filteredTypes)
                if (!typeof(Exception).IsAssignableFrom(t))
                    throw new ArgumentException("One of the types cannot be assigned to Exception.");

            this.FilteredTypes = filteredTypes;
            this.LoggerLevel = LoggerLevel.Error;
        }

        /// <summary>
        /// Filters the exception handling block to only handle the exception types specified.  If empty,
        /// filters are off.  Unmatched exceptions will be rethrown.
        /// </summary>
        public Type[] FilteredTypes { get; private set; }

        /// <summary>An optional tag that can be used alongside with <see cref="ExceptionPolicyInterceptor.IHandler"/> to differentiate between different caught exceptions.</summary>
        public string Tag { get; set; }

        /// <summary>Gets or sets whether the caught exception should be swallowed.</summary>
        public bool Swallow { get; set; }
    }
}