using GraphQL.DI;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Provides configuration for an auto-schema.
/// See <see cref="GraphQLBuilderExtensions.AddAutoSchema{TQueryClrType}(IGraphQLBuilder, Action{IConfigureAutoSchema})"/>.
/// </summary>
public interface IConfigureAutoSchema
{
    /// <summary>
    /// Returns a <see cref="IGraphQLBuilder"/> reference that can be used to configure the schema or service provider.
    /// </summary>
    IGraphQLBuilder Builder { get; }

    /// <summary>
    /// Returns the type of constructed schema, which can be used to type match prior to additional configurations.
    /// Usually it is always <see cref="AutoSchema{TQueryClrType}"/>.
    /// </summary>
    Type SchemaType { get; }
}

internal class ConfigureAutoSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] TQueryClrType> : IConfigureAutoSchema
{
    public ConfigureAutoSchema(IGraphQLBuilder baseBuilder)
    {
        Builder = baseBuilder;
        SchemaType = typeof(AutoSchema<TQueryClrType>);
    }

    public IGraphQLBuilder Builder { get; }

    public Type SchemaType { get; }
}
