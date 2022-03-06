using GraphQL.Tests.DI;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQLParser;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsFragmentTests : StarWarsTestBase
    {
        [Theory]
        [DependencyInjectionData]
        public void use_fragment_spread_to_avoid_duplicate_content(string container)
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ...DroidFragment
                  }

                  c3po: droid(id: ""4"") {
                    ...DroidFragment
                  }
               }
               fragment DroidFragment on Droid {
                 name
               }
            ";

            var expected = @"{
              ""r2d2"": {
                ""name"": ""R2-D2""
              },
              ""c3po"": {
                ""name"": ""C-3PO""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void use_inline_fragment_on_interface(string container)
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ... on Character {
                      name
                    }
                  }
               }
            ";

            var expected = @"{
              ""r2d2"": {
                ""name"": ""R2-D2""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void use_unnamed_inline_fragment_on_interface(string container)
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ... {
                      name
                    }
                  }
               }
            ";

            var expected = @"{
              ""r2d2"": {
                ""name"": ""R2-D2""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Theory]
        [DependencyInjectionData]
        public void use_undefined_fragment(string container)
        {
            var query = @"
                query someDroids {
                    r2d2: droid(id: ""3"") {
                        ...unknown_fragment
                        name
                    }
               }
            ";
            var errors = new ExecutionErrors();
            var error = new ValidationError(query, KnownFragmentNamesError.NUMBER, "Unknown fragment 'unknown_fragment'.")
            {
                Code = "KNOWN_FRAGMENT_NAMES"
            };
            error.AddLocation(new Location(4, 25));
            errors.Add(error);

            AssertQuery(query, CreateQueryResult(null, errors, executed: false), null, null);
        }
    }
}
