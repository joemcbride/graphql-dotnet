#nullable enable

using GraphQL.Tests.Subscription;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class InputObjectGraphTypeTests
{
    [Fact]
    public void should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field()
    {
        var type = new InputObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<ObjectGraphType>("test"));

        exception.Message.ShouldContain("Input type 'InputObject' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");
    }

    [Fact]
    public void should_throw_an_exception_if_object_graph_type_contains_Input_object_graph_type_field()
    {
        var type = new ObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<InputObjectGraphType>("test"));
        exception.Message.ShouldContain("Output type 'Object' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType.");
    }

    [Fact]
    public void invalid_type_throws()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputType();
        schema.RegisterType(inputType);

        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("No public constructors found on CLR type 'MyInput'.");
    }

    [Fact]
    public void registered_value_converter_skips_validation()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputType();
        schema.RegisterType(inputType);
        ValueConverter.Register<MyInput>(_ => new MyInput2());
        try
        {
            schema.Initialize();
            inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput2>();
        }
        finally
        {
            ValueConverter.Register<MyInput>(null);
        }
    }

    [Fact]
    public void overriding_parsedictionary_ignores_type_validation()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputCustomParseDictionaryType();
        schema.RegisterType(inputType);
        schema.Initialize();
        inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput2>();
    }

    public abstract class MyInput
    {
        public string? Name { get; set; }
    }

    public class MyInput2 : MyInput
    {
    }

    public class MyInputType : InputObjectGraphType<MyInput>
    {
        public MyInputType()
        {
            Field(x => x.Name);
        }
    }

    public class MyInputCustomParseDictionaryType : InputObjectGraphType<MyInput>
    {
        public MyInputCustomParseDictionaryType()
        {
            Field(x => x.Name);
        }

        public override object ParseDictionary(IDictionary<string, object?> value) => new MyInput2();
    }

    [Fact]
    public void overriding_initialize_still_works()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInput3Type();
        schema.RegisterType(inputType);
        schema.Initialize();
        inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput3>();
    }

    public class MyInput3
    {
        public string? Name { get; set; }
    }

    public class MyInput3Type : InputObjectGraphType<MyInput3>
    {
        public MyInput3Type()
        {
            Field(x => x.Name);
        }

        public override void Initialize(ISchema schema) { }
    }

    [Fact]
    public async Task input_resolver_works()
    {
        // demonstrates having a StringGraphType field that accepts a Uri as input
        // the string value is coerced to a Uri prior to beginning execution of the request
        var inputType = new InputObjectGraphType<Class1>();
        inputType.Field<StringGraphType, Uri>("url")
            .ParseValue(original =>
            {
                var originalString = (string?)original;
                if (originalString == null)
                    return null;
                return new Uri(originalString);
            });
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>(
            "test",
            arguments: new QueryArguments(
                new QueryArgument(inputType)
                {
                    Name = "input"
                }),
            resolve: context =>
            {
                var input = context.GetArgument<Class1>("input");
                return input.Url?.ToString();
            });
        var schema = new Schema { Query = queryType };
        // check with valid url
        var result = await new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = """{ test(input: { url: "http://www.google.com" }) }""";
        });
        result.ShouldBeSimilarTo("""{"data":{"test":"http://www.google.com/"}}""");
        // check with invalid url
        result = await new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = """{ test(input: { url: "abcd" }) }""";
        });
        result.ShouldBeSimilarTo(
            """
                {"errors":[
                    {
                        "message":"Invalid literal for argument \u0027input\u0027 of field \u0027test\u0027. Invalid URI: The format of the URI could not be determined.",
                        "locations":[{"line":1,"column":15}],
                        "extensions":{
                            "code":"INVALID_LITERAL",
                            "codes":["INVALID_LITERAL","URI_FORMAT"],
                            "number":"5.6"
                        }
                    }
                ]}
            """);
    }

    private class Class1
    {
        public Uri? Url { get; set; }
    }
}
