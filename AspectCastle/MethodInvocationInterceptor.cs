using System;
using System.Diagnostics;
using System.Text;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>Intercepts methods and logs entry, exit, any errors/exceptions, and optionally instruments the speed.</summary>
    public class MethodInvocationInterceptor : InterceptorBase<WithMethodInvocationAttribute>
    {
        protected override void Intercept(IInvocation invocation, WithMethodInvocationAttribute marker)
        {
            if (!this.IsLoggerEnabled(marker.LoggerLevel))
            {
                invocation.Proceed();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}.{1}(", invocation.TargetType != null ? invocation.TargetType.Name : invocation.Method.DeclaringType.Name, invocation.Method.Name);
            foreach (object obj in invocation.Arguments)
            {
                if (marker.EvaluateArguments)
                {
                    sb.Append(obj ?? "<null>");
                }
                else
                {
                    sb.Append(obj != null ? obj.GetType().Name : "<null>");
                }
                sb.Append(", ");
            }
            
            if (invocation.Arguments.Length > 0)
                sb.Remove(sb.Length - 2, 2);

            sb.Append(") invoked");
            var sw = Stopwatch.StartNew();
            bool error = false;
            try
            {
                invocation.Proceed();
            }
            catch (Exception e)
            {
                error = true;
                sb.AppendFormat(" and encountered an exception: {0}.", e);
                throw;
            }
            finally
            {
                if (!error)
                    sb.Append(" and completed successfully.");

                if (marker.Instrument)
                {
                    sb.AppendFormat("  ({0}ms).", sw.ElapsedMilliseconds);
                }
                Console.WriteLine(sb.ToString());
                Log(marker.LoggerLevel, sb.ToString);
            }
        }
    }

    /// <summary>Marks a class/method to intercept with the <see cref="MethodInvocationInterceptor"/>.</summary>
    public sealed class WithMethodInvocationAttribute : MarkerBaseAttribute
    {
        /// <summary>Initializes a new instance of <see cref="WithMethodInvocationAttribute"/>.</summary>
        public WithMethodInvocationAttribute()
        {
            this.Instrument = true;
        }

        /// <summary>Enables or disables runtime instrumentation output.</summary>
        public bool Instrument { get; set; }

        /// <summary>Enables or disables evaluting the values of arguments.</summary>
        public bool EvaluateArguments { get; set; }
    }
}