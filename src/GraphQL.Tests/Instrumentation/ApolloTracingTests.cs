using GraphQL.Instrumentation;
using GraphQL.Tests.DI;
using GraphQL.Tests.StarWars;

namespace GraphQL.Tests.Instrumentation
{
    public class ApolloTracingTests : StarWarsTestBase
    {
        [Theory]
        [DependencyInjectionData]
        public void extension_has_expected_format(string container)
        {
            var query = @"
query {
  hero {
    name
    friends {
      name
    }
  }
}";

            var start = DateTime.UtcNow;
            Schema.FieldMiddleware.Use(new InstrumentFieldsMiddleware());
            var result = Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
                _.EnableMetrics = true;
            }).Result;
            result.EnrichWithApolloTracing(start);
            var trace = (ApolloTrace)result.Extensions["tracing"];

            trace.Version.ShouldBe(1);
            trace.Parsing.StartOffset.ShouldNotBe(0);
            trace.Parsing.Duration.ShouldNotBe(0);
            trace.Validation.StartOffset.ShouldNotBe(0);
            trace.Validation.Duration.ShouldNotBe(0);
            trace.Validation.StartOffset.ShouldNotBeSameAs(trace.Parsing.StartOffset);
            trace.Validation.Duration.ShouldNotBeSameAs(trace.Parsing.Duration);

            var expectedPaths = new HashSet<List<object>>
            {
                new List<object> { "hero" },
                new List<object> { "hero", "name" },
                new List<object> { "hero", "friends" },
                new List<object> { "hero", "friends", 0, "name" },
                new List<object> { "hero", "friends", 1, "name" },
            };

            var paths = new List<List<object>>();
            foreach (var resolver in trace.Execution.Resolvers)
            {
                resolver.StartOffset.ShouldNotBe(0);
                resolver.Duration.ShouldNotBe(0);
                resolver.ParentType.ShouldNotBeNull();
                resolver.ReturnType.ShouldNotBeNull();
                resolver.FieldName.ShouldBe((string)resolver.Path.Last());
                paths.Add(resolver.Path);
            }
            paths.Count.ShouldBe(expectedPaths.Count);
            new HashSet<List<object>>(paths).ShouldBe(expectedPaths);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void serialization_should_have_correct_case(IGraphQLTextSerializer writer)
        {
            var trace = new ApolloTrace(new DateTime(2019, 12, 05, 15, 38, 00, DateTimeKind.Utc), 102.5);
            var expected = @"{
  ""version"": 1,
  ""startTime"": ""2019-12-05T15:38:00Z"",
  ""endTime"": ""2019-12-05T15:38:00.103Z"",
  ""duration"": 102500000,
  ""parsing"": {
    ""startOffset"": 0,
    ""duration"": 0
  },
  ""validation"": {
    ""startOffset"": 0,
    ""duration"": 0
  },
  ""execution"": {
    ""resolvers"": []
  }
}";

            var result = writer.Serialize(trace);

            result.ShouldBeCrossPlat(expected);
        }
    }
}
