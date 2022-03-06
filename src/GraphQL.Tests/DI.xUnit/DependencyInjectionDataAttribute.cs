using System.Reflection;

namespace GraphQL.Tests.DI;

internal sealed class DependencyInjectionDataAttribute : ClassDataAttribute
{
    public DependencyInjectionDataAttribute() : base(typeof(DependencyInjectionAdapterData))
    {
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        return testMethod.DeclaringType.IsDefined(typeof(PrepareDependencyInjectionAttribute))
            ? base.GetData(testMethod)
            : throw new InvalidOperationException("[DependencyInjectionData] attribute should only be used for methods of types marked with [PrepareDependencyInjection] attribute, for example, QueryTestBase<TSchema, TDocumentBuilder>.");
    }
}
