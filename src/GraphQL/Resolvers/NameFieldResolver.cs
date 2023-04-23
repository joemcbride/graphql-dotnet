using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// <para>
    /// Attempts to return a value for a field from the graph's source object, matching the name of
    /// the field to a property or a method with the same name on the source object.
    /// </para><para>
    /// Call <see cref="Instance"/> to retrieve an instance of this class.
    /// </para>
    /// </summary>
    public class NameFieldResolver : IFieldResolver
    {
        private static readonly ConcurrentDictionary<(Type targetType, string name), IFieldResolver> _resolvers = new();

        private NameFieldResolver() { }

        /// <summary>
        /// Returns the static instance of the <see cref="NameFieldResolver"/> class.
        /// </summary>
        public static NameFieldResolver Instance { get; } = new();

        /// <inheritdoc/>
        public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => Resolve(context, context.FieldDefinition.Name);

        private static ValueTask<object?> Resolve(IResolveFieldContext context, string? name)
        {
            if (context.Source == null || name == null)
                return default;

            // We use reflection to create a delegate to access the property/method
            // Then cache the delegate
            // This is over 10x faster that just using reflection to get the property/method value
            var resolver = _resolvers.GetOrAdd((context.Source.GetType(), name), t => CreateResolver(t.targetType, t.name));
            return resolver.ResolveAsync(context);
        }

        /// <summary>
        /// <para>
        /// Dynamically creates the necessary delegate in runtime to get the property/method value of the specified type.
        /// </para><para>
        /// Example:
        /// </para><code>
        /// public class Person
        /// {
        ///     public int Age { get; set; }
        /// }
        /// </code>
        /// <para>
        /// So resulting Func will be generated as <c>{x => Convert(Convert(x, Person).Age, Object)}</c><br/>
        /// 1. First, the input parameter 'x' is converted from the object to a specific type.<br/>
        /// 2. The required property or method is extracted from casted value.<br/>
        /// 3. Then result is converted again to the object and returned from the method.
        /// </para>
        /// </summary>
        /// <param name="target"> The type from which you want to get the value. </param>
        /// <param name="name"> Property/method name. </param>
        /// <returns> Compiled field resolver. </returns>
        private static IFieldResolver CreateResolver(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type target,
            string name)
        {
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var source = Expression.MakeMemberAccess(param, _sourcePropertyInfo);
            var cast = Expression.Convert(source, target);
            var instanceLambda = Expression.Lambda(cast, param);

            MemberInfo? member = target.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (member == null)
            {
                try
                {
                    member = target.GetMethod(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                }
                catch (AmbiguousMatchException)
                {
                    throw new InvalidOperationException($"Expected to find a single property or method '{name}' on type '{target.Name}' but multiple methods were found.");
                }
                if (member == null)
                {
                    throw new InvalidOperationException($"Expected to find property or method '{name}' on type '{target.Name}' but it does not exist.");
                }
            }
            return AutoRegisteringHelper.BuildFieldResolver(member, target, null, instanceLambda);
        }

        private static readonly PropertyInfo _sourcePropertyInfo = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;
    }
}
