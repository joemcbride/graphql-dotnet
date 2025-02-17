using System.Text.Json;
using GraphQL.SystemTextJson;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class JsonGraphTypeTests : ScalarGraphTypeTest<JsonGraphType>
{
    [Fact]
    public void Can_parse_value()
    {
        type.CanParseValue(null).ShouldBeTrue();
        type.CanParseValue("{}").ShouldBeTrue();
        type.CanParseValue(JsonDocument.Parse("{}")).ShouldBeTrue();
        type.CanParseValue(42).ShouldBeFalse();
        type.CanParseValue(true).ShouldBeFalse();
    }

    [Fact]
    public void Can_parse_literal()
    {
        type.CanParseLiteral(null).ShouldBeFalse();
        type.CanParseLiteral(new GraphQLStringValue("{}")).ShouldBeTrue();
        type.CanParseLiteral(new GraphQLNullValue()).ShouldBeTrue();
        type.CanParseValue(new GraphQLIntValue(42)).ShouldBeFalse();
        type.CanParseValue(new GraphQLTrueBooleanValue()).ShouldBeFalse();
    }

    [Theory]
    [InlineData(123.0)]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Fact]
    public void Coerces_input_to_valid_json()
    {
        string json = "{\"name\": \"Tom\"}";
        var document = JsonDocument.Parse(json);

        var parsed = type.ParseValue(json).ShouldBeOfType<JsonDocument>();
        parsed.RootElement.GetProperty("name").GetString().ShouldBe("Tom");

        type.ParseValue(document).ShouldBe(document);
        type.ParseValue(null).ShouldBe(null);
    }
}
