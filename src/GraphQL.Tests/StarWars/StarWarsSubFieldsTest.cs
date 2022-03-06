using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Tests.DI;
using GraphQL.Types;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsSubFieldsTests : StarWarsTestBase
    {
        public StarWarsQuery RootQuery => (StarWarsQuery)Schema.Query;

        [Theory]
        [DependencyInjectionData]
        public void subfields_is_not_null_for_ListGraphType_of_ObjectGraphType(string container)
        {
            RootQuery.Field<ListGraphType<HumanType>>("listOfHumans", resolve: ctx =>
            {
                ctx.SubFields.ShouldNotBeNull();
                ctx.SubFields.Keys.ShouldContain("id");
                ctx.SubFields.Keys.ShouldContain("friends");
                return new List<Human>();
            });
            var query = @"
                {
                    listOfHumans {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    ""listOfHumans"": []
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_is_not_null_for_single_ObjectGraphType(string container)
        {
            RootQuery.Field<HumanType>("singleHuman", resolve: ctx =>
            {
                ctx.SubFields.ShouldNotBeNull();
                ctx.SubFields.Keys.ShouldContain("id");
                ctx.SubFields.Keys.ShouldContain("friends");
                return null;
            });

            var query = @"
                {
                    singleHuman {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";
            var expected = @"
                {
                    ""singleHuman"": null
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_is_not_null_for_ListGraphType_of_InterfaceGraphType(string container)
        {
            RootQuery.Field<ListGraphType<CharacterInterface>>("listOfCharacters", resolve: ctx =>
            {
                ctx.SubFields.ShouldNotBeNull();
                ctx.SubFields.Keys.ShouldContain("id");
                ctx.SubFields.Keys.ShouldContain("friends");
                return new List<Human>();
            });
            var query = @"
                {
                    listOfCharacters {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    ""listOfCharacters"": []
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_is_not_null_for_single_InterfaceGraphType(string container)
        {
            RootQuery.FieldAsync<CharacterInterface>("singleCharacter", resolve: ctx =>
           {
               ctx.SubFields.ShouldNotBeNull();
               ctx.SubFields.Keys.ShouldContain("id");
               ctx.SubFields.Keys.ShouldContain("friends");
               return Task.FromResult<object>(null);
           });
            var query = @"
                {
                    singleCharacter {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    ""singleCharacter"": null
                }
            ";
            AssertQuerySuccess(query, expected);
        }


        [Theory]
        [DependencyInjectionData]
        public void subfields_does_not_throw_for_primitive(string container)
        {
            RootQuery.Field<IntGraphType>("someNumber", resolve: ctx =>
            {
                ctx.SubFields.ShouldBeNull();
                return 1;
            });

            var query = @"
                {
                    someNumber
                }
            ";
            var expected = @"
                {
                    ""someNumber"": 1
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_does_not_throw_for_list_of_primitive(string container)
        {
            RootQuery.Field<ListGraphType<IntGraphType>>("someNumbers", resolve: ctx =>
            {
                ctx.SubFields.ShouldBeNull();
                return new[] { 1, 2 };
            });

            var query = @"
                {
                    someNumbers
                }
            ";
            var expected = @"
                {
                    ""someNumbers"": [1,2]
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_contains_keys_from_fragment_spread_on_non_null_fields(string container)
        {
            RootQuery.Field<NonNullGraphType<HumanType>>("luke", resolve: context =>
            {
                context.SubFields.ShouldNotBeNull();
                context.SubFields.Keys.ShouldContain("id");
                context.SubFields.Keys.ShouldContain("name");
                return new Human { Id = "1", Name = "Luke" };
            });

            var query = @"
                query Luke {
                    luke {
                        ...HumanData
                    }
                }

                fragment HumanData on Human {
                    id
                    name
                }
            ";

            var expected = @"
                {
                    ""luke"": {
                        ""id"": ""1"",
                        ""name"": ""Luke""
                    }
                }
            ";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_contains_keys_from_inline_fragment_on_non_null_fields(string container)
        {
            RootQuery.Field<NonNullGraphType<HumanType>>("luke", resolve: context =>
            {
                context.SubFields.ShouldNotBeNull();
                context.SubFields.Keys.ShouldContain("id");
                context.SubFields.Keys.ShouldContain("name");
                return new Human { Id = "1", Name = "Luke" };
            });

            var query = @"
                query Luke {
                    luke {
                        ...on Human
                        {
                            id
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    ""luke"": {
                        ""id"": ""1"",
                        ""name"": ""Luke""
                    }
                }
            ";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_contains_keys_from_fragment_spread_on_list_fields(string container)
        {
            RootQuery.Field<ListGraphType<HumanType>>("lukes", resolve: context =>
            {
                context.SubFields.ShouldNotBeNull();
                context.SubFields.Keys.ShouldContain("id");
                context.SubFields.Keys.ShouldContain("name");
                return new[] { new Human { Id = "1", Name = "Luke" }, new Human { Id = "2", Name = "Luke Copy" } };
            });

            var query = @"
                query Luke {
                    lukes {
                        ...HumanData
                    }
                }

                fragment HumanData on Human {
                    id
                    name
                }
            ";

            var expected = @"
                {
                    ""lukes"": [
                    {
                        ""id"": ""1"",
                        ""name"": ""Luke""
                    },
                    {
                        ""id"": ""2"",
                        ""name"": ""Luke Copy""
                    }
                ]}
            ";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void subfields_contains_keys_from_inline_fragment_on_list_fields(string container)
        {
            RootQuery.Field<ListGraphType<HumanType>>("lukes", resolve: context =>
            {
                context.SubFields.ShouldNotBeNull();
                context.SubFields.Keys.ShouldContain("id");
                context.SubFields.Keys.ShouldContain("name");
                return new[] { new Human { Id = "1", Name = "Luke" }, new Human { Id = "2", Name = "Luke Copy" } };
            });

            var query = @"
                query Luke {
                    lukes {
                        ... on Human
                        {
                            id
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    ""lukes"": [
                    {
                        ""id"": ""1"",
                        ""name"": ""Luke""
                    },
                    {
                        ""id"": ""2"",
                        ""name"": ""Luke Copy""
                    }
                ]}
            ";

            AssertQuerySuccess(query, expected);
        }
    }
}
