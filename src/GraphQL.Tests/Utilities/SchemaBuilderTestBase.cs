using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GraphQL.Utilities;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;

namespace GraphQL.Tests.Utilities
{
    public class SchemaBuilderTestBase
    {
        public SchemaBuilderTestBase()
        {
            Builder = new SchemaBuilder();
        }

        protected readonly IDocumentExecuter Executer = new DocumentExecuter();
        protected SchemaBuilder Builder { get; set; }

        public ExecutionResult AssertQuery(IDocumentWriter writer, Action<ExecuteConfig> configure)
        {
            var config = new ExecuteConfig();
            configure(config);

            var schema = Builder.Build(config.Definitions);
            schema.Initialize();

            var queryResult = CreateQueryResult(config.ExpectedResult);

            return AssertQuery(
                writer,
                _ =>
                {
                    _.Schema = schema;
                    _.Query = config.Query;
                    _.Inputs = SystemTextJson.StringExtensions.ToInputs(config.Variables);
                    _.Root = config.Root;
                    _.ThrowOnUnhandledException = config.ThrowOnUnhandledException;
                },
                queryResult);
        }

        public ExecutionResult AssertQuery(IDocumentWriter writer, Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
        {
            var runResult = Executer.ExecuteAsync(options).Result;

            var writtenResult = writer.WriteToStringAsync(runResult).Result;
            var expectedResult = writer.WriteToStringAsync(expectedExecutionResult).Result;

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);

            return runResult;
        }

        public ExecutionResult CreateQueryResult(string result)
        {
            object expected = null;
            if (!string.IsNullOrWhiteSpace(result))
            {
                expected = JObject.Parse(result);
            }
            return new ExecutionResult { Data = expected };
        }

        protected string ReadSchema(string fileName)
        {
            return File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files", fileName));
        }
    }

    public class ExecuteConfig
    {
        public string Definitions { get; set; }
        public string Query { get; set; }
        public string Variables { get; set; }
        public string ExpectedResult { get; set; }
        public object Root { get; set; }
        public bool ThrowOnUnhandledException { get; set; }
    }
}
