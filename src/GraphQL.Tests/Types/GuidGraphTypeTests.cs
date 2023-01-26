using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class GuidGraphTypeTests : ScalarGraphTypeTest<GuidGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData("936DA01F9ABD4d9d80C702AF85C822A")]
    [InlineData("936DA01F-9ABD-4d9d-80C7-02AF85C822A")]
    [InlineData("936DA01F")]
    public void Coerces_input_to_format_exception(object input)
        => AssertException<FormatException>(input);

    [Theory]
    [InlineData("936DA01F9ABD4d9d80C702AF85C822A8", "936DA01F-9ABD-4d9d-80C7-02AF85C822A8")]
    [InlineData("936DA01F-9ABD-4d9d-80C7-02AF85C822A8", "936DA01F-9ABD-4d9d-80C7-02AF85C822A8")]
    public void Coerces_input_to_valid_guid(object input, string expected)
        => type.ParseValue(input).ShouldBe(new Guid(expected));
}
