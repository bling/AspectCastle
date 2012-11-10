using System;
using AspectCastle.Core;
using Castle.DynamicProxy;

namespace AspectCastle
{
    /// <summary>A class which provides the ability to generate proxies which adhere to interception ordering and per-method features.</summary>
    public class ProxyFactory
    {
        private readonly ProxyGenerator generator = new ProxyGenerator(false);
        private readonly ProxyGenerationOptions proxyOptions = new ProxyGenerationOptions { Selector = new InterceptorSelector() };

        /// <summary>Creates proxy object intercepting calls to virtual members of type classToProxy on newly created instance of that type with given interceptors.</summary>
        public object CreateClassProxy(Type classToProxy, params IInterceptor[] interceptors)
        {
            return this.generator.CreateClassProxy(classToProxy, this.proxyOptions, interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to virtual members of type classToProxy on newly created instance of that type with given interceptors.</summary>
        public object CreateClassProxy(object[] constructorArguments, Type classToProxy, params IInterceptor[] interceptors)
        {
            return this.generator.CreateClassProxy(classToProxy, Type.EmptyTypes, this.proxyOptions, constructorArguments, interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to virtual members of type classToProxy on newly created instance of that type with given interceptors.</summary>
        public T CreateClassProxy<T>(params IInterceptor[] interceptors)
        {
            return (T)CreateClassProxy(typeof(T), interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to virtual members of type classToProxy on newly created instance of that type with given interceptors.</summary>
        public T CreateClassProxy<T>(object[] constructorArguments, params IInterceptor[] interceptors)
        {
            return (T)CreateClassProxy(constructorArguments, typeof(T), interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to members of interface interfaceToProxy on target object with given interceptors.</summary>
        public object CreateInterfaceProxyWithTarget(object target, Type interfaceToProxy, params IInterceptor[] interceptors)
        {
            return this.generator.CreateInterfaceProxyWithTarget(interfaceToProxy, target, this.proxyOptions, interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to members of interface interfaceToProxy on target object with given interceptors.</summary>
        public TInterface CreateInterfaceProxyWithTarget<TInterface>(object target, params IInterceptor[] interceptors) where TInterface : class
        {
            return (TInterface)CreateInterfaceProxyWithTarget(target, typeof(TInterface), interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to members of interface interfaceToProxy on target object generated at runtime with given interceptors.</summary>
        public object CreateInterfaceProxyWithoutTarget(Type interfaceToProxy, params IInterceptor[] interceptors)
        {
            return CreateInterfaceProxyWithoutTarget(interfaceToProxy, Type.EmptyTypes, interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to members of interface interfaceToProxy on target object with given interceptors.</summary>
        public TInterface CreateInterfaceProxyWithoutTarget<TInterface>(params IInterceptor[] interceptors)
        {
            return (TInterface)CreateInterfaceProxyWithoutTarget(typeof(TInterface), Type.EmptyTypes, interceptors);
        }

        /// <summary>Creates proxy object intercepting calls to members of interface interfaceToProxy on target object generated at runtime with given interceptors.</summary>
        public object CreateInterfaceProxyWithoutTarget(Type interfaceToProxy, Type[] additionalInterfacesToProxy, params IInterceptor[] interceptors)
        {
            return this.generator.CreateInterfaceProxyWithoutTarget(interfaceToProxy, additionalInterfacesToProxy, this.proxyOptions, interceptors);
        }
    }
}