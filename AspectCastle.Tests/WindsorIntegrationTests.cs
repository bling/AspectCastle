//using System;
//using NUnit.Framework;

//namespace AspectCastle.Tests
//{
//    [TestFixture]
//    public class WindsorIntegrationTests
//    {
//        public interface IInterface
//        {
//            void Throw1();

//            void Throw2();
//        }

//        public class NoMarkerOnType
//        {
//            public virtual void Throw()
//            {
//                throw new AppDomainUnloadedException();
//            }
//        }

//        [Interceptor(typeof(ExceptionPolicyInterceptor))]
//        public class InterfaceTesterOnMethods : IInterface
//        {
//            #region IInterface Members

//            [WithExceptionPolicy(Swallow = true)]
//            public void Throw1()
//            {
//                throw new Exception();
//            }

//            public void Throw2()
//            {
//                throw new Exception();
//            }

//            #endregion
//        }

//        [Interceptor(typeof(ExceptionPolicyInterceptor))]
//        [WithExceptionPolicy(Swallow = true)]
//        public class InterfaceTesterOnAll : IInterface
//        {
//            #region IInterface Members

//            public void Throw1()
//            {
//                throw new Exception();
//            }

//            public void Throw2()
//            {
//                throw new Exception();
//            }

//            #endregion
//        }

//        [Interceptor(typeof(ExceptionPolicyInterceptor))]
//        public class VirtualTester
//        {
//            [WithExceptionPolicy(Swallow = true)]
//            public virtual void Throw1()
//            {
//                throw new Exception();
//            }

//            public void Throw2()
//            {
//                throw new Exception();
//            }
//        }

//        [Test]
//        public void No_marker_on_type_and_no_default_property_still_gets_intercepted()
//        {
//            ExceptionPolicyInterceptor epi = new ExceptionPolicyInterceptor();
//            Assert.IsNull(epi.DefaultMarkerInstance); // none set
//            IWindsorContainer c = new WindsorContainer();
//            c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(epi));
//            c.Register(Component.For<NoMarkerOnType>().Interceptors(new InterceptorReference(typeof(ExceptionPolicyInterceptor))).Anywhere);
//            var i = c.Resolve<NoMarkerOnType>();
//            Assert.Throws<AppDomainUnloadedException>(i.Throw);
//        }

//        [Test]
//        public void No_marker_on_type_still_gets_intercept_with_default_property()
//        {
//            ExceptionPolicyInterceptor epi = new ExceptionPolicyInterceptor { DefaultMarkerInstance = new WithExceptionPolicyAttribute { Swallow = true } };
//            IWindsorContainer c = new WindsorContainer();
//            c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(epi));
//            c.Register(Component.For<NoMarkerOnType>().Interceptors(new InterceptorReference(typeof(ExceptionPolicyInterceptor))).Anywhere);
//            var i = c.Resolve<NoMarkerOnType>();
//            Assert.DoesNotThrow(i.Throw);
//        }

//        [Test]
//        public void Proxy_is_created_for_interface_for_marked_methods()
//        {
//            ExceptionPolicyInterceptor epi = new ExceptionPolicyInterceptor();
//            IWindsorContainer c = new WindsorContainer();
//            c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(epi));
//            c.Register(Component.For<IInterface>().ImplementedBy<InterfaceTesterOnMethods>());
//            var i = c.Resolve<IInterface>();
//            Assert.DoesNotThrow(i.Throw1);
//            Assert.Throws<Exception>(i.Throw2);
//        }

//        [Test]
//        public void Proxy_is_created_for_interface_with_type_marker()
//        {
//            ExceptionPolicyInterceptor epi = new ExceptionPolicyInterceptor();
//            IWindsorContainer c = new WindsorContainer();
//            c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(epi));
//            c.Register(Component.For<IInterface>().ImplementedBy<InterfaceTesterOnAll>());
//            var i = c.Resolve<IInterface>();
//            Assert.DoesNotThrow(i.Throw1);
//            Assert.DoesNotThrow(i.Throw2);
//        }

//        [Test]
//        public void Proxy_is_created_for_virtual_classes()
//        {
//            ExceptionPolicyInterceptor epi = new ExceptionPolicyInterceptor();
//            IWindsorContainer c = new WindsorContainer();
//            c.Register(Component.For<ExceptionPolicyInterceptor>().Instance(epi));
//            c.Register(Component.For<VirtualTester>());
//            var i = c.Resolve<VirtualTester>();
//            Assert.DoesNotThrow(i.Throw1);
//            Assert.Throws<Exception>(i.Throw2);
//        }

//        public interface IFactory
//        {
//            NoMarkerOnType Create();
//        }

//        [Test]
//        public void Proxy_is_created_for_proxies_with_no_concrete_implementation()
//        {
//            IWindsorContainer c = new WindsorContainer();
//            c.AddFacility<TypedFactoryFacility>();
//            c.Register(Component.For<NoMarkerOnType>());
//            c.Register(Component.For<IFactory>().AsFactory());
//            Assert.Throws<AppDomainUnloadedException>(() => c.Resolve<IFactory>().Create().Throw());
//        }

//        [Interceptor(typeof(MethodInvocationInterceptor))]
//        public class DoNothing
//        {
//            public virtual void NoOp()
//            {
//            }
//        }

//        [Test]
//        public void Default_marker_instance_custom_dependency_works()
//        {
//            IWindsorContainer c = new WindsorContainer();
//            WithMethodInvocationAttribute e = new WithMethodInvocationAttribute();
//            c.Register(Component.For<MethodInvocationInterceptor>());
//            c.Register(Component.For<DoNothing>().DependsOn(Property.ForKey(ExceptionPolicyInterceptor.DefaultMarkerInstanceKey).Eq(e)));
//            c.Resolve<DoNothing>().NoOp();
//            Assert.That(c.Resolve<MethodInvocationInterceptor>().DefaultMarkerInstance, Is.SameAs(e));
//        }
//    }
//}