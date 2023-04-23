using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Builders
{
    /// <summary>
    /// Static methods to create connection field builders.
    /// </summary>
    public static class ConnectionBuilder
    {
        /// <summary>
        /// Returns a builder for new connection field for the specified node type.
        /// The edge type is <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;&gt;.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, TSourceType>()
            where TNodeType : IGraphType
            => ConnectionBuilder<TSourceType>.Create<TNodeType>();

        /// <summary>
        /// Returns a builder for new connection field for the specified node and edge type.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            => ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType>();

        /// <summary>
        /// Returns a builder for new connection field for the specified node, edge and connection type.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TConnectionType">The graph type of the connection. Must derive from <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType, TConnectionType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
            => ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType, TConnectionType>();
    }

    /// <summary>
    /// Builds a connection field for graphs that have the specified source type.
    /// </summary>
    // TODO: Remove in v5
    public class ConnectionBuilder<TSourceType>
    {
        internal const string PAGE_SIZE_METADATA_KEY = "__ConnectionBuilder_PageSize";

        private bool IsBidirectional => FieldType.Arguments?.Find("before")?.Type == typeof(StringGraphType) && FieldType.Arguments.Find("last")?.Type == typeof(IntGraphType);

        private int? PageSizeFromMetadata
        {
            get => FieldType.GetMetadata<int?>(PAGE_SIZE_METADATA_KEY);
            set => FieldType.WithMetadata(PAGE_SIZE_METADATA_KEY, value);
        }

        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; protected set; }

        /// <summary>
        /// Initializes a new instance for the specified <see cref="Types.FieldType"/>.
        /// </summary>
        protected ConnectionBuilder(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for new connection field for the specified node type.
        /// The edge type is <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;&gt;.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType>(string name = "default")
            where TNodeType : IGraphType => Create<TNodeType, EdgeType<TNodeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node and edge type.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            => Create<TNodeType, TEdgeType, ConnectionType<TNodeType, TEdgeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node, edge and connection type.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TConnectionType">The graph type of the connection. Must derive from <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType, TConnectionType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            var fieldType = new FieldType
            {
                Name = name,
                Type = typeof(TConnectionType),
                Arguments = new QueryArguments(),
            };
            fieldType.Arguments.Add(new QueryArgument(typeof(StringGraphType))
            {
                Name = "after",
                Description = "Only return edges after the specified cursor.",
            });
            fieldType.Arguments.Add(new QueryArgument(typeof(IntGraphType))
            {
                Name = "first",
                Description = "Specifies the maximum number of edges to return, starting after the cursor specified by 'after', or the first number of edges if 'after' is not specified.",
            });
            return new ConnectionBuilder<TSourceType>(fieldType);
        }

        /// <summary>
        /// Configure the connection to be bi-directional.
        /// </summary>
        public ConnectionBuilder<TSourceType> Bidirectional()
        {
            if (IsBidirectional)
            {
                return this;
            }

            Argument<StringGraphType, string>("before",
                "Only return edges prior to the specified cursor.");
            Argument<IntGraphType, int?>("last",
                "Specifies the maximum number of edges to return, starting prior to the cursor specified by 'before', or the last number of edges if 'before' is not specified.");

            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Name(string)"/>
        public virtual ConnectionBuilder<TSourceType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Description(string)"/>
        public virtual ConnectionBuilder<TSourceType> Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.DeprecationReason(string)"/>
        public virtual ConnectionBuilder<TSourceType> DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default page size.
        /// </summary>
        public virtual ConnectionBuilder<TSourceType> PageSize(int pageSize)
        {
            PageSizeFromMetadata = pageSize;
            return this;
        }

        /// <summary>
        /// Clears the default page size, so all records are returned by default.
        /// </summary>
        public virtual ConnectionBuilder<TSourceType> ReturnAll()
        {
            PageSizeFromMetadata = null;
            return this;
        }

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments!.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        public virtual ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string? description)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg => arg.Description = description);

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        public virtual ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
            [AllowNull] TArgumentType defaultValue = default!)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
            });

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
            [AllowNull] TArgumentType defaultValue, Action<QueryArgument>? configure)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Runs a configuration delegate for the connection field.
        /// </summary>
        public virtual ConnectionBuilder<TSourceType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field without specifying arguments. If the directive
        /// declaration has arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual ConnectionBuilder<TSourceType> Directive(string name)
        {
            FieldType.ApplyDirective(name);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying one argument. If the directive
        /// declaration has other arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        public virtual ConnectionBuilder<TSourceType> Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual ConnectionBuilder<TSourceType> Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }

        /// <summary>
        /// Specifies the return data type of the connection resolver. Typically this would be <see cref="Connection{TNode}"/>
        /// or <see cref="Connection{TNode, TEdge}"/>, but it can be any type that returns the proper fields for the
        /// connection graph type.
        /// <br/><br/>
        /// For instance, to specify a return type for a Widget you might write:
        /// <br/><br/>
        /// <code>
        /// .Returns&lt;Connection&lt;Widget&gt;&gt;()
        /// </code>
        /// </summary>
        public virtual ConnectionBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new ConnectionBuilder<TSourceType, TNewReturnType>(FieldType);
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public virtual void Resolve(Func<IResolveConnectionContext<TSourceType>, object?> resolver)
        {
            var isUnidirectional = !IsBidirectional;
            var pageSize = PageSizeFromMetadata;
            FieldType.Resolver = new Resolvers.FuncFieldResolver<object>(context =>
            {
                var connectionContext = new ResolveConnectionContext<TSourceType>(context, isUnidirectional, pageSize);
                CheckForErrors(connectionContext);
                return resolver(connectionContext);
            });
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public virtual void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, Task<object?>> resolver)
        {
            var isUnidirectional = !IsBidirectional;
            var pageSize = PageSizeFromMetadata;
            FieldType.Resolver = new Resolvers.FuncFieldResolver<object>(context =>
            {
                var connectionContext = new ResolveConnectionContext<TSourceType>(context, isUnidirectional, pageSize);
                CheckForErrors(connectionContext);
                return new ValueTask<object?>(resolver(connectionContext));
            });
        }

        private static void CheckForErrors(IResolveConnectionContext<TSourceType> context)
        {
            if (context.First.HasValue && context.Last.HasValue)
            {
                throw new ArgumentException("Cannot specify both `first` and `last`.");
            }
            if (context.IsUnidirectional && context.Last.HasValue)
            {
                throw new ArgumentException("Cannot use `last` with unidirectional connections.");
            }
        }
    }
}
