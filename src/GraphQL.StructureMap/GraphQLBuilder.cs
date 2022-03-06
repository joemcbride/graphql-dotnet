using GraphQL.DI;
using StructureMap;
using StructureMap.Pipeline;

namespace GraphQL.StructureMap;

public class GraphQLBuilder : GraphQLBuilderBase, IServiceRegister
{
    /// <inheritdoc />
    public override IServiceRegister Services => this;

    public IRegistry Registry { get; }

    public GraphQLBuilder(IRegistry registry, Action<IGraphQLBuilder>? configure)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        configure?.Invoke(this);
        RegisterDefaultServices();
        Registry.For<IServiceProvider>(Lifecycles.Container).Use<ServiceProviderAdapter>();
    }

    private static ILifecycle TranslateLifetime(ServiceLifetime serviceLifetime)
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Singleton => Lifecycles.Singleton,
            ServiceLifetime.Scoped => Lifecycles.Singleton, //TODO,
            ServiceLifetime.Transient => Lifecycles.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(serviceLifetime))
        };
    }

    public IServiceRegister Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
        where TOptions : class, new()
    {
        //this.TryRegister(services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions(), ServiceLifetime.Singleton);
        //if (action != null)
        //{
        //    // This is used instead of "normal" services.Configure(configureOptions) to pass IServiceProvider to user code.
        //    this.Register<IConfigureOptions<TOptions>>(services => new ConfigureNamedOptions<TOptions>(Options.DefaultName, opt => action(opt, services)), ServiceLifetime.Singleton);
        //}

        return this;
    }

    public IServiceRegister Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).ClearAll().Add(implementationType);
        }
        else
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).Add(implementationType);
        }

        return this;
    }

    public IServiceRegister Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).ClearAll().Add(context => implementationFactory(context.GetInstance<IServiceProvider>()));
        }
        else
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).Add(context => implementationFactory(context.GetInstance<IServiceProvider>()));
        }

        return this;
    }

    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, Lifecycles.Singleton).ClearAll().Add(implementationInstance);
        }
        else
        {
            Registry.For(serviceType, Lifecycles.Singleton).Add(implementationInstance);
        }

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        if (mode == RegistrationCompareMode.ServiceType)
        {
            Register(serviceType, implementationType, serviceLifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Register(serviceType, implementationType, serviceLifetime);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationFactory == null)
            throw new ArgumentNullException(nameof(implementationFactory));

        if (mode == RegistrationCompareMode.ServiceType)
        {
            Register(serviceType, implementationFactory, serviceLifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Register(serviceType, implementationFactory, serviceLifetime);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationInstance == null)
            throw new ArgumentNullException(nameof(implementationInstance));

        if (mode == RegistrationCompareMode.ServiceType)
        {
            Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Register(serviceType, implementationInstance);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }
}
