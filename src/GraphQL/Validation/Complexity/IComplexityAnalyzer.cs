using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    public interface IComplexityAnalyzer
    {
        /// <exception cref="InvalidOperationException">
        /// Thrown if complexity is not within the defined range in parameters.
        /// </exception>
        void Validate(Document document, ComplexityConfiguration parameters);
    }
}
