AspectCastle
============

A lightweight AOP framework built on top of Castle DynamicProxy.

## Introduction
Castle DynamicProxy provides a simple and easy way to write interceptors through the IInterceptor interface.  However, it's quite cumbersome when you want to customize specific behaviors of the interception, specifically, the order of interception, and filtering on which methods to intercept.  AspectCastle makes it very easy to accomplish both of these tasks.

## Example
    public class Foo {
        [WithMethodInvocation]
        public virtual void Bar1() { }
        
        public virtual void Bar2() { }
    }

    var f = new AspectCastle.ProxyFactory().CreateClassProxy<Foo>();
    f.Bar1(); // this is intercepted with the MethodInvocationInterceptor
    f.Bar2(); // this is not

## License
All source code is released under the [Ms-PL](http://www.opensource.org/licenses/ms-pl) license.