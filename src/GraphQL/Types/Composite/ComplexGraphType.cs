using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a default base class for all complex (that is, having their own properties) input and output graph types.
    /// </summary>
    public abstract class ComplexGraphType<TSourceType> : GraphType, IComplexGraphType
    {
        internal const string ORIGINAL_EXPRESSION_PROPERTY_NAME = nameof(ORIGINAL_EXPRESSION_PROPERTY_NAME);

        /// <inheritdoc/>
        protected ComplexGraphType()
        {
            if (typeof(IGraphType).IsAssignableFrom(typeof(TSourceType)) && GetType() != typeof(Introspection.__Type))
                throw new InvalidOperationException($"Cannot use graph type '{typeof(TSourceType).Name}' as a model for graph type '{GetType().Name}'. Please use a model rather than a graph type for {nameof(TSourceType)}.");

            Description ??= typeof(TSourceType).Description();
            DeprecationReason ??= typeof(TSourceType).ObsoleteMessage();
        }

        /// <inheritdoc/>
        public TypeFields Fields { get; } = new();

        /// <inheritdoc/>
        public bool HasField(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var field in Fields.List)
            {
                if (string.Equals(field.Name, name, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public FieldType? GetField(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var field in Fields.List)
            {
                if (field.Name == name)
                    return field;
            }

            return null;
        }

        /// <inheritdoc/>
        public virtual FieldType AddField(FieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateNameNotNull(fieldType.Name, NamedElement.Field);

            if (!fieldType.ResolvedType.IsGraphQLTypeReference())
            {
                if (this is IInputObjectGraphType)
                {
                    if (fieldType.ResolvedType != null ? fieldType.ResolvedType.IsInputType() == false : fieldType.Type?.IsInputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Input type '{Name ?? GetType().GetFriendlyName()}' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType. Field '{fieldType.Name}' has an output type.");
                }
                else
                {
                    if (fieldType.ResolvedType != null ? fieldType.ResolvedType.IsOutputType() == false : fieldType.Type?.IsOutputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Output type '{Name ?? GetType().GetFriendlyName()}' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType. Field '{fieldType.Name}' has an input type.");
                }
            }

            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType),
                    $"A field with the name '{fieldType.Name}' is already registered for GraphType '{Name ?? GetType().Name}'");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
                }
                else if (!fieldType.Type.IsGraphType())
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared Field type '{fieldType.Type.Name}' should derive from GraphType.");
                }
            }

            Fields.Add(fieldType);

            return fieldType;
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual FieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual FieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>(IGraphType type)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType Field(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, object?>? resolve = null,
            string? deprecationReason = null)
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, object?>? resolve = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Delegate? resolve = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
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

            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolver,
            });
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldAsync(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<object?>>? resolve = null,
            string? deprecationReason = null)
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(context => new ValueTask<object?>(resolve(context)))
                    : null
            });
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<object?>>? resolve = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(context => new ValueTask<object?>(resolve(context)))
                    : null
            });
        }

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <typeparam name="TReturnType">The type of the return value of the field resolver delegate.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType, TReturnType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>>? resolve = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, TReturnType>(context => new ValueTask<TReturnType?>(resolve(context)))
                    : null
            });
        }

        /// <summary>
        /// Adds a subscription field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of this field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Data from an event stream is processed by this field resolver as the source before being passed to the field's children as the source. Typically this would be <c>context => context.Source</c>.</param>
        /// <param name="subscribe">A source stream resolver delegate.</param>
        /// <param name="deprecationReason">The deprecation reason for the field.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldSubscribe<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, object?>? resolve = null, // TODO: remove?
            Func<IResolveFieldContext, IObservable<object?>>? subscribe = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
                StreamResolver = subscribe != null
                    ? new SourceStreamResolver<object>(subscribe)
                    : null
            });
        }

        /// <summary>
        /// Adds a subscription field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of this field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Data from an event stream is processed by this field resolver as the source before being passed to the field's children as the source. Typically this would be <c>context => context.Source</c>.</param>
        /// <param name="subscribeAsync">A source stream resolver delegate.</param>
        /// <param name="deprecationReason">The deprecation reason for the field.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and the methods defined on it or just use AddField() method directly. This method may be removed in a future release. For now you can continue to use this API but we do not encourage this.")]
        public FieldType FieldSubscribeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(
            string name,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext<TSourceType>, object?>? resolve = null, // TODO: remove?
            Func<IResolveFieldContext, Task<IObservable<object?>>>? subscribeAsync = null,
            string? deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
                StreamResolver = subscribeAsync != null
                    ? new SourceStreamResolver<object>(context => new ValueTask<IObservable<object?>>(subscribeAsync(context)))
                    : null
            });
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <typeparam name="TReturnType">The return type of the field resolver.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType, TReturnType>(string name)
            where TGraphType : IGraphType
        {
            var builder = CreateBuilder<TReturnType>(typeof(TGraphType)).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <inheritdoc cref="Field{TGraphType, TReturnType}(string)"/>
        [Obsolete("Please call Field<TGraphType, TReturnType>(string name) instead.")]
        public virtual FieldBuilder<TSourceType, TReturnType> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType, TReturnType>()
            where TGraphType : IGraphType
            => Field<TGraphType, TReturnType>("default");

        /// <inheritdoc cref="Field{TGraphType}(string)"/>
        [Obsolete("Please call Field<TGraphType>(string name) instead.")]
        public virtual FieldBuilder<TSourceType, object> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>()
            where TGraphType : IGraphType
            => Field<TGraphType, object>("default");

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual FieldBuilder<TSourceType, object> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(string name)
            where TGraphType : IGraphType
            => Field<TGraphType, object>(name);

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Field<TReturnType>(string name, bool nullable = false)
        {
            Type type;

            try
            {
                type = typeof(TReturnType).GetGraphTypeFromType(nullable, this is IInputObjectGraphType ? TypeMappingMode.InputType : TypeMappingMode.OutputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from type '{typeof(TReturnType).Name}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder<TReturnType>(type)
                .Name(name);

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual FieldBuilder<TSourceType, object> Field(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual FieldBuilder<TSourceType, object> Field(string name, IGraphType type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="name">The name of this field.</param>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            string name,
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
        {
            try
            {
                if (type == null)
                    type = typeof(TProperty).GetGraphTypeFromType(nullable, this is IInputObjectGraphType ? TypeMappingMode.InputType : TypeMappingMode.OutputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from expression '{expression}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder<TProperty>(type)
                .Name(name)
                .Description(expression.DescriptionOf())
                .DeprecationReason(expression.DeprecationReasonOf())
                .DefaultValue(expression.DefaultValueOf());

            if (this is IObjectGraphType)
                builder.Resolve(new ExpressionFieldResolver<TSourceType, TProperty>(expression));

            if (expression.Body is MemberExpression expr)
            {
                builder.FieldType.Metadata[ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
            }

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// The default name of this field is inferred by the property represented within the expression.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
        {
            string name;
            try
            {
                name = expression.NameOf();
            }
            catch
            {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
            return Field(name, expression, nullable, type);
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType}(string)"/>
        public ConnectionBuilder<TSourceType> Connection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType, TEdgeType}(string)"/>
        public ConnectionBuilder<TSourceType> Connection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
        {
            var builder = ConnectionBuilder.Create<TNodeType, TEdgeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType, TEdgeType, TConnectionType}(string)"/>
        public ConnectionBuilder<TSourceType> Connection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType, TConnectionType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            var builder = ConnectionBuilder.Create<TNodeType, TEdgeType, TConnectionType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }
    }
}
