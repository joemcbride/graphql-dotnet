using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Validates the schema meets limitations as required by the official specification. Also looks for
/// default values within arguments and inputs fields which are stored in AST nodes
/// and coerces them to their internally represented values.
/// </summary>
public class AotValidationVisitor : BaseSchemaNodeVisitor
{
    /// <summary>
    /// Returns a static instance of the <see cref="AotValidationVisitor"/> class.
    /// </summary>
    public static AotValidationVisitor Instance { get; } = new();

    /// <inheritdoc/>
    [SuppressMessage("Trimming", "IL2065:The method has a DynamicallyAccessedMembersAttribute (which applies to the implicit 'this' parameter), but the value used for the 'this' parameter can not be statically analyzed.",
        Justification = "Property is statically referenced via Field method.")]
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        if (type is IInputObjectGraphType)
        {
            // validate that an expression has been tied to the field
            var propName = field.GetMetadata<string?>(ComplexGraphType<string>.ORIGINAL_EXPRESSION_PROPERTY_NAME);
            if (propName == null)
            {
                throw new InvalidOperationException("All fields on input objects must be defined with an expression in scenarios when dynamic compilation is not supported by the runtime. " +
                    $"Please fix {type.Name}.{field.Name}.");
            }
            var inputGraphTypeType = type.GetType();
            if (inputGraphTypeType.IsConstructedGenericType && inputGraphTypeType.GetGenericTypeDefinition() == typeof(InputObjectGraphType<>))
            {
                var sourceType = inputGraphTypeType.GetGenericArguments()[0];
                var parameter = sourceType.GetProperty(propName);
                if (parameter != null)
                {
                    var propType = parameter.PropertyType;
                    if (propType.IsInterface && typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
                    {
                        // todo:
                        // determine the list item type
                        // if it's a value type, throw an error
                        // what about dictionaries?
                    }
                }
            }
        }
        else if (type is not IInterfaceGraphType)
        {
            if (field.Resolver == null || field.Resolver is NameFieldResolver)
            {
                throw new InvalidOperationException("NameFieldResolver is not supported in scenarios when dynamic compilation is not supported by the runtime. " +
                    $"Please fix {type.Name}.{field.Name}.");
            }
        }
    }
}
