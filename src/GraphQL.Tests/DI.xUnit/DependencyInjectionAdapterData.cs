using System.Collections;
using GraphQL.DI;

namespace GraphQL.Tests.DI;

/// <summary>
/// Allows test methods from <see cref="QueryTestBase{TSchema, TDocumentBuilder}"/> and descendants
/// work with different DI providers. Mark your test method with [Theory] and [DependencyInjectionData]
/// attributes instead of [Fact] attribute. Also add 'string container' parameter (unused, the name of DI provider)
/// into your test method. By default all methods from <see cref="QueryTestBase{TSchema, TDocumentBuilder}"/>
/// and descendants (not marked with [DependencyInjectionData]) work with the first DI provider from
/// <see cref="DependencyInjectionAdapterData"/>.
/// </summary>
// TODO: remove or rewrite this class; should support theories in conjuction with multiple DI providers
internal sealed class DependencyInjectionAdapterData : IEnumerable<object[]>
{
    public static IEnumerable<IDependencyInjectionAdapter> GetDependencyInjectionAdapters(Action<IServiceRegister> configure)
    {
        yield return new MicrosoftAdapter(configure); // default DI provider
        yield return new StructureMapAdapter(configure);
        //yield return new CastleWindsorAdapter(configure);
    }

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "MSDI" }; // default DI provider
        yield return new object[] { "StructureMap" };
        //yield return new object[] { "CastleWindsor" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
