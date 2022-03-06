using GraphQL.DI;
using GraphQL.MicrosoftDI;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.DI;

internal sealed class MicrosoftAdapter : IDependencyInjectionAdapter
{
    private readonly GraphQLBuilder _builder;
    private IServiceProvider _provider;

    public MicrosoftAdapter(Action<IServiceRegister> configure)
    {
        _builder = new GraphQLBuilder(new ServiceCollection(), b => configure(b.Services));
    }

    public IServiceProvider ServiceProvider => _provider ??= _builder.ServiceCollection.BuildServiceProvider();
}
