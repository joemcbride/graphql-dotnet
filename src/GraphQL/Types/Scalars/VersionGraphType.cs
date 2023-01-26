using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// The Version scalar graph type represents a version number according to SemVer.
/// </summary>
public class VersionGraphType : ScalarGraphType
{
    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue s => Version.Parse(
#if NETSTANDARD2_0
            (string)
#endif
            s.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue s => Version.TryParse(
#if NETSTANDARD2_0
            (string)
#endif
            s.Value, out _),
        GraphQLNullValue _ => true,
        _ => false
    };

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        Version _ => value, // no boxing
        string s => Version.Parse(s),
        null => null,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value switch
    {
        Version _ => true,
        string s => Version.TryParse(s, out _),
        null => true,
        _ => false
    };

    /// <inheritdoc/>
    public override object? Serialize(object? value) => value switch
    {
        Version g => g.ToString(),
        null => null,
        _ => ThrowSerializationError(value)
    };
}
