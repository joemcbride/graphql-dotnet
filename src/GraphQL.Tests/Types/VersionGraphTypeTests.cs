using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class VersionGraphTypeTests : ScalarGraphTypeTest<VersionGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData("ooops", typeof(ArgumentException))]
    [InlineData("4.a.6", typeof(FormatException))]
    [InlineData("-100", typeof(ArgumentException))]
    public void Coerces_input_to_exception(object input, Type exceptionType)
        => AssertException(input, exceptionType);

    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("4.5.6.7", "4.5.6.7")]
    public void Coerces_input_to_valid_version(object input, string expected)
        => type.ParseValue(input).ShouldBe(new Version(expected));
}
