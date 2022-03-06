using GraphQL.Tests.DI;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types
{
    // these tests relate to the code in custom-scalars.md
    public class Vector3ScalarTests : QueryTestBase<Vector3ScalarTests.Vector3ScalarSchema>
    {
        [Theory]
        [DependencyInjectionData]
        public void test_parseliteral_string(string container)
        {
            AssertQuerySuccess(@"{ input(arg: ""1,2,3"") }", @"{ ""input"": ""=1=2=3="" }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_parseliteral_structured(string container)
        {
            AssertQuerySuccess(@"{ input(arg: {x:1,y:2,z:3}) }", @"{ ""input"": ""=1=2=3="" }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_parsevalue_string(string container)
        {
            AssertQuerySuccess(@"query($arg: Vector3) { input(arg: $arg) }", @"{ ""input"": ""=1=2=3="" }", @"{ ""arg"": ""1,2,3"" }".ToInputs());
        }

        [Theory]
        [DependencyInjectionData]
        public void test_parsevalue_structured(string container)
        {
            AssertQuerySuccess(@"query($arg: Vector3) { input(arg: $arg) }", @"{ ""input"": ""=1=2=3="" }", @"{ ""arg"": { ""x"": 1, ""y"": 2, ""z"": 3 } }".ToInputs());
        }

        [Theory]
        [DependencyInjectionData]
        public void test_default(string container)
        {
            AssertQuerySuccess(@"{ input }", @"{ ""input"": ""=7=8=9="" }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_output(string container)
        {
            AssertQuerySuccess(@"{ output }", @"{ ""output"": { ""x"": 4, ""y"": 5, ""z"": 6 } }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_loopback_with_value(string container)
        {
            AssertQuerySuccess(@"{ loopback(arg: ""11,12,13"") }", @"{ ""loopback"": { ""x"": 11, ""y"": 12, ""z"": 13 } }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_loopback_with_null(string container)
        {
            AssertQuerySuccess(@"{ loopback }", @"{ ""loopback"": null }");
        }

        [Theory]
        [DependencyInjectionData]
        public void test_parseLiteral_toAst(string container)
        {
            var scalar = new Vector3Type();
            var input = new Vector3(1, 2, 3);
            var ast = scalar.ToAST(input);
            var value = scalar.ParseLiteral(ast);
            var output = value.ShouldBeOfType<Vector3>();
            output.X.ShouldBe(input.X);
            output.Y.ShouldBe(input.Y);
            output.Z.ShouldBe(input.Z);
        }

        public struct Vector3
        {
            public Vector3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public override string ToString()
            {
                return $"={X}={Y}={Z}=";
            }
        }

        public class Vector3ScalarSchema : Schema
        {
            public Vector3ScalarSchema()
            {
                Query = new Vector3ScalarQuery();
            }
        }

        public class Vector3ScalarQuery : ObjectGraphType
        {
            public Vector3ScalarQuery()
            {
                Field(typeof(StringGraphType), "input",
                    arguments: new QueryArguments
                    {
                        new QueryArgument<Vector3Type> { Name = "arg", DefaultValue = new Vector3(7, 8, 9) }
                    },
                    resolve: context => context.GetArgument<Vector3?>("arg")?.ToString());

                Field(typeof(Vector3Type), "output",
                    resolve: context => new Vector3(4, 5, 6));

                Field(typeof(Vector3Type), "loopback",
                    arguments: new QueryArguments
                    {
                        new QueryArgument<Vector3Type> { Name = "arg" }
                    },
                    resolve: context => context.GetArgument<Vector3?>("arg"));
            }
        }

        public class Vector3Type : ScalarGraphType
        {
            private readonly FloatGraphType _floatScalar = new FloatGraphType();

            public Vector3Type()
            {
                Name = "Vector3";
            }

            public override object ParseValue(object value)
            {
                if (value == null)
                    return null;

                if (value is string vector3InputString)
                {
                    try
                    {
                        var vector3Parts = vector3InputString.Split(','); // strings allocations
                        var x = float.Parse(vector3Parts[0]);
                        var y = float.Parse(vector3Parts[1]);
                        var z = float.Parse(vector3Parts[2]);
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse {nameof(Vector3)} from input '{vector3InputString}'. Input should be a string of three comma-separated floats in X Y Z order, ex. 1.0,2.0,3.0");
                    }
                }

                if (value is ROM vector3InputROM)
                {
                    try
                    {
                        // no strings allocations
                        var span = vector3InputROM.Span;

                        var i = span.IndexOf(',');
                        var x = float.Parse(span.Slice(0, i).ToString()); // string conversion for NET48

                        span = span.Slice(i + 1);

                        i = span.IndexOf(',');
                        var y = float.Parse(span.Slice(0, i).ToString()); // string conversion for NET48

                        span = span.Slice(i + 1);

                        var z = float.Parse(span.Slice(0, i).ToString()); // string conversion for NET48
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse {nameof(Vector3)} from input '{vector3InputROM}'. Input should be a string of three comma-separated floats in X Y Z order, ex. 1.0,2.0,3.0");
                    }
                }

                if (value is IDictionary<string, object> dictionary)
                {
                    try
                    {
                        var x = Convert.ToSingle(dictionary["x"]);
                        var y = Convert.ToSingle(dictionary["y"]);
                        var z = Convert.ToSingle(dictionary["z"]);
                        if (dictionary.Count > 3)
                            return ThrowValueConversionError(value);
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse {nameof(Vector3)} from object. Input should be an object of three floats named X Y and Z");
                    }
                }

                return ThrowValueConversionError(value);
            }

            public override object ParseLiteral(GraphQLValue value)
            {
                if (value is GraphQLNullValue)
                    return null;

                if (value is GraphQLStringValue stringValue)
                    return ParseValue(stringValue.Value);

                if (value is GraphQLObjectValue objectValue)
                {
                    var entries = objectValue.Fields.ToDictionary(x => x.Name.Value, x => _floatScalar.ParseLiteral(x.Value));
                    if (entries.Count != 3)
                        return ThrowLiteralConversionError(value);
                    var x = (double)entries["x"];
                    var y = (double)entries["y"];
                    var z = (double)entries["z"];
                    return new Vector3((float)x, (float)y, (float)z);
                }

                return ThrowLiteralConversionError(value);
            }

            public override object Serialize(object value)
            {
                if (value == null)
                    return null;

                if (value is Vector3 vector3)
                {
                    return new
                    {
                        x = vector3.X,
                        y = vector3.Y,
                        z = vector3.Z
                    };
                }

                return ThrowSerializationError(value);
            }

            public override GraphQLValue ToAST(object value)
            {
                if (value == null)
                    return new GraphQLNullValue();

                if (value is Vector3 vector3)
                {
                    return new GraphQLObjectValue
                    {
                        Fields = new List<GraphQLObjectField>
                        {
                            new GraphQLObjectField
                            {
                                Name = new GraphQLName("x"),
                                Value = new GraphQLFloatValue(vector3.X.ToString())
                            },
                            new GraphQLObjectField
                            {
                                Name = new GraphQLName("y"),
                                Value = new GraphQLFloatValue(vector3.Y.ToString())
                            },
                            new GraphQLObjectField
                            {
                                Name = new GraphQLName("z"),
                                Value = new GraphQLFloatValue(vector3.Z.ToString())
                            }
                        }
                    };
                }

                return ThrowASTConversionError(value);
            }
        }
    }
}
