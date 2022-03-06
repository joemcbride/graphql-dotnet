using GraphQL.Execution;
using GraphQLParser.AST;

namespace GraphQL.Tests.Language
{
    public class CommentTests
    {
        private static readonly GraphQLDocumentBuilder _builder = new() { IgnoreComments = false };

        [Fact]
        public void operation_comment_should_be_null()
        {
            const string query = @"
query _ {
    person {
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation().Comments.ShouldBeNull();
        }

        [Fact]
        public void operation_comment_should_not_be_null()
        {
            const string query = @"#comment
query _ {
    person {
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation().Comments[0].Value.ShouldBe("comment");
        }

        [Fact]
        public void field_comment_should_be_null()
        {
            const string query = @"
query _ {
    person {
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First().Comments.ShouldBeNull();
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First()
                .SelectionSet.Selections.OfType<GraphQLField>().First().Comments.ShouldBeNull();
        }

        [Fact]
        public void field_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    #comment1
    person {
        #comment2
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First().Comments[0].Value.ShouldBe("comment1");
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First()
                .SelectionSet.Selections.OfType<GraphQLField>().First().Comments[0].Value.ShouldBe("comment2");
        }

        [Fact]
        public void fragmentdefinition_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        ...human
    }
}

#comment
fragment human on person {
        name
}";

            var document = _builder.Build(query);
            document.Definitions.OfType<GraphQLFragmentDefinition>().First().Comments[0].Value.ShouldBe("comment");
        }

        [Fact]
        public void fragmentspread_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        #comment
        ...human
    }
}

fragment human on person {
        name
}";

            var document = _builder.Build(query);
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First()
                .SelectionSet.Selections.OfType<GraphQLFragmentSpread>().First().Comments[0].Value.ShouldBe("comment");
        }

        [Fact]
        public void inlinefragment_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        #comment
        ... on human {
            name
        }
    }
}";

            var document = _builder.Build(query);
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First()
                .SelectionSet.Selections.OfType<GraphQLInlineFragment>().First().Comments[0].Value.ShouldBe("comment");
        }

        [Fact]
        public void argument_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person(
        #comment
        _where: ""foo"") {
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation()
                .SelectionSet.Selections.OfType<GraphQLField>().First()
                .Arguments.First().Comments[0].Value.ShouldBe("comment");
        }

        [Fact]
        public void variable_comment_should_not_be_null()
        {
            const string query = @"
query _(
    #comment
    $id: ID) {
    person {
        name
    }
}";

            var document = _builder.Build(query);
            document.Operation()
                .Variables.First().Comments[0].Value.ShouldBe("comment");
        }
    }
}
