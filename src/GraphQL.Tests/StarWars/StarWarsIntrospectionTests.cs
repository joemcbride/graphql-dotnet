using GraphQL.Tests.DI;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsIntrospectionTests : StarWarsTestBase
    {
        [Theory]
        [DependencyInjectionData]
        public void provides_typename(string container)
        {
            var query = "{ hero { __typename name } }";

            var expected = @"{ ""hero"": { ""__typename"": ""Droid"", ""name"": ""R2-D2"" } }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_schema_for_an_object_kind(string container)
        {
            var query = @"
                query IntrospectionDroidKindQuery {
                  __type(name: ""Droid"") {
                    name,
                    kind
                  }
                }
            ";

            var expected = @"{
              ""__type"": {
                ""name"": ""Droid"",
                ""kind"": ""OBJECT""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_schema_for_an_interface_kind(string container)
        {
            var query = @"
            query IntrospectionCharacterKindQuery {
              __type(name: ""Character"") {
                name
                kind
              }
            }
            ";

            var expected = @"{
              ""__type"": {
                ""name"": ""Character"",
                ""kind"": ""INTERFACE""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_schema_for_possibleTypes_of_an_interface(string container)
        {
            var query = @"
            query IntrospectionCharacterKindQuery {
              __type(name: ""Character"") {
                name
                kind
                  possibleTypes {
                    name,
                    kind
                  }
              }
            }
            ";

            var expected = @"{
              ""__type"": {
                ""name"": ""Character"",
                ""kind"": ""INTERFACE"",
                ""possibleTypes"": [
                  { ""name"": ""Human"", ""kind"": ""OBJECT"" },
                  { ""name"": ""Droid"", ""kind"": ""OBJECT"" }
                ]
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_the_schema_for_object_fields(string container)
        {
            var query = @"
            query IntrospectionDroidFieldsQuery {
              __type(name: ""Droid"") {
                name
                fields {
                    name
                    type {
                        name
                        kind
                    }
                }
              }
            }
            ";

            var expected = @"{
                ""__type"": {
                  ""name"": ""Droid"",
                  ""fields"": [
                    {
                      ""name"": ""id"",
                      ""type"": {
                        ""name"": null,
                        ""kind"": ""NON_NULL""
                      }
                    },
                    {
                      ""name"": ""name"",
                      ""type"": {
                        ""name"": ""String"",
                        ""kind"": ""SCALAR""
                      }
                    },
                    {
                      ""name"": ""friends"",
                      ""type"": {
                        ""name"": null,
                        ""kind"": ""LIST""
                      }
                    },
                    {
                      ""name"": ""friendsConnection"",
                      ""type"": {
                        ""name"": ""CharacterInterfaceConnection"",
                        ""kind"": ""OBJECT""
                      }
                    },
                    {
                      ""name"": ""appearsIn"",
                      ""type"": {
                        ""name"": null,
                        ""kind"": ""LIST""
                      }
                    },
                    {
                      ""name"": ""primaryFunction"",
                      ""type"": {
                        ""name"": ""String"",
                        ""kind"": ""SCALAR""
                      }
                    }
                  ]
                }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_the_schema_for_documentation(string container)
        {
            var query = @"
            query IntrospectionDroidDescriptionQuery {
              __type(name: ""Droid"") {
                name
                description
              }
            }
            ";
            var expected = @"{
              ""__type"": {
                ""name"": ""Droid"",
                ""description"": ""A mechanical creature in the Star Wars universe.""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_the_schema(string container)
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                types { name, kind }
                queryType { name, kind }
                mutationType { name }
                directives {
                  name
                  description
                  onOperation
                  onFragment
                  onField
                }
              }
            }
            ";
            var expected = @"{
                ""__schema"": {
                    ""types"": [
                    {
                        ""name"": ""__DirectiveLocation"",
                        ""kind"": ""ENUM""
                    },
                    {
                        ""name"": ""__TypeKind"",
                        ""kind"": ""ENUM""
                    },
                    {
                        ""name"": ""__EnumValue"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""String"",
                        ""kind"": ""SCALAR""
                    },
                    {
                        ""name"": ""Boolean"",
                        ""kind"": ""SCALAR""
                    },
                    {
                        ""name"": ""__Directive"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""__InputValue"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""__Type"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""__Field"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""__Schema"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Query"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Character"",
                        ""kind"": ""INTERFACE""
                    },
                    {
                        ""name"": ""CharacterInterfaceConnection"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Int"",
                        ""kind"": ""SCALAR""
                    },
                    {
                        ""name"": ""PageInfo"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""CharacterInterfaceEdge"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Episode"",
                        ""kind"": ""ENUM""
                    },
                    {
                        ""name"": ""Human"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Droid"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""Mutation"",
                        ""kind"": ""OBJECT""
                    },
                    {
                        ""name"": ""HumanInput"",
                        ""kind"": ""INPUT_OBJECT""
                    }
                    ],
                    ""queryType"": {
                      ""name"": ""Query"",
                      ""kind"": ""OBJECT""
                    },
                    ""mutationType"": {
                      ""name"": ""Mutation""
                    },
                    ""directives"": [
                    {
                        ""name"": ""include"",
                        ""description"": ""Directs the executor to include this field or fragment only when the 'if' argument is true."",
                        ""onOperation"": false,
                        ""onFragment"": true,
                        ""onField"": true
                    },
                    {
                        ""name"": ""skip"",
                        ""description"": ""Directs the executor to skip this field or fragment when the 'if' argument is true."",
                        ""onOperation"": false,
                        ""onFragment"": true,
                        ""onField"": true
                    },
                    {
                        ""name"": ""deprecated"",
                        ""description"": ""Marks an element of a GraphQL schema as no longer supported."",
                        ""onOperation"": false,
                        ""onFragment"": false,
                        ""onField"": false
                    }
                    ]
                }
            }";

            AssertQuerySuccess(query, expected);
        }

        // https://github.com/graphql-dotnet/graphql-dotnet/issues/2233
        [Theory]
        [DependencyInjectionData]
        public void allow_querying_input_object_type_fields(string container)
        {
            var query = @"{
  __type(name: ""HumanInput"") {
    fields { name }
    inputFields { name }
  }
}
            ";

            AssertQuerySuccess(query, "HumanInputIntrospectionResult".ReadJsonResult());
        }

        [Theory]
        [DependencyInjectionData]
        public void allows_querying_field_args(string container)
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                queryType {
                  fields {
                    name
                    args {
                      name
                      description
                      type {
                        name
                        kind
                        ofType {
                          name
                          kind
                        }
                      }
                      defaultValue
                    }
                  }
                }
              }
            }
            ";
            var expected = @"{
              ""__schema"": {
                ""queryType"": {
                  ""fields"": [
                    {
                      ""name"": ""hero"",
                      ""args"": []
                    },
                    {
                      ""name"": ""human"",
                      ""args"": [
                        {
                          ""name"": ""id"",
                          ""description"": ""id of the human"",
                          ""type"": {
                            ""name"": null,
                            ""kind"": ""NON_NULL"",
                            ""ofType"": {
                              ""name"": ""String"",
                              ""kind"": ""SCALAR""
                            }
                          },
                          ""defaultValue"": null
                        }
                      ]
                    },
                    {
                      ""name"": ""droid"",
                      ""args"": [
                        {
                          ""name"": ""id"",
                          ""description"": ""id of the droid"",
                          ""type"": {
                            ""name"": null,
                            ""kind"": ""NON_NULL"",
                            ""ofType"": {
                              ""name"": ""String"",
                              ""kind"": ""SCALAR""
                            }
                          },
                          ""defaultValue"": null
                        }
                      ]
                    }
                  ]
                }
              }
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
