using StructureMap;

namespace GraphQL.StructureMap.Tests
{
    public class GraphQLBuilderTests
    {
        [Fact]
        public void NullConstructor()
        {
            Should.Throw<ArgumentNullException>(() => new GraphQLBuilder(null, _ => { }));
        }

        //[Theory]
        //[InlineData(typeof(List<>), typeof(List<>), DI.ServiceLifetime.Singleton, false, false)]
        //[InlineData(typeof(IList<>), typeof(List<>), DI.ServiceLifetime.Singleton, false, false)]
        //[InlineData(typeof(Class1), typeof(Class1), DI.ServiceLifetime.Singleton, false, false)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Singleton, false, false)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Scoped, false, false)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Transient, false, false)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Scoped, true, false)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Scoped, true, true)]
        //public void Register(Type serviceType, Type implementationType, DI.ServiceLifetime serviceLifetime, bool replace, bool withExisting)
        //{
        //    bool match = false;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == serviceType)
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationType.ShouldBe(implementationType);
        //            d.Lifetime.ShouldBe(serviceLifetime switch
        //            {
        //                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
        //                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
        //                DI.ServiceLifetime.Transient => ServiceLifetime.Transient,
        //                _ => throw new ApplicationException()
        //            });
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    if (replace && withExisting)
        //    {
        //        var toRemove = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
        //        descriptorList.Add(toRemove);
        //        mockServiceCollection.Setup(x => x.Remove(toRemove)).Returns<ServiceDescriptor>(d => descriptorList.Remove(d)).Verifiable();
        //    }
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.Register(serviceType, implementationType, serviceLifetime, replace));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Theory]
        //[InlineData(DI.ServiceLifetime.Singleton, false, false)]
        //[InlineData(DI.ServiceLifetime.Scoped, false, false)]
        //[InlineData(DI.ServiceLifetime.Transient, false, false)]
        //[InlineData(DI.ServiceLifetime.Scoped, true, false)]
        //[InlineData(DI.ServiceLifetime.Scoped, true, true)]
        //public void Register_Factory(DI.ServiceLifetime serviceLifetime, bool replace, bool withExisting)
        //{
        //    bool match = false;
        //    Func<IServiceProvider, Class1> factory = _ => null;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == typeof(Interface1))
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationFactory.ShouldBe(factory);
        //            d.Lifetime.ShouldBe(serviceLifetime switch
        //            {
        //                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
        //                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
        //                DI.ServiceLifetime.Transient => ServiceLifetime.Transient,
        //                _ => throw new ApplicationException()
        //            });
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    if (replace && withExisting)
        //    {
        //        var toRemove = new ServiceDescriptor(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient);
        //        descriptorList.Add(toRemove);
        //        mockServiceCollection.Setup(x => x.Remove(toRemove)).Returns<ServiceDescriptor>(d => descriptorList.Remove(d)).Verifiable();
        //    }
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.Register<Interface1>(factory, serviceLifetime, replace));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Theory]
        //[InlineData(false, false)]
        //[InlineData(true, false)]
        //[InlineData(true, true)]
        //public void Register_Instance(bool replace, bool withExisting)
        //{
        //    bool match = false;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var instance = new Class1();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == typeof(Interface1))
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationInstance.ShouldBe(instance);
        //            d.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    if (replace && withExisting)
        //    {
        //        var toRemove = new ServiceDescriptor(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient);
        //        descriptorList.Add(toRemove);
        //        mockServiceCollection.Setup(x => x.Remove(toRemove)).Returns<ServiceDescriptor>(d => descriptorList.Remove(d)).Verifiable();
        //    }
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.Register<Interface1>(instance, replace));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Theory]
        //[InlineData(typeof(List<>), typeof(List<>), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(IList<>), typeof(List<>), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Class1), typeof(Class1), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Scoped)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Transient)]
        //public void TryRegister_Succeed(Type serviceType, Type implementationType, DI.ServiceLifetime serviceLifetime)
        //{
        //    bool match = false;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == serviceType)
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationType.ShouldBe(implementationType);
        //            d.Lifetime.ShouldBe(serviceLifetime switch
        //            {
        //                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
        //                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
        //                DI.ServiceLifetime.Transient => ServiceLifetime.Transient,
        //                _ => throw new ApplicationException()
        //            });
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.TryRegister(serviceType, implementationType, serviceLifetime));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Theory]
        //[InlineData(typeof(List<>), typeof(List<>), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(IList<>), typeof(List<>), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Class1), typeof(Class1), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Singleton)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Scoped)]
        //[InlineData(typeof(Interface1), typeof(Class1), DI.ServiceLifetime.Transient)]
        //public void TryRegister_Fail(Type serviceType, Type implementationType, DI.ServiceLifetime serviceLifetime)
        //{
        //    bool match = false;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == serviceType)
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationType.ShouldBeNull();
        //            d.ImplementationFactory.ShouldNotBeNull();
        //            d.Lifetime.ShouldBe(ServiceLifetime.Transient);
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    services.AddTransient(serviceType, _ => null);
        //    var builder = new GraphQLBuilder(services, b => b.Services.TryRegister(serviceType, implementationType, serviceLifetime));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Theory]
        //[InlineData(DI.ServiceLifetime.Singleton)]
        //[InlineData(DI.ServiceLifetime.Scoped)]
        //[InlineData(DI.ServiceLifetime.Transient)]
        //public void TryRegister_Factory(DI.ServiceLifetime serviceLifetime)
        //{
        //    bool match = false;
        //    Func<IServiceProvider, Class1> factory = _ => null;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == typeof(Interface1))
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationFactory.ShouldBe(factory);
        //            d.Lifetime.ShouldBe(serviceLifetime switch
        //            {
        //                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
        //                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
        //                DI.ServiceLifetime.Transient => ServiceLifetime.Transient,
        //                _ => throw new ApplicationException()
        //            });
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.TryRegister<Interface1>(factory, serviceLifetime));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        //[Fact]
        //public void TryRegister_Instance()
        //{
        //    bool match = false;
        //    var descriptorList = new List<ServiceDescriptor>();
        //    var instance = new Class1();
        //    var mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        //    mockServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>())).Callback<ServiceDescriptor>(d =>
        //    {
        //        if (d.ServiceType == typeof(Interface1))
        //        {
        //            match.ShouldBeFalse();
        //            d.ImplementationInstance.ShouldBe(instance);
        //            d.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        //            match = true;
        //        }
        //        descriptorList.Add(d);
        //    }).Verifiable();
        //    mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => descriptorList.GetEnumerator());
        //    var services = mockServiceCollection.Object;
        //    var builder = new GraphQLBuilder(services, b => b.Services.TryRegister<Interface1>(instance));
        //    mockServiceCollection.Verify();
        //    match.ShouldBeTrue();
        //}

        [Fact]
        public void Register_InvalidParameters()
        {
            var builder = new GraphQLBuilder(new Registry(), _ => { });
            Should.Throw<ArgumentNullException>(() => builder.Register(null, typeof(Class1), DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.Register(typeof(Class1), (Type)null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.Register(null, _ => null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.Register(typeof(Class1), (Func<IServiceProvider, object>)null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.Register(null, new Class1()));
            Should.Throw<ArgumentNullException>(() => builder.Register(typeof(Class1), null));
            Should.Throw<ArgumentOutOfRangeException>(() => builder.Register(typeof(Class1), typeof(Class1), (DI.ServiceLifetime)10));
            Should.Throw<ArgumentOutOfRangeException>(() => builder.Register(typeof(Class1), _ => null, (DI.ServiceLifetime)10));
        }

        [Fact]
        public void TryRegister_InvalidParameters()
        {
            var builder = new GraphQLBuilder(new Registry(), _ => { });
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(null, typeof(Class1), DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(typeof(Class1), (Type)null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(null, _ => null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(typeof(Class1), (Func<IServiceProvider, object>)null, DI.ServiceLifetime.Singleton));
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(null, new Class1()));
            Should.Throw<ArgumentNullException>(() => builder.TryRegister(typeof(Class1), null));
            Should.Throw<ArgumentOutOfRangeException>(() => builder.TryRegister(typeof(Class1), typeof(Class1), (DI.ServiceLifetime)10));
            Should.Throw<ArgumentOutOfRangeException>(() => builder.TryRegister(typeof(Class1), _ => null, (DI.ServiceLifetime)10));
        }

        //[Fact]
        //public void Configure()
        //{
        //    var services = new ServiceCollection();
        //    services.AddGraphQL(b => b.Services.Configure<TestOptions>());
        //    services.BuildServiceProvider().GetRequiredService<TestOptions>().Value.ShouldBe(0);
        //    services.BuildServiceProvider().GetRequiredService<IOptions<TestOptions>>().Value.Value.ShouldBe(0);
        //}

        //[Fact]
        //public void Configure_Value()
        //{
        //    var services = new ServiceCollection();
        //    services.AddGraphQL(b => b.Services.Configure<TestOptions>(o => o.Value += 1));
        //    var serviceProvider = services.BuildServiceProvider();
        //    serviceProvider.GetRequiredService<TestOptions>().Value.ShouldBe(1);
        //    serviceProvider.GetRequiredService<TestOptions>().Value.ShouldBe(1); //ensure execution only occurs once
        //    services.BuildServiceProvider().GetRequiredService<IOptions<TestOptions>>().Value.Value.ShouldBe(1);
        //}

        //[Fact]
        //public void Configure_Multiple()
        //{
        //    var services = new ServiceCollection();
        //    services.AddGraphQL(b => b.Services
        //        .Configure<TestOptions>(o => o.Value += 1)
        //        .Configure<TestOptions>(o => o.Value += 2));
        //    var serviceProvider = services.BuildServiceProvider();
        //    serviceProvider.GetRequiredService<TestOptions>().Value.ShouldBe(3);
        //    serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value.Value.ShouldBe(3);
        //}

        //[Fact]
        //public void Configure_Options()
        //{
        //    var services = new Registry();
        //    services.AddGraphQL(b => b.Services.Configure<TestOptions>());
        //    services.Configure<TestOptions>(o => o.Value += 1);
        //    services.Configure<TestOptions>(o => o.Value += 2);
        //    var serviceProvider = services.BuildServiceProvider();
        //    serviceProvider.GetRequiredService<TestOptions>().Value.ShouldBe(3);
        //    serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value.Value.ShouldBe(3);
        //}

        private class TestOptions
        {
            public int Value { get; set; }
        }

        private class Class1 : Interface1
        {
        }

        private interface Interface1
        {
        }
    }
}
