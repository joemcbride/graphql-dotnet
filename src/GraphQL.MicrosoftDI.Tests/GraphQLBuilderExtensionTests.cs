using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI.Tests;

public class GraphQLBuilderExtensionTests
{
    [Fact]
    public void AddGraphQL()
    {
        bool executed = false;
        var services = new ServiceCollection();
        services.AddGraphQL(builder =>
        {
            builder.ShouldBeOfType<GraphQLBuilder>();
            executed = true;
        });
        executed.ShouldBeTrue();
        services.BuildServiceProvider().GetService<IDocumentExecuter>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(DI.ServiceLifetime.Singleton)]
    [InlineData(DI.ServiceLifetime.Scoped)]
    public void AddSelfActivatingSchema(DI.ServiceLifetime serviceLifetime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Class2.Instance);
        services.AddGraphQL(b => b.AddSelfActivatingSchema<MySchema>(serviceLifetime));
        services.Single(x => x.ServiceType == typeof(MySchema)).Lifetime.ShouldBe(serviceLifetime switch
        {
            DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
            DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
            _ => throw new ApplicationException()
        });
        services.Single(x => x.ServiceType == typeof(ISchema)).Lifetime.ShouldBe(serviceLifetime switch
        {
            DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
            DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
            _ => throw new ApplicationException()
        });
        using var serviceProvider = services.BuildServiceProvider();
        var schema1 = serviceProvider.GetRequiredService<MySchema>();
        schema1.ShouldBeOfType<MySchema>();
        var schema2 = serviceProvider.GetRequiredService<ISchema>();
        schema2.ShouldBeOfType<MySchema>();
        ReferenceEquals(schema1, schema2).ShouldBeTrue();
    }

    [Fact]
    public void AddSelfActivatingSchema_Transient()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(builder => Should.Throw<InvalidOperationException>(() => builder.AddSelfActivatingSchema<MySchema>(DI.ServiceLifetime.Transient)));
    }

    private class MySchema : Schema
    {
        public MySchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            serviceProvider.ShouldBeOfType<SelfActivatingServiceProvider>();
            //test pulling a registered class
            serviceProvider.GetRequiredService<Class2>().ShouldBe(Class2.Instance);
            //test pulling an unregistered class
            serviceProvider.GetRequiredService<Class1>().ShouldNotBeNull();
        }
    }

    private class Class1
    {
    }

    private class Class2
    {
        public static Class2 Instance = new();

        private Class2()
        {
        }
    }
}
