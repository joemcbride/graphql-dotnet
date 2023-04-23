namespace GraphQL.Types;

/// <summary>
/// Maps unmapped complex types to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// and <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>.
/// </summary>
public class AutoRegisteringGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    private readonly bool _mapInputTypes;
    private readonly bool _mapOutputTypes;
    private readonly bool _mapInterfaceTypes;

    /// <summary>
    /// Creates an instance that maps both input and output types.
    /// CLR interface output types will be mapped as GraphQL interfaces.
    /// </summary>
    public AutoRegisteringGraphTypeMappingProvider()
        : this(true, true)
    {
    }

    /// <summary>
    /// Creates an instance that maps input and/or output types, as specified.
    /// When output types are enabled, <paramref name="mapInterfaceTypes"/> indicates whether CLR
    /// interface output types are mapped as GraphQL interfaces or GraphQL object types.
    /// </summary>
    public AutoRegisteringGraphTypeMappingProvider(bool mapInputTypes, bool mapOutputTypes, bool mapInterfaceTypes = true)
    {
        _mapInputTypes = mapInputTypes;
        _mapOutputTypes = mapOutputTypes;
        _mapInterfaceTypes = mapInterfaceTypes;
    }

    /// <inheritdoc/>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringObjectGraphType<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringInterfaceGraphType<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(AutoRegisteringInputObjectGraphType<>))]
    public virtual Type? GetGraphTypeFromClrType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        if (isInputType && !_mapInputTypes ||
            !isInputType && !_mapOutputTypes ||
            clrType.IsEnum ||
            SchemaTypes.BuiltInScalarMappings.ContainsKey(clrType))
            return null;

        if (isInputType)
        {
            return typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType);
        }
        else if (clrType.IsInterface && _mapInterfaceTypes)
        {
            return typeof(AutoRegisteringInterfaceGraphType<>).MakeGenericType(clrType);
        }
        else
        {
            return typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        }
    }
}
