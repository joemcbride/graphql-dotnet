using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class ScalarGraphTypeTest<T> where T : ScalarGraphType, new()
{
    protected readonly T type = new();

    protected void AssertException<TArg>(object value) where TArg : Exception =>
        Should.Throw<TArg>(() => type.ParseValue(value));

    protected void AssertException(object value, Type exceptionType) =>
       Should.Throw(() => type.ParseValue(value), exceptionType);
}
