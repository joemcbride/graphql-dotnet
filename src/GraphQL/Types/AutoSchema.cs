using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// A schema with a Query type that is initialized to an instance
/// of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// with <typeparamref name="TQueryClrType"/> as the query CLR type.
/// </summary>
public class AutoSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] TQueryClrType> : Schema
{
    /// <summary>
    /// Initializes a new instance from the specified service provider.
    /// </summary>
    public AutoSchema(IServiceProvider serviceProvider) : base(serviceProvider, true)
    {
        Query = serviceProvider.GetRequiredService<AutoRegisteringObjectGraphType<TQueryClrType>>();
    }
}
