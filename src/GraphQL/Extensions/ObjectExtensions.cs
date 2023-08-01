using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for objects and a method for converting a dictionary into a strongly typed object.
    /// </summary>
    public static class ObjectExtensions
    {
        private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> _types = new();

        /// <summary>
        /// Creates a new instance of the indicated type, populating it with the dictionary.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <param name="source">The source of values.</param>
        /// <returns>T.</returns>
        [RequiresUnreferencedCode("The type of the object to be converted to must be statically discoverable.")]
        public static T ToObject<T>(this IDictionary<string, object?> source)
            where T : class
            => (T)ToObject(source, typeof(T));

        private static readonly List<object?> _emptyValues = new();

        /// <summary>
        /// Creates a new instance of the indicated type, populating it with the dictionary.
        /// Can use any constructor of the indicated type, provided that there are keys in the
        /// dictionary that correspond (case sensitive) to the names of the constructor parameters.
        /// </summary>
        /// <param name="source">The source of values.</param>
        /// <param name="type">The type to create.</param>
        /// <param name="mappedType">
        /// GraphType for matching dictionary keys with <paramref name="type"/> property names.
        /// GraphType contains information about this matching in Metadata property.
        /// In case of configuring field as Field(x => x.FName).Name("FirstName") source dictionary
        /// will have 'FirstName' key but its value should be set to 'FName' property of created object.
        /// </param>
        [RequiresUnreferencedCode("The type of the object to be converted to must be statically discoverable.")]
        public static object ToObject(this IDictionary<string, object?> source, Type type, IGraphType? mappedType = null)
        {
            // Given Field(x => x.FName).Name("FirstName") and key == "FirstName" returns "FName"
            string GetPropertyName(string key, out FieldType? field)
            {
                var complexType = mappedType?.GetNamedType() as IComplexGraphType;

                // type may not contain mapping information
                field = complexType?.GetField(key);
                return field?.GetMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, key) ?? key;
            }

            // Returns values (from source or defaults) that match constructor signature + used keys from source
            (List<object?>?, List<string>?) GetValuesAndUsedKeys(ParameterInfo[] parameters)
            {
                // parameterless constructors are the most common use case
                if (parameters.Length == 0)
                    return (_emptyValues, null);

                // otherwise we have to iterate over the parameters - worse performance but this is rather rare case
                List<object?>? values = null;
                List<string>? keys = null;

                if (parameters.All(p =>
                {
                    // Source values take precedence
                    if (source.Any(keyValue =>
                    {
                        bool matched = string.Equals(GetPropertyName(keyValue.Key, out var _), p.Name, StringComparison.InvariantCultureIgnoreCase);
                        if (matched)
                        {
                            (values ??= new()).Add(keyValue.Value);
                            (keys ??= new()).Add(keyValue.Key);
                        }
                        return matched;
                    }))
                    {
                        return true;
                    }

                    // Then check for default values if any
                    if (p.HasDefaultValue)
                    {
                        (values ??= new()).Add(p.DefaultValue);
                        return true;
                    }

                    return false;
                }))
                {
                    return (values, keys);
                }

                return (null, null);
            }

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // if conversion from IDictionary<string, object> to desired type is registered then use it
            if (ValueConverter.TryConvertTo(source, type, out object? result, typeof(IDictionary<string, object>)))
                return result!;

            if (type.IsAbstract)
                throw new InvalidOperationException($"Type '{type}' is abstract and can not be used to construct objects from dictionary values. Please register a conversion within the ValueConverter or for input graph types override ParseDictionary method.");

            // attempt to use the most specific constructor sorting in decreasing order of parameters number
            var ctorCandidates = _types.GetOrAdd(type, t => t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderByDescending(ctor => ctor.GetParameters().Length).ToArray());

            ConstructorInfo? targetCtor = null;
            ParameterInfo[]? ctorParameters = null;
            List<object?>? values = null;
            List<string>? usedKeys = null;

            foreach (var ctor in ctorCandidates)
            {
                var parameters = ctor.GetParameters();
                (values, usedKeys) = GetValuesAndUsedKeys(parameters);
                if (values != null)
                {
                    targetCtor = ctor;
                    ctorParameters = parameters;
                    break;
                }
            }

            if (targetCtor == null || ctorParameters == null || values == null)
                throw new ArgumentException($"Type '{type}' does not contain a constructor that could be used for current input arguments.", nameof(type));

            object?[] ctorArguments = ctorParameters.Length == 0 ? Array.Empty<object>() : new object[ctorParameters.Length];

            for (int i = 0; i < ctorParameters.Length; ++i)
            {
                object? arg = GetPropertyValue(values[i], ctorParameters[i].ParameterType);
                ctorArguments[i] = arg;
            }

            object obj;
            try
            {
                obj = targetCtor.Invoke(ctorArguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return ""; // never executed, necessary only for intellisense
            }

            foreach (var item in source)
            {
                // these parameters have already been used in the constructor, no need to set property
                if (usedKeys?.Any(k => k == item.Key) == true)
                    continue;

                string propertyName = GetPropertyName(item.Key, out var field);
                PropertyInfo? propertyInfo = null;

                try
                {
                    propertyInfo = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                catch (AmbiguousMatchException)
                {
                    propertyInfo = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    object? value = GetPropertyValue(item.Value, propertyInfo.PropertyType, field?.ResolvedType);
                    propertyInfo.SetValue(obj, value, null); //issue: this works even if propertyInfo is ValueType and value is null
                }
                else
                {
                    FieldInfo? fieldInfo;

                    try
                    {
                        fieldInfo = type.GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    }
                    catch (AmbiguousMatchException)
                    {
                        fieldInfo = type.GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    }

                    if (fieldInfo != null)
                    {
                        object? value = GetPropertyValue(item.Value, fieldInfo.FieldType, field?.ResolvedType);
                        fieldInfo.SetValue(obj, value);
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Converts the indicated value into a type that is compatible with fieldType.
        /// </summary>
        /// <param name="propertyValue">The value to be converted.</param>
        /// <param name="fieldType">The desired type.</param>
        /// <param name="mappedType">
        /// GraphType for matching dictionary keys with <paramref name="fieldType"/> property names.
        /// GraphType contains information about this matching in Metadata property.
        /// In case of configuring field as Field(x => x.FName).Name("FirstName") source dictionary
        /// will have 'FirstName' key but its value should be set to 'FName' property of created object.
        /// </param>
        /// <remarks>There is special handling for strings, IEnumerable&lt;T&gt;, Nullable&lt;T&gt;, and Enum.</remarks>
        [RequiresUnreferencedCode("The type of the object to be converted to must be statically discoverable.")]
        public static object? GetPropertyValue(this object? propertyValue, Type fieldType, IGraphType? mappedType = null)
        {
            // Short-circuit conversion if the property value already of the right type
            if (propertyValue == null || fieldType == typeof(object) || fieldType.IsInstanceOfType(propertyValue))
            {
                return propertyValue;
            }

            if (ValueConverter.TryConvertTo(propertyValue, fieldType, out object? result))
                return result;

            var enumerableInterface = fieldType.Name == "IEnumerable`1"
              ? fieldType
              : fieldType.GetInterface("IEnumerable`1");

            if (fieldType != typeof(string) && enumerableInterface != null)
            {
                IList newCollection;
                var elementType = enumerableInterface.GetGenericArguments()[0];
                var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                var fieldTypeImplementsIList = fieldType.GetInterface("IList") != null;

                var propertyValueAsIList = propertyValue as IList;

                // Custom container
                if (fieldTypeImplementsIList && !fieldType.IsArray)
                {
                    newCollection = (IList)Activator.CreateInstance(fieldType)!;
                }
                // Array of known size is created immediately
                else if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    newCollection = Array.CreateInstance(elementType, propertyValueAsIList.Count);
                }
                // List<T>
                else
                {
                    var genericListType = typeof(List<>).MakeGenericType(elementType);
                    newCollection = (IList)Activator.CreateInstance(genericListType)!;
                }

                if (propertyValue is not IEnumerable valueList)
                    return newCollection;

                // Array of known size is populated in-place
                if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    for (int i = 0; i < propertyValueAsIList.Count; ++i)
                    {
                        var listItem = propertyValueAsIList[i];
                        newCollection[i] = listItem == null ? null : GetPropertyValue(listItem, underlyingType, mappedType);
                    }
                }
                // Array of unknown size is created only after populating list
                else
                {
                    foreach (var listItem in valueList)
                    {
                        newCollection.Add(listItem == null ? null : GetPropertyValue(listItem, underlyingType, mappedType));
                    }

                    if (fieldType.IsArray)
                        newCollection = ((dynamic)newCollection!).ToArray();
                }

                return newCollection;
            }

            var value = propertyValue;

            var nullableFieldType = Nullable.GetUnderlyingType(fieldType);

            // if this is a nullable type and the value is null, return null
            if (nullableFieldType != null && value == null)
            {
                return null;
            }

            if (nullableFieldType != null)
            {
                fieldType = nullableFieldType;
            }

            if (propertyValue is IDictionary<string, object?> objects)
            {
                return ToObject(objects, fieldType, mappedType);
            }

            if (fieldType.IsEnum)
            {
                if (value == null)
                {
                    var enumNames = Enum.GetNames(fieldType);
                    value = enumNames[0];
                }

                if (!IsDefinedEnumValue(fieldType, value))
                {
                    throw new InvalidOperationException($"Unknown value '{value}' for enum '{fieldType.Name}'.");
                }

                string str = value.ToString()!;
                value = Enum.Parse(fieldType, str, true);
            }

            return ValueConverter.ConvertTo(value, fieldType);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the value is <see langword="null"/>, value.ToString equals an empty string, or the value can be converted into a named enum value.
        /// </summary>
        /// <param name="type">An enum type.</param>
        /// <param name="value">The value being tested.</param>
        public static bool IsDefinedEnumValue(Type type, object? value) //TODO: rewrite, comment above seems wrong
        {
            try
            {
                var names = Enum.GetNames(type);
                if (names.Contains(value?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                var underlyingType = Enum.GetUnderlyingType(type);
                var converted = Convert.ChangeType(value, underlyingType);

                var values = Enum.GetValues(type);

                foreach (var val in values)
                {
                    var convertedVal = Convert.ChangeType(val, underlyingType);
                    if (convertedVal.Equals(converted))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // TODO: refactor IsDefinedEnumValue
            }

            return false;
        }
    }
}
