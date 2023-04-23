namespace GraphQL.Types;

/// <summary>
/// Provides a mapping from CLR types to graph types.
/// </summary>
public interface IGraphTypeMappingProvider
{
    /// <summary>
    /// Returns a graph type for a given CLR type, or <see langword="null"/> if no mapping is available.
    /// Should return <paramref name="preferredGraphType"/> if this instance does not wish to change the mapping.
    /// </summary>
    /// <param name="clrType">The CLR type to be mapped.</param>
    /// <param name="isInputType">Indicates whether the type is an input type.</param>
    /// <param name="preferredGraphType">The graph type that is suggested for this CLR type.</param>
    Type? GetGraphTypeFromClrType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type clrType, bool isInputType, Type? preferredGraphType);
}
