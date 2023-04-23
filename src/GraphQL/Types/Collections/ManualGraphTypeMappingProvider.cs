namespace GraphQL.Types.Collections;

internal class ManualGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    private readonly Type _clrType;
    private readonly Type _graphType;
    private readonly bool _isInputType;
    private readonly bool _isOutputType;

    public ManualGraphTypeMappingProvider(Type clrType, Type graphType)
    {
        _clrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
        _graphType = graphType ?? throw new ArgumentNullException(nameof(graphType));
        _isInputType = graphType.IsInputType();
        _isOutputType = graphType.IsOutputType();
    }

    public Type? GetGraphTypeFromClrType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type clrType, bool isInputType, Type? preferredGraphType)
        => clrType == _clrType && (isInputType && _isInputType || !isInputType && _isOutputType) ? _graphType : preferredGraphType;
}
