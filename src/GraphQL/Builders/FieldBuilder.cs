using System.Linq.Expressions;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation.Complexity;

namespace GraphQL.Builders
{
    /// <summary>
    /// Static methods to create field builders.
    /// </summary>
    [Obsolete("This class will be removed in v8.")]
    public static class FieldBuilder
    {
        /// <summary>
        /// Returns a builder for a new field with a specified source type, return type and graph type.
        /// </summary>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        /// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
        /// <param name="type">The graph type of the field.</param>
        [Obsolete("Please use FieldBuilder<TSourceType, TReturnType>.Create() method. This method will be removed in v8.")]
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);

        /// <inheritdoc cref="Create{TSourceType, TReturnType}(Type)"/>
        [Obsolete("Please use FieldBuilder<TSourceType, TReturnType>.Create() method. This method will be removed in v8.")]
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(IGraphType type)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);
    }

    /// <summary>
    /// Builds a field for a graph with a specified source type and return type.
    /// </summary>
    /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
    /// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
    public class FieldBuilder<TSourceType, TReturnType>
    {
        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; }

        /// <summary>
        /// Initializes a new instance for the specified <see cref="Types.FieldType"/>.
        /// </summary>
        protected FieldBuilder(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for a new field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        /// <param name="name">The name of the field.</param>
        public static FieldBuilder<TSourceType, TReturnType> Create(IGraphType type, string name = "default")
        {
            var fieldType = new FieldType
            {
                Name = name,
                ResolvedType = type,
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        /// <inheritdoc cref="Create(IGraphType, string)"/>
        public static FieldBuilder<TSourceType, TReturnType> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null, string name = "default")
        {
            var fieldType = new FieldType
            {
                Name = name,
                Type = type,
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        /// <summary>
        /// Sets the graph type of the field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Type(IGraphType type)
        {
            FieldType.ResolvedType = type;
            return this;
        }

        /// <summary>
        /// Sets the name of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the description of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <summary>
        /// Sets the deprecation reason of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default value of fields on input object graph types.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType? defaultValue = default)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        internal FieldBuilder<TSourceType, TReturnType> DefaultValue(object? defaultValue)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Sets the resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver? resolver)
        {
            FieldType.Resolver = resolver;
            return this;
        }

        /// <inheritdoc cref="Resolve(IFieldResolver)"/>
        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));

        /// <inheritdoc cref="Resolve(IFieldResolver)"/>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(context => new ValueTask<TReturnType?>(resolve(context))));

        /// <inheritdoc cref="Resolve(IFieldResolver)"/>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveDelegate(Delegate? resolve)
        {
            IFieldResolver? resolver = null;

            if (resolve != null)
            {
                // create an instance expression that points to the instance represented by the delegate
                // for instance, if the delegate represents obj.MyMethod,
                // then the lambda would be: _ => obj
                var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
                var body = Expression.Constant(resolve.Target, resolve.Method.DeclaringType!);
                var lambda = Expression.Lambda(body, param);
                resolver = AutoRegisteringHelper.BuildFieldResolver(resolve.Method, null, null, lambda);
            }

            return Resolve(resolver);
        }

        /// <summary>
        /// Sets the return type of the field.
        /// </summary>
        /// <typeparam name="TNewReturnType">The type of the return value of the resolver.</typeparam>
        public virtual FieldBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
            => new(FieldType);

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        [Obsolete("Please use Action<QueryArgument> parameter from other Argument() method overloads to set default value for parameter or use Arguments() method. This method will be removed in v8.")]
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType, TArgumentType>(string name, string? description,
            TArgumentType? defaultValue = default, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, null);

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
            => Argument(typeof(TArgumentGraphType), name, configure);

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentClrType">The clr type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="nullable">Indicates if the argument is optional or not.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentClrType>(string name, bool nullable = false, Action<QueryArgument>? configure = null)
        {
            Type type;

            try
            {
                type = typeof(TArgumentClrType).GetGraphTypeFromType(nullable, TypeMappingMode.InputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for argument '{FieldType.Name}.{name}' could not be derived implicitly from type '{typeof(TArgumentClrType).Name}'. " + exp.Message, exp);
            }

            return Argument(type, name, configure);
        }

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentClrType">The clr type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="nullable">Indicates if the argument is optional or not.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentClrType>(string name, bool nullable, string? description, Action<QueryArgument>? configure = null)
            => Argument<TArgumentClrType>(name, nullable, b =>
            {
                b.Description = description;
                configure?.Invoke(b);
            });

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <param name="type">The graph type of the argument.</param>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string name, Action<QueryArgument>? configure = null)
        {
            var arg = new QueryArgument(type)
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments ??= new();
            FieldType.Arguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds the specified collection of arguments to the field.
        /// </summary>
        /// <param name="arguments">Arguments to add.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Arguments(IEnumerable<QueryArgument> arguments)
        {
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    FieldType.Arguments ??= new();
                    FieldType.Arguments.Add(arg);
                }
            }
            return this;
        }

        /// <summary>
        /// Adds the specified collection of arguments to the field.
        /// </summary>
        /// <param name="arguments">Arguments to add.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Arguments(params QueryArgument[] arguments)
        {
            return Arguments((IEnumerable<QueryArgument>)arguments);
        }

        /// <summary>
        /// Runs a configuration delegate for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Sets a source stream resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveStream(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> sourceStreamResolver)
        {
            FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(sourceStreamResolver);
            FieldType.Resolver ??= SourceFieldResolver.Instance;
            return this;
        }

        /// <summary>
        /// Sets a source stream resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveStreamAsync(Func<IResolveFieldContext<TSourceType>, Task<IObservable<TReturnType?>>> sourceStreamResolver)
        {
            FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(context => new ValueTask<IObservable<TReturnType?>>(sourceStreamResolver(context)));
            FieldType.Resolver ??= SourceFieldResolver.Instance;
            return this;
        }

        /// <summary>
        /// Apply directive to field without specifying arguments. If the directive declaration has arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name)
        {
            FieldType.ApplyDirective(name);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying one argument. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive specifying two arguments. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argument1Name">First argument name.</param>
        /// <param name="argument1Value">First argument value.</param>
        /// <param name="argument2Name">Second argument name.</param>
        /// <param name="argument2Value">Second argument value.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name, string argument1Name, object? argument1Value, string argument2Name, object? argument2Value)
        {
            FieldType.ApplyDirective(name, argument1Name, argument1Value, argument2Name, argument2Value);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }

        /// <summary>
        /// Specify field's complexity impact which will be taken into account by <see cref="ComplexityAnalyzer"/>.
        /// </summary>
        /// <param name="impact">Field's complexity impact.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> ComplexityImpact(double impact)
        {
            FieldType.WithComplexityImpact(impact);
            return this;
        }
    }
}
