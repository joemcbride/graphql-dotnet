using GraphQL.DI;
using StructureMap;

namespace GraphQL.StructureMap;

public static class GraphQLBuilderExtensions
{
    /// <summary>
    /// Configures a GraphQL pipeline using the configuration delegate passed into
    /// <paramref name="configure"/> for the specified service collection and
    /// registers a default set of services required by GraphQL if they have not already been registered.
    /// <br/><br/>
    /// Does not include <see cref="IGraphQLSerializer"/>, and the default <see cref="IDocumentExecuter"/>
    /// implementation does not support subscriptions.
    /// </summary>
    public static IRegistry AddGraphQL(this IRegistry registry, Action<IGraphQLBuilder>? configure)
    {
        _ = new GraphQLBuilder(registry, configure);
        return registry;
    }
}
