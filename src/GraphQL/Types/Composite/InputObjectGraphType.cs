using System.Reflection;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an input object graph type.
    /// </summary>
    public interface IInputObjectGraphType : IComplexGraphType
    {
        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        object ParseDictionary(IDictionary<string, object?> value);

        /// <summary>
        /// Returns a boolean indicating if the provided value is valid as a default value for a
        /// field or argument of this type.
        /// </summary>
        bool IsValidDefault(object value);

        /// <summary>
        /// Converts a value to an AST representation. This is necessary for introspection queries
        /// to return the default value for fields of this scalar type. This method may throw an exception
        /// or return <see langword="null"/> for a failed conversion.
        /// </summary>
        GraphQLValue ToAST(object value);
    }

    /// <inheritdoc/>
    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    /// <inheritdoc cref="IInputObjectGraphType"/>
    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// The default implementation uses <see cref="ObjectExtensions.ToObject"/> to convert the
        /// supplied field values into an object of type <typeparamref name="TSourceType"/>.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Field expressions are required and ensure that properties can be statically identified.")]
        public virtual object ParseDictionary(IDictionary<string, object?> value)
        {
            if (value == null)
                return null!;

            // for InputObjectGraphType just return the dictionary
            if (typeof(TSourceType) == typeof(object))
                return value;

            // for InputObjectGraphType<TSourceType>, convert to TSourceType via ToObject.
            return value.ToObject(typeof(TSourceType), this);
        }

        /// <inheritdoc/>
        public virtual bool IsValidDefault(object value)
        {
            if (value is not TSourceType)
                return false;

            foreach (var field in Fields)
            {
                if (!field.ResolvedType!.IsValidDefault(GetFieldValue(field, value)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a value to an AST representation. This is necessary for introspection queries
        /// to return the default value for fields of this input object type. Also AST representation
        /// is used while printing schema as SDL.
        /// <br/>
        /// This method may throw an exception or return <see langword="null"/> for a failed conversion.
        /// <br/><br/>
        /// The default implementation returns <see cref="GraphQLNullValue"/> if <paramref name="value"/>
        /// is <see langword="null"/> and <see cref="GraphQLObjectValue"/> filled with the values
        /// for all input fields except ones returning <see cref="GraphQLNullValue"/>.
        /// <br/><br/>
        /// Note that you may need to override this method if you have already overrided <see cref="ParseDictionary"/>.
        /// </summary>
        public virtual GraphQLValue ToAST(object? value)
        {
            if (value == null)
                return GraphQLValuesCache.Null;

            var objectValue = new GraphQLObjectValue
            {
                Fields = new List<GraphQLObjectField>(Fields.Count)
            };

            foreach (var field in Fields)
            {
                var fieldValue = field.ResolvedType!.ToAST(GetFieldValue(field, value));
                if (fieldValue is not GraphQLNullValue)
                {
                    objectValue.Fields.Add(new GraphQLObjectField(new GraphQLName(field.Name), fieldValue));
                }
            }

            return objectValue;
        }

        private static object? GetFieldValue(FieldType field, object? value)
        {
            if (value == null)
                return null;

            // Given Field(x => x.FName).Name("FirstName") and key == "FirstName" returns "FName"
            string propertyName = field.GetMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, field.Name) ?? field.Name;
            PropertyInfo? propertyInfo;
            try
            {
                propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
            catch (AmbiguousMatchException)
            {
                propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }

            return propertyInfo?.CanRead == true
                ? propertyInfo.GetValue(value)
                : null;
        }
    }
}
