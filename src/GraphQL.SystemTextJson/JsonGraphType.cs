using System.Text.Json;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.SystemTextJson;

/// <summary>
/// The Json scalar graph type represents a JSON document.
/// </summary>
public class JsonGraphType : ScalarGraphType
{
    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue s => JsonDocument.Parse(s.Value), // https://github.com/dotnet/runtime/issues/49207
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value is GraphQLStringValue || value is GraphQLNullValue;

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        JsonDocument _ => value,
        string s => JsonDocument.Parse(s),
        null => null,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value is JsonDocument || value is string || value == null;
}
