using Microsoft.CodeAnalysis.Testing;

namespace GraphQL.Analyzers.Tests.VerifiersExtensions;

public static class AnalyzerTestInitializer
{
    public static void Initialize<TVerifier>(this AnalyzerTest<TVerifier> test)
        where TVerifier : IVerifier, new()
    {
        test.ReferenceAssemblies = ReferenceResolver.ResolveReferenceAssemblies();
        test.TestState.AdditionalReferences.Add(typeof(Types.ISchema).Assembly.Location);
        test.TestState.AdditionalReferences.Add(typeof(MicrosoftDI.GraphQLBuilder).Assembly.Location);
    }
}
