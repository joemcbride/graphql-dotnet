using System.Reflection;

namespace GraphQL.Reflection
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// Creates an <see cref="IAccessor"/> for the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        /// <param name="resolverType">defaults to Resolver</param>
        public static IAccessor? ToAccessor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] this Type? type, string field, ResolverType resolverType)
        {
            if (type == null)
                return null;

            var methodInfo = type.MethodForField(field, resolverType);
            if (methodInfo != null)
            {
                return new SingleMethodAccessor(type, methodInfo);
            }

            if (resolverType != ResolverType.Resolver)
            {
                return null;
            }

            var propertyInfo = type.PropertyForField(field);
            if (propertyInfo != null)
            {
                return new SinglePropertyAccessor(type, propertyInfo);
            }

            return null;
        }

        /// <summary>
        /// Returns the method associated with the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        /// <param name="resolverType">Indicates if a resolver or stream resolver method is requested.</param>
        public static MethodInfo? MethodForField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type type, string field, ResolverType resolverType)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var method = methods.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<GraphQLMetadataAttribute>();
                var name = attr?.Name ?? m.Name;
                return string.Equals(field, name, StringComparison.OrdinalIgnoreCase) && resolverType == (attr?.ResolverType ?? ResolverType.Resolver);
            });

            return method;
        }

        /// <summary>
        /// Returns the property associated with the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        public static PropertyInfo? PropertyForField([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type, string field)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var property = properties.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<GraphQLMetadataAttribute>();
                var name = attr?.Name ?? m.Name;
                return string.Equals(field, name, StringComparison.OrdinalIgnoreCase);
            });

            return property;
        }
    }
}
