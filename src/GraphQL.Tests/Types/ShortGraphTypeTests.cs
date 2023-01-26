using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class ShortGraphTypeTests : ScalarGraphTypeTest<ShortGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData(-999999)]
    [InlineData(999999)]
    public void Coerces_given_inputs_to_overflow_exception(object input) =>
        AssertException<OverflowException>(input);

    [Theory]
    [InlineData("999999")]
    [InlineData("-999999")]
    [InlineData("abc")]
    [InlineData("-999999.99")]
    [InlineData("32767")]
    [InlineData("-32768")]
    [InlineData("0")]
    [InlineData(125.0)]
    [InlineData(999999.99)]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Theory]
    [InlineData(32767, 32767)]
    [InlineData(-32768, -32768)]
    [InlineData(12, 12)]
    public void Coerces_given_inputs_to_short_value(object input, short expected)
        => type.ParseValue(input).ShouldBe(expected);
}
