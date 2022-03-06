using StructureMap;

namespace GraphQL.StructureMap;

public sealed class ServiceProviderAdapter : IServiceProvider
{
    public ServiceProviderAdapter(IContainer container)
    {
        Container = container;
    }

    public IContainer Container { get; }

    public object GetService(Type serviceType) => Container.GetInstance(serviceType);
}
