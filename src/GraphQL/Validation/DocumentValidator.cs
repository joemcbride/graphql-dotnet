using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    /// <summary>
    /// Validates a document against a set of validation rules and returns a list of the errors found.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <inheritdoc cref="IDocumentValidator"/>
        Task<IValidationResult> ValidateAsync(
            string originalQuery,
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null,
            IDictionary<string, object> userContext = null,
            Inputs inputs = null);
    }

    /// <inheritdoc/>
    public class DocumentValidator : IDocumentValidator
    {
        /// <summary>
        /// Returns the default set of validation rules: all except <see cref="OverlappingFieldsCanBeMerged"/>.
        /// </summary>
        public static readonly IEnumerable<IValidationRule> CoreRules = new List<IValidationRule>
        {
            UniqueOperationNames.Instance,
            LoneAnonymousOperation.Instance,
            SingleRootFieldSubscriptions.Instance,
            KnownTypeNames.Instance,
            FragmentsOnCompositeTypes.Instance,
            VariablesAreInputTypes.Instance,
            ScalarLeafs.Instance,
            FieldsOnCorrectType.Instance,
            UniqueFragmentNames.Instance,
            KnownFragmentNames.Instance,
            NoUnusedFragments.Instance,
            PossibleFragmentSpreads.Instance,
            NoFragmentCycles.Instance,
            NoUndefinedVariables.Instance,
            NoUnusedVariables.Instance,
            UniqueVariableNames.Instance,
            KnownDirectives.Instance,
            UniqueDirectivesPerLocation.Instance,
            KnownArgumentNames.Instance,
            UniqueArgumentNames.Instance,
            ArgumentsOfCorrectType.Instance,
            ProvidedNonNullArguments.Instance,
            DefaultValuesOfCorrectType.Instance,
            VariablesInAllowedPosition.Instance,
            UniqueInputFieldNames.Instance
        }.AsReadOnly();

        /// <inheritdoc/>
        public async Task<IValidationResult> ValidateAsync(
            string originalQuery,
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null,
            IDictionary<string, object> userContext = null,
            Inputs inputs = null)
        {
            if (!schema.Initialized)
            {
                schema.Initialize();
            }

            if (rules == null)
            {
                rules = CoreRules;
            }
            else if (!rules.Any())
                return SuccessfullyValidatedResult.Instance;

            var context = new ValidationContext
            {
                OriginalQuery = originalQuery ?? document.OriginalQuery,
                Schema = schema,
                Document = document,
                TypeInfo = new TypeInfo(schema),
                UserContext = userContext,
                Inputs = inputs
            };

            var awaitedVisitors = rules.Select(x => x.ValidateAsync(context)).Where(x => x != null);
            var visitors = (await Task.WhenAll(awaitedVisitors)).ToList();

            visitors.Insert(0, context.TypeInfo);

            new BasicVisitor(visitors).Visit(document, context);

            return context.HasErrors
                ? new ValidationResult(context.Errors)
                : (IValidationResult)SuccessfullyValidatedResult.Instance;
        }
    }
}
