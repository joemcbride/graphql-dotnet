using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using GraphQL.Conversion;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Resolvers;
using GraphQL.Types.Collections;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a list of all the graph types utilized by a schema.
    /// Also provides lookup for all schema types and has algorithms for discovering them.
    /// <br/>
    /// NOTE: After creating an instance of this class, its contents cannot be changed.
    /// </summary>
    public class SchemaTypes : IEnumerable<IGraphType>
    {
        private const string INITIALIZATIION_TRACE_KEY = "__INITIALIZATIION_TRACE_KEY__";

        /// <summary>
        /// Returns a dictionary of default CLR type to graph type mappings for a set of built-in (primitive) types.
        /// </summary>
        public static ReadOnlyDictionary<Type, Type> BuiltInScalarMappings { get; } = new(new Dictionary<Type, Type>
        {
            [typeof(int)] = typeof(IntGraphType),
            [typeof(long)] = typeof(LongGraphType),
            [typeof(BigInteger)] = typeof(BigIntGraphType),
            [typeof(double)] = typeof(FloatGraphType),
            [typeof(float)] = typeof(FloatGraphType),
            [typeof(decimal)] = typeof(DecimalGraphType),
            [typeof(string)] = typeof(StringGraphType),
            [typeof(bool)] = typeof(BooleanGraphType),
            [typeof(DateTime)] = typeof(DateTimeGraphType),
#if NET5_0_OR_GREATER
            [typeof(Half)] = typeof(HalfGraphType),
#endif
#if NET6_0_OR_GREATER
            [typeof(DateOnly)] = typeof(DateOnlyGraphType),
            [typeof(TimeOnly)] = typeof(TimeOnlyGraphType),
#endif
            [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
            [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
            [typeof(Guid)] = typeof(IdGraphType),
            [typeof(short)] = typeof(ShortGraphType),
            [typeof(ushort)] = typeof(UShortGraphType),
            [typeof(ulong)] = typeof(ULongGraphType),
            [typeof(uint)] = typeof(UIntGraphType),
            [typeof(byte)] = typeof(ByteGraphType),
            [typeof(sbyte)] = typeof(SByteGraphType),
            [typeof(Uri)] = typeof(UriGraphType),
        });

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GraphQLClrInputTypeReference<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GraphQLClrOutputTypeReference<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ListGraphType<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NonNullGraphType<>))]
        static SchemaTypes()
        {
            // The above attributes preserve those classes when T is a reference type, but not
            // when T is a value type, which is necessary for GraphQL CLR type references.
            // Also, specifying the closed generic type does not help, so the only way to force
            // the trimmer to preserve types such as GraphQLClrInputTypeReference<int> is to
            // directly reference the type within the compiled MSIL, even if the code does
            // not actually run. While this does not help with user defined structs, the combination
            // of the above attributes and below code will allow all built-in types to be handled
            // by the auto-registering graph types such as AutoRegisteringObjectGraphType and
            // similar code.

            if (BuiltInScalarMappings != null) // always true
                return; // no need to actually execute the below code, but it must be present in the compiled IL

            // prevent trimming of these input and output type reference types
            Preserve<int>();
            Preserve<long>();
            Preserve<BigInteger>();
            Preserve<double>();
            Preserve<float>();
            Preserve<decimal>();
            Preserve<string>();
            Preserve<bool>();
            Preserve<DateTime>();
#if NET5_0_OR_GREATER
            Preserve<Half>();
#endif
#if NET6_0_OR_GREATER
            Preserve<DateOnly>();
            Preserve<TimeOnly>();
#endif
            Preserve<DateTimeOffset>();
            Preserve<TimeSpan>();
            Preserve<Guid>();
            Preserve<short>();
            Preserve<ushort>();
            Preserve<ulong>();
            Preserve<uint>();
            Preserve<byte>();
            Preserve<sbyte>();
            Preserve<Uri>();

            static void Preserve<T>()
            {
                // force the MSIL to contain strong references to the specified type
                GC.KeepAlive(typeof(GraphQLClrInputTypeReference<T>));
                GC.KeepAlive(typeof(GraphQLClrOutputTypeReference<T>));
            }
        }

        // Introspection types https://spec.graphql.org/October2021/#sec-Schema-Introspection
        private Dictionary<Type, IGraphType> _introspectionTypes;

        // Standard scalars https://spec.graphql.org/October2021/#sec-Scalars
        private readonly Dictionary<Type, IGraphType> _builtInScalars = new IGraphType[]
        {
            new StringGraphType(),
            new BooleanGraphType(),
            new FloatGraphType(),
            new IntGraphType(),
            new IdGraphType(),
        }
        .ToDictionary(t => t.GetType());

        // .NET custom scalars
        private readonly Dictionary<Type, IGraphType> _builtInCustomScalars = new IGraphType[]
        {
            new DateGraphType(),
#if NET5_0_OR_GREATER
            new HalfGraphType(),
#endif
#if NET6_0_OR_GREATER
            new DateOnlyGraphType(),
            new TimeOnlyGraphType(),
#endif
            new DateTimeGraphType(),
            new DateTimeOffsetGraphType(),
            new TimeSpanSecondsGraphType(),
            new TimeSpanMillisecondsGraphType(),
            new DecimalGraphType(),
            new UriGraphType(),
            new GuidGraphType(),
            new ShortGraphType(),
            new UShortGraphType(),
            new UIntGraphType(),
            new LongGraphType(),
            new BigIntGraphType(),
            new ULongGraphType(),
            new ByteGraphType(),
            new SByteGraphType(),
        }
        .ToDictionary(t => t.GetType());

        private TypeCollectionContext _context;
        private INameConverter _nameConverter;

        /// <summary>
        /// Initializes a new instance with no types registered.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected SchemaTypes()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <summary>
        /// Initializes a new instance for the specified schema, and with the specified type resolver.
        /// </summary>
        /// <param name="schema">A schema for which this instance is created.</param>
        /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
        public SchemaTypes(ISchema schema, IServiceProvider serviceProvider)
            : this(schema, serviceProvider, (IEnumerable<IGraphTypeMappingProvider>?)serviceProvider.GetService(typeof(IEnumerable<IGraphTypeMappingProvider>)))
        {
        }

        /// <summary>
        /// Initializes a new instance for the specified schema, with the specified type resolver,
        /// with the specified set of <see cref="IGraphTypeMappingProvider"/> instances.
        /// </summary>
        /// <param name="schema">A schema for which this instance is created.</param>
        /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
        /// <param name="graphTypeMappings">A list of <see cref="IGraphTypeMappingProvider"/> instances used to map CLR types to graph types.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SchemaTypes(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Initialize(schema, serviceProvider, graphTypeMappings);
        }

        private bool _initialized = false;
        /// <summary>
        /// Initializes the instance for the specified schema, and with the specified type resolver.
        /// </summary>
        /// <param name="schema">A schema for which this instance is created.</param>
        /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
        /// <param name="graphTypeMappings">A service used to map CLR types to graph types.</param>
        protected void Initialize(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (_initialized)
                throw new InvalidOperationException("SchemaTypes has already been initialized.");
            _initialized = true;

            var (typeInstances, types) = GetSchemaTypes(schema, serviceProvider);
            if (schema.TypeMappings != null)
            {
                // this code could be moved into Schema
                var additionalMappings = schema.TypeMappings.Select(x => new ManualGraphTypeMappingProvider(x.clrType, x.graphType));
                graphTypeMappings = graphTypeMappings != null ? graphTypeMappings.Concat(additionalMappings).ToList() : additionalMappings.ToList();
            }
            var directives = schema.Directives ?? throw new ArgumentNullException(nameof(schema) + "." + nameof(ISchema.Directives));

            _typeDictionary = new Dictionary<Type, IGraphType>();
            if (schema.Features.DeprecationOfInputValues)
            {
                // TODO: remove this code block when the next version of the spec will be released
                schema.Directives.Deprecated.Locations.Add(GraphQLParser.AST.DirectiveLocation.ArgumentDefinition);
                schema.Directives.Deprecated.Locations.Add(GraphQLParser.AST.DirectiveLocation.InputFieldDefinition);
            }
            _introspectionTypes = CreateIntrospectionTypes(schema.Features.AppliedDirectives, schema.Features.RepeatableDirectives, schema.Features.DeprecationOfInputValues);

            _context = new TypeCollectionContext(
               type => BuildGraphQLType(type, ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] t) => _builtInScalars.TryGetValue(t, out var graphType) ? graphType : _introspectionTypes.TryGetValue(t, out graphType) ? graphType : (IGraphType)Activator.CreateInstance(t)!),
               (name, type, ctx) =>
               {
                   SetGraphType(name, type);
                   ctx.AddType(name, type, null!);
               },
               graphTypeMappings,
               schema);

            // Add manually-added scalar types. To allow overriding of built-in scalars, these must be added
            // prior to adding any other types (including introspection types).
            using (_context.Trace("Loop over manually-added scalar types from AdditionalTypeInstances"))
            {
                foreach (var type in typeInstances)
                {
                    if (type is ScalarGraphType)
                        AddType(type, _context);
                }
            }

            // Add introspection types. Note that introspection types rely on the
            // CamelCaseNameConverter, as some fields are defined in pascal case - e.g. Field(x => x.Name)
            _nameConverter = CamelCaseNameConverter.Instance;

            using (_context.Trace("__Schema root type"))
                AddType(_introspectionTypes[typeof(__Schema)], _context);

            // set the name converter properly
            _nameConverter = schema.NameConverter ?? CamelCaseNameConverter.Instance;

            var ctx = new TypeCollectionContext(
                t => _builtInScalars.TryGetValue(t, out var graphType) ? graphType : (IGraphType)serviceProvider.GetRequiredService(t),
                (name, graphType, context) =>
                {
                    if (this[name] == null)
                    {
                        using var _ = context.Trace("TypeCollectionContext.AddType delegate");
                        AddType(graphType, context);
                    }
                },
                graphTypeMappings,
                schema);

            using (ctx.Trace("Loop over manually-added non-scalar types from AdditionalTypeInstances"))
            {
                foreach (var type in typeInstances)
                {
                    if (type is not ScalarGraphType)
                        AddTypeIfNotRegistered(type, ctx);
                }
            }

            using (ctx.Trace("Loop over manually-added types from AdditionalTypes"))
            {
                foreach (var type in types)
                {
                    AddTypeIfNotRegistered(type, ctx);
                }
            }

            // these fields must not have their field names translated by INameConverter; see HandleField
            using (ctx.Trace("__schema root field"))
                HandleField(null, SchemaMetaFieldType, ctx, false);
            using (ctx.Trace("__type root field"))
                HandleField(null, TypeMetaFieldType, ctx, false);
            using (ctx.Trace("__typename root field"))
                HandleField(null, TypeNameMetaFieldType, ctx, false);

            using (ctx.Trace("Loop for directives"))
            {
                foreach (var directive in directives)
                {
                    using var _ = ctx.Trace("Directive '{0}'", directive.Name);
                    HandleDirective(directive, ctx);
                }
            }

            ApplyTypeReferences();

            // https://github.com/graphql-dotnet/graphql-dotnet/issues/1004
            InheritInterfaceDescriptions();

            Debug.Assert(ctx.InFlightRegisteredTypes.Count == 0);
            Debug.Assert((ctx.InitializationTrace?.Count ?? 0) == 0);
            Debug.Assert((_context.InitializationTrace?.Count ?? 0) == 0);

            _typeDictionary = null!; // not needed once initialization is complete
        }

        private static (IEnumerable<IGraphType>, IEnumerable<Type>) GetSchemaTypes(ISchema schema, IServiceProvider serviceProvider)
        {
            return (GetSchemaTypeInstances(schema, serviceProvider),
                schema.AdditionalTypes.Select(x => x.GetNamedType()).Where(x => !typeof(ScalarGraphType).IsAssignableFrom(x)));
        }

        private static IEnumerable<IGraphType> GetSchemaTypeInstances(ISchema schema, IServiceProvider serviceProvider)
        {
            // Manually registered AdditionalTypeInstances and AdditionalTypes should be handled first.
            // This is necessary for the correct processing of overridden built-in scalars.

            foreach (var instance in schema.AdditionalTypeInstances)
                yield return instance;

            foreach (var type in schema.AdditionalTypes)
            {
                var type2 = type.GetNamedType();
                if (typeof(ScalarGraphType).IsAssignableFrom(type2))
                {
                    yield return (IGraphType)serviceProvider.GetRequiredService(type2);
                }
            }

            //TODO: According to the specification, Query is a required type. But if you uncomment these lines, then the mass of tests begin to fail, because they do not set Query.
            // if (Query == null)
            //    throw new InvalidOperationException("Query root type must be provided. See https://spec.graphql.org/October2021/#sec-Schema-Introspection");

            if (schema.Query != null)
                yield return schema.Query;

            if (schema.Mutation != null)
                yield return schema.Mutation;

            if (schema.Subscription != null)
                yield return schema.Subscription;
        }

        private static Dictionary<Type, IGraphType> CreateIntrospectionTypes(bool allowAppliedDirectives, bool allowRepeatable, bool deprecationOfInputValues)
        {
            return (allowAppliedDirectives
                ? new IGraphType[]
                {
                    new __DirectiveLocation(),
                    new __DirectiveArgument(),
                    new __AppliedDirective(),
                    new __TypeKind(),
                    new __EnumValue(true),
                    new __Directive(true, allowRepeatable),
                    new __Field(true, deprecationOfInputValues),
                    new __InputValue(true, deprecationOfInputValues),
                    new __Type(true, deprecationOfInputValues),
                    new __Schema(true)
                }
                : new IGraphType[]
                {
                    new __DirectiveLocation(),
                    //new __DirectiveArgument(), forbidden
                    //new __AppliedDirective(),  forbidden
                    new __TypeKind(),
                    new __EnumValue(false),
                    new __Directive(false, allowRepeatable),
                    new __Field(false, deprecationOfInputValues),
                    new __InputValue(false, deprecationOfInputValues),
                    new __Type(false, deprecationOfInputValues),
                    new __Schema(false)
                })
            .ToDictionary(t => t.GetType());
        }

        /// <summary>
        /// Returns a dictionary that relates type names to graph types.
        /// </summary>
        protected internal virtual Dictionary<ROM, IGraphType> Dictionary { get; } = new Dictionary<ROM, IGraphType>();
        private Dictionary<Type, IGraphType> _typeDictionary;

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<IGraphType> GetEnumerator() => Dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the count of all the graph types utilized by the schema.
        /// </summary>
        public int Count => Dictionary.Count;

        private IGraphType BuildGraphQLType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            IGraphType resolvedType)
            => BuildGraphQLType(type, _ => resolvedType);

        /// <summary>
        /// Returns a new instance of the specified graph type, using the specified resolver to
        /// instantiate a new instance if the required type cannot be found from the lookup table.
        /// Defaults to <see cref="Activator.CreateInstance(Type)"/> if no <paramref name="resolve"/>
        /// parameter is specified. List and non-null graph types are instantiated and their
        /// <see cref="IProvideResolvedType.ResolvedType"/> property is set to a new instance of
        /// the base (wrapped) type.
        /// </summary>
        protected internal virtual IGraphType BuildGraphQLType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            Func<Type, IGraphType> resolve)
        {
            var local = resolve;
            local ??= ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] t) => (IGraphType)Activator.CreateInstance(t)!;
            resolve = t => FindGraphType(t) ?? local(t);

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>))
                {
                    var nonNull = (NonNullGraphType)Activator.CreateInstance(type)!;
                    nonNull.ResolvedType = BuildGraphQLType(type.GenericTypeArguments[0], resolve);
                    return nonNull;
                }

                if (type.GetGenericTypeDefinition() == typeof(ListGraphType<>))
                {
                    var list = (ListGraphType)Activator.CreateInstance(type)!;
                    list.ResolvedType = BuildGraphQLType(type.GenericTypeArguments[0], resolve);
                    return list;
                }
            }

            return resolve(type) ??
                   throw new InvalidOperationException(
                       $"Expected non-null value, but {nameof(resolve)} delegate return null for '{type.Name}'");
        }

        /// <summary>
        /// Applies all delegates specified by the middleware builder to the schema.
        /// <br/><br/>
        /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
        /// Therefore, as a rule, this method should be called only once - during schema initialization.
        /// </summary>
        public void ApplyMiddleware(IFieldMiddlewareBuilder fieldMiddlewareBuilder)
        {
            var transform = (fieldMiddlewareBuilder ?? throw new ArgumentNullException(nameof(fieldMiddlewareBuilder))).Build();

            // allocation free optimization if no middlewares are defined
            if (transform != null)
            {
                ApplyMiddleware(transform);
            }
        }

        /// <summary>
        /// Applies the specified middleware transform delegate to the schema.
        /// <br/><br/>
        /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
        /// Therefore, as a rule, this method should be called only once - during schema initialization.
        /// </summary>
        public void ApplyMiddleware(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            foreach (var item in Dictionary)
            {
                if (item.Value is IObjectGraphType obj)
                {
                    foreach (var field in obj.Fields.List)
                    {
                        var inner = field.Resolver ?? (field.StreamResolver == null ? NameFieldResolver.Instance : SourceFieldResolver.Instance);

                        var fieldMiddlewareDelegate = transform(inner.ResolveAsync);

                        field.Resolver = new FuncFieldResolver<object>(fieldMiddlewareDelegate.Invoke);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a graph type instance from the lookup table by its GraphQL type name.
        /// </summary>
        public IGraphType? this[ROM typeName]
        {
            get
            {
                if (typeName.IsEmpty)
                {
                    throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
                }

                return Dictionary.TryGetValue(typeName, out var type) ? type : null;
            }
        }

        /// <summary>
        /// Returns a graph type instance from the lookup table by its .NET type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type.</param>
        private IGraphType? FindGraphType(Type type)
        {
            return _typeDictionary == null
                ? null
                : _typeDictionary.TryGetValue(type, out var value) ? value : null;
        }

        private void AddType(IGraphType type, TypeCollectionContext context)
        {
            if (type == null || type is GraphQLTypeReference)
            {
                return;
            }

            if (type is NonNullGraphType || type is ListGraphType)
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Only add root types.");
            }

            type.Initialize(context.Schema);
            if (context.InitializationTrace != null)
                type.WithMetadata(INITIALIZATIION_TRACE_KEY, string.Join(Environment.NewLine, context.InitializationTrace));
            SetGraphType(type.Name, type);

            if (type is IComplexGraphType complexType)
            {
                using var _ = context.Trace("Loop for fields of complex type '{0}'", complexType.Name);
                foreach (var field in complexType.Fields)
                {
                    using var __ = context.Trace("Field '{0}.{1}'", complexType.Name, field.Name);
                    HandleField(complexType, field, context, true);
                }
            }

            if (type is IObjectGraphType obj)
            {
                using var _ = context.Trace("Loop for interfaces of object type '{0}'", obj.Name);
                foreach (var objectInterface in obj.Interfaces.List)
                {
                    using var __ = context.Trace("Interface '{0}'", objectInterface.Name);
                    object typeOrError = RebuildType(objectInterface, false, context.ClrToGraphTypeMappings);
                    if (typeOrError is string error)
                        throw new InvalidOperationException($"The GraphQL implemented type '{objectInterface.GetFriendlyName()}' for object graph type '{type.Name}' could not be derived implicitly. " + error);
                    var objectInterface2 = (Type)typeOrError;
                    if (AddTypeIfNotRegistered(objectInterface2, context) is IInterfaceGraphType interfaceInstance)
                    {
                        obj.AddResolvedInterface(interfaceInstance);
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new InvalidOperationException(
                               $"Interface type '{interfaceInstance.Name}' does not provide a 'resolveType' function " +
                               $"and possible Type '{obj.Name}' does not provide a 'isTypeOf' function. " +
                                "There is no way to resolve this possible type during execution.");
                        }
                    }
                }
            }

            if (type is UnionGraphType union)
            {
                using var _ = context.Trace("Loop for possible types of union type '{0}'", union.Name);
                if (!union.Types.Any() && !union.PossibleTypes.Any())
                {
                    throw new InvalidOperationException($"Must provide types for Union '{union}'.");
                }

                foreach (var unionedType in union.PossibleTypes)
                {
                    using var __ = context.Trace("Possible graph type '{0}'", unionedType.Name);
                    // skip references
                    if (unionedType is GraphQLTypeReference)
                        continue;

                    AddTypeIfNotRegistered(unionedType, context);

                    if (union.ResolveType == null && unionedType.IsTypeOf == null)
                    {
                        throw new InvalidOperationException(
                           $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                           $"and possible Type '{unionedType.Name}' does not provide a 'isTypeOf' function. " +
                            "There is no way to resolve this possible type during execution.");
                    }
                }

                foreach (var unionedType in union.Types)
                {
                    using var __ = context.Trace("Possible clr type '{0}'", unionedType.Name);
                    object typeOrError = RebuildType(unionedType, false, context.ClrToGraphTypeMappings);
                    if (typeOrError is string error)
                        throw new InvalidOperationException($"The GraphQL type '{unionedType.GetFriendlyName()}' for union graph type '{type.Name}' could not be derived implicitly. " + error);
                    var unionedType2 = (Type)typeOrError;
                    if (AddTypeIfNotRegistered(unionedType2, context) is not IObjectGraphType objType)
                        throw new InvalidOperationException($"The GraphQL type '{unionedType.GetFriendlyName()}' for union graph type '{type.Name}' could not be derived implicitly. The resolved type is not an {nameof(IObjectGraphType)}.");

                    if (union.ResolveType == null && objType != null && objType.IsTypeOf == null)
                    {
                        throw new InvalidOperationException(
                           $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                           $"and possible Type '{objType.Name}' does not provide a 'isTypeOf' function. " +
                            "There is no way to resolve this possible type during execution.");
                    }

                    union.AddPossibleType(objType!);
                }
            }
        }

        private void HandleField(IComplexGraphType? parentType, FieldType field, TypeCollectionContext context, bool applyNameConverter)
        {
            // applyNameConverter will be false while processing the three root introspection query fields: __schema, __type, and __typename
            //
            // During processing of those three root fields, the NameConverter will be set to the schema's selected NameConverter,
            //   and the field names must not be processed by the NameConverter
            //
            // For other introspection types and fields, the NameConverter will be set to CamelCaseNameConverter at the time this
            //   code executes, and applyNameConverter will be true
            //
            // For any other fields, the NameConverter will be set to the schema's selected NameConverter at the time this code
            //   executes, and applyNameConverter will be true

            if (applyNameConverter)
            {
                field.Name = _nameConverter.NameForField(field.Name, parentType!);
                NameValidator.ValidateNameOnSchemaInitialize(field.Name, NamedElement.Field);
            }

            if (field.ResolvedType == null)
            {
                if (field.Type == null)
                    throw new InvalidOperationException($"Both ResolvedType and Type properties on field '{parentType?.Name}.{field.Name}' are null.");

                object typeOrError = RebuildType(field.Type, parentType is IInputObjectGraphType, context.ClrToGraphTypeMappings);
                if (typeOrError is string error)
                    throw new InvalidOperationException($"The GraphQL type for field '{parentType?.Name}.{field.Name}' could not be derived implicitly. " + error);
                field.Type = (Type)typeOrError;

                var namedType = AddTypeIfNotRegistered(field.Type, context);
                field.ResolvedType = BuildGraphQLType(field.Type, namedType);
            }
            else
            {
                AddTypeIfNotRegistered(field.ResolvedType, context);
            }

            if (field.Arguments?.Count > 0)
            {
                using var _ = context.Trace("Loop for arguments of field '{0}'", field.Name);
                foreach (var arg in field.Arguments.List!)
                {
                    using var __ = context.Trace("Argument '{0}'", arg.Name);

                    if (applyNameConverter)
                    {
                        arg.Name = _nameConverter.NameForArgument(arg.Name, parentType!, field);
                        NameValidator.ValidateNameOnSchemaInitialize(arg.Name, NamedElement.Argument);
                    }

                    if (arg.ResolvedType == null)
                    {
                        if (arg.Type == null)
                            throw new InvalidOperationException($"Both ResolvedType and Type properties on argument '{parentType?.Name}.{field.Name}.{arg.Name}' are null.");

                        object typeOrError = RebuildType(arg.Type, true, context.ClrToGraphTypeMappings);
                        if (typeOrError is string error)
                            throw new InvalidOperationException($"The GraphQL type for argument '{parentType?.Name}.{field.Name}.{arg.Name}' could not be derived implicitly. " + error);
                        arg.Type = (Type)typeOrError;

                        var namedType = AddTypeIfNotRegistered(arg.Type, context);
                        arg.ResolvedType = BuildGraphQLType(arg.Type, namedType);
                    }
                    else
                    {
                        AddTypeIfNotRegistered(arg.ResolvedType, context);
                    }
                }
            }
        }

        private void HandleDirective(Directive directive, TypeCollectionContext context)
        {
            if (directive.Arguments?.Count > 0)
            {
                using var _ = context.Trace("Loop for arguments of directive '{0}'", directive.Name);
                foreach (var arg in directive.Arguments.List!)
                {
                    using var __ = context.Trace("Argument '{0}'", arg.Name);
                    if (arg.ResolvedType == null)
                    {
                        if (arg.Type == null)
                            throw new InvalidOperationException($"Both ResolvedType and Type properties on argument '{directive.Name}.{arg.Name}' are null.");

                        object typeOrError = RebuildType(arg.Type, true, context.ClrToGraphTypeMappings);
                        if (typeOrError is string error)
                            throw new InvalidOperationException($"The GraphQL type for argument '{directive.Name}.{arg.Name}' could not be derived implicitly. " + error);
                        arg.Type = (Type)typeOrError;

                        var namedType = AddTypeIfNotRegistered(arg.Type, context);
                        arg.ResolvedType = BuildGraphQLType(arg.Type, namedType);
                    }
                    else
                    {
                        AddTypeIfNotRegistered(arg.ResolvedType, context);
                        arg.ResolvedType = ConvertTypeReference(directive, arg.ResolvedType);
                    }
                }
            }
        }

        // https://github.com/graphql-dotnet/graphql-dotnet/pull/1010
        private void AddTypeWithLoopCheck(IGraphType resolvedType, TypeCollectionContext context, Type namedType)
        {
            if (context.InFlightRegisteredTypes.Any(t => t == namedType))
            {
                throw new InvalidOperationException($@"A loop has been detected while registering schema types.
There was an attempt to re-register '{namedType.FullName}' with instance of '{resolvedType.GetType().FullName}'.
Make sure that your ServiceProvider is configured correctly.");
            }

            context.InFlightRegisteredTypes.Push(namedType);
            try
            {
                using var _ = context.Trace("AddTypeWithLoopCheck for type '{0}'", namedType.Name);
                AddType(resolvedType, context);
            }
            finally
            {
                context.InFlightRegisteredTypes.Pop();
            }
        }

        private IGraphType AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
        {
            using var _ = context.Trace("AddTypeIfNotRegistered(Type, TypeCollectionContext) for type '{0}'", type.Name);
            var namedType = type.GetNamedType();
            var foundType = FindGraphType(namedType);
            if (foundType == null)
            {
                if (namedType == typeof(PageInfoType))
                {
                    foundType = new PageInfoType();
                    AddType(foundType, context);
                }
                else if (namedType.IsGenericType && (namedType.ImplementsGenericType(typeof(EdgeType<>)) || namedType.ImplementsGenericType(typeof(ConnectionType<,>))))
                {
                    foundType = (IGraphType)Activator.CreateInstance(namedType)!;
                    AddType(foundType, context);
                }
                else if (_builtInCustomScalars.TryGetValue(namedType, out var builtInCustomScalar))
                {
                    foundType = builtInCustomScalar;
                    AddType(foundType, _context); // TODO: why _context instead of context here? See https://github.com/graphql-dotnet/graphql-dotnet/pull/3488
                }
                else
                {
                    foundType = context.ResolveType(namedType);
                    AddTypeWithLoopCheck(foundType, context, namedType);
                }
            }
            return foundType;
        }

        private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            namedType ??= context.ResolveType(namedType2!);

            using var _ = context.Trace("AddTypeIfNotRegistered(IGraphType, TypeCollectionContext) for type '{0}'", namedType.Name);

            var existingType = this[namedType.Name];
            if (existingType is null)
            {
                AddType(namedType, context);
            }
            else
            {
                EnsureTypeEquality(existingType, namedType, context);
            }
        }

        private void EnsureTypeEquality(IGraphType existingType, IGraphType newType, TypeCollectionContext context)
        {
            if (ReferenceEquals(existingType, newType))
            {
                return;
            }

            // Ignore scalars
            if (existingType is ScalarGraphType && newType is ScalarGraphType)
            {
                return;
            }

            if (existingType.GetType() != newType.GetType())
            {
                throw new InvalidOperationException($"Unable to register GraphType '{newType.GetType().GetFriendlyName()}' with the name '{newType.Name}'. The name '{newType.Name}' is already registered to '{existingType.GetType().GetFriendlyName()}'. Check your schema configuration.");
            }

            // All other types are considered "potentially wrong" when being re-registered, throw detailed exception
            throw new InvalidOperationException(ErrorMessage());

            string ErrorMessage()
            {
                string error = $"A different instance of the GraphType '{newType.GetType().GetFriendlyName()}' with the name '{newType.Name}' has already been registered within the schema. Please use the same instance for all references within the schema, or use {nameof(GraphQLTypeReference)} to reference a type instantiated elsewhere.";
                var traceInfo = $"To view additional trace enable {nameof(GlobalSwitches)}.{nameof(GlobalSwitches.TrackGraphTypeInitialization)} switch.";
                if (GlobalSwitches.TrackGraphTypeInitialization)
                {
                    var trace1 = $"Existing type trace:{Environment.NewLine}{Environment.NewLine}{existingType.GetMetadata<string>(INITIALIZATIION_TRACE_KEY)}";
                    var trace2 = $"New type trace:{Environment.NewLine}{Environment.NewLine}{(context.InitializationTrace == null ? "" : string.Join(Environment.NewLine, context.InitializationTrace))}";
                    traceInfo = $"{trace1}{Environment.NewLine}{Environment.NewLine}{trace2}";
                }

                return $"{error}{Environment.NewLine}{traceInfo}";
            }
        }

        private object RebuildType(Type type, bool input, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
        {
            if (!type.IsGenericType)
                return type;

            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(GraphQLClrOutputTypeReference<>) || genericDef == typeof(GraphQLClrInputTypeReference<>))
            {
                return GetGraphType(type.GetGenericArguments()[0], input, typeMappings);
            }
            else
            {
                var typeList = type.GetGenericArguments();
                var changed = false;
                for (var i = 0; i < typeList.Length; i++)
                {
                    object typeOrError = RebuildType(typeList[i], input, typeMappings);
                    if (typeOrError is string)
                        return typeOrError;
                    var changedType = (Type)typeOrError;
                    changed |= changedType != typeList[i];
                    typeList[i] = changedType;
                }
                return changed ? genericDef.MakeGenericType(typeList) : type;
            }
        }

        private object GetGraphType(Type clrType, bool input, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
        {
            var ret = GetGraphTypeFromClrType(clrType, input, typeMappings);

            if (ret == null)
            {
                string additionalMessage = typeof(IGraphType).IsAssignableFrom(clrType)
                    ? $" Note that '{clrType.FullName}' is already a GraphType (i.e. not CLR type like System.DateTime or System.String). Most likely you need to specify corresponding CLR type instead of GraphType."
                    : "";
                return $"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?{additionalMessage}";
            }

            return ret;
        }

        /// <summary>
        /// Returns a graph type for a specified input or output CLR type.
        /// This method is called when a graph type is specified as a <see cref="GraphQLClrInputTypeReference{T}"/> or <see cref="GraphQLClrOutputTypeReference{T}"/>.
        /// </summary>
        /// <param name="clrType">The CLR type to be mapped.</param>
        /// <param name="isInputType">Indicates if the CLR type should be mapped to an input or output graph type.</param>
        /// <param name="typeMappings">The list of registered type mappings on the schema.</param>
        /// <returns>The graph type to be used, or <see langword="null"/> if no match can be found.</returns>
        /// <remarks>
        /// This method should not return wrapped types such as <see cref="ListGraphType"/> or <see cref="NonNullGraphType"/>.
        /// These are handled within <see cref="GraphQL.TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>,
        /// and should already have been wrapped around the type reference.
        /// </remarks>
        protected virtual Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
        {
            Type? mappedType = null;

            // check custom mappings first
            if (typeMappings != null)
            {
                foreach (var mapping in typeMappings)
                {
                    mappedType = mapping.GetGraphTypeFromClrType(clrType, isInputType, mappedType);
                }
            }

            if (mappedType != null)
                return mappedType;

            // then built-in mappings
            if (BuiltInScalarMappings.TryGetValue(clrType, out var graphType))
                return graphType;

            // create an enumeration graph type if applicable
            if (clrType.IsEnum)
                return typeof(EnumerationGraphType<>).MakeGenericType(clrType);

            return null;
        }

        private void ApplyTypeReferences()
        {
            // ToList() is a necessary measure here since otherwise we get System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'
            foreach (var type in Dictionary.Values.ToList())
            {
                ApplyTypeReference(type);
            }
        }

        private void ApplyTypeReference(IGraphType type)
        {
            if (type is IComplexGraphType complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    field.ResolvedType = ConvertTypeReference(type, field.ResolvedType!);

                    if (field.Arguments?.Count > 0)
                    {
                        foreach (var arg in field.Arguments.List!)
                        {
                            arg.ResolvedType = ConvertTypeReference(type, arg.ResolvedType!);
                        }
                    }
                }
            }

            if (type is IObjectGraphType objectType)
            {
                var list = objectType.ResolvedInterfaces.List;
                for (int i = 0; i < list.Count; ++i)
                {
                    var interfaceType = (IInterfaceGraphType)ConvertTypeReference(objectType, list[i]);

                    if (objectType.IsTypeOf == null && interfaceType.ResolveType == null)
                    {
                        throw new InvalidOperationException(
                               $"Interface type '{interfaceType.Name}' does not provide a 'resolveType' function " +
                               $"and possible Type '{objectType.Name}' does not provide a 'isTypeOf' function.  " +
                                "There is no way to resolve this possible type during execution.");
                    }

                    interfaceType.AddPossibleType(objectType);

                    list[i] = interfaceType;
                }
            }

            if (type is UnionGraphType union)
            {
                var list = union.PossibleTypes.List;
                for (int i = 0; i < list.Count; ++i)
                {
                    var unionType = ConvertTypeReference(union, list[i]) as IObjectGraphType;

                    if (union.ResolveType == null && unionType != null && unionType.IsTypeOf == null)
                    {
                        throw new InvalidOperationException(
                           $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                           $"and possible Type '{union.Name}' does not provide a 'isTypeOf' function. " +
                            "There is no way to resolve this possible type during execution.");
                    }

                    list[i] = unionType!;
                }
            }
        }

        private IGraphType ConvertTypeReference(INamedType parentType, IGraphType type)
        {
            if (type is NonNullGraphType nonNull)
            {
                nonNull.ResolvedType = ConvertTypeReference(parentType, nonNull.ResolvedType!);
                return nonNull;
            }

            if (type is ListGraphType list)
            {
                list.ResolvedType = ConvertTypeReference(parentType, list.ResolvedType!);
                return list;
            }

            if (type is GraphQLTypeReference reference)
            {
                var type2 = this[reference.TypeName];
                if (type2 == null)
                {
                    type2 = _builtInScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName) ?? _builtInCustomScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName);
                    if (type2 != null)
                        SetGraphType(type2.Name, type2);
                }
                if (type2 == null)
                {
                    throw new InvalidOperationException($"Unable to resolve reference to type '{reference.TypeName}' on '{parentType.Name}'");
                }
                type = type2;
            }

            return type;
        }

        private void SetGraphType(string typeName, IGraphType graphType)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
            }

            var type = graphType.GetType();
            if (Dictionary.TryGetValue(typeName, out var existingGraphType))
            {
                if (ReferenceEquals(existingGraphType, graphType) || existingGraphType.GetType() == type)
                {
                    // Soft schema configuration error.
                    // Intentionally or inadvertently, a situation may arise when the same GraphType is registered more that one time.
                    // This may be due to the simultaneous registration of GraphType instances and the GraphType types. In this case
                    // the duplicate MUST be ignored, otherwise errors will occur.
                }
                else if (type.IsAssignableFrom(existingGraphType.GetType()) && typeof(ScalarGraphType).IsAssignableFrom(type))
                {
                    // This can occur when a built-in scalar graph type is overridden by preregistering a replacement graph type that
                    // has the same name and inherits from it.

                    if (!_typeDictionary.ContainsKey(type))
                        _typeDictionary.Add(type, existingGraphType);
                }
                else
                {
                    // Fatal schema configuration error.
                    throw new InvalidOperationException($@"Unable to register GraphType '{type.GetFriendlyName()}' with the name '{typeName}'. The name '{typeName}' is already registered to '{existingGraphType.GetType().GetFriendlyName()}'. Check your schema configuration.");
                }
            }
            else
            {
                Dictionary.Add(typeName, graphType);
                // if building a schema from code, the .NET types will not be unique, which should be ignored
                if (!_typeDictionary.ContainsKey(type))
                    _typeDictionary.Add(type, graphType);
            }
        }

        private void InheritInterfaceDescriptions()
        {
            foreach (var fieldOwner in Dictionary.Values.OfType<IComplexGraphType>())
            {
                if (fieldOwner is IImplementInterfaces implementation && implementation.ResolvedInterfaces.Count > 0)
                {
                    foreach (var field in fieldOwner.Fields.Where(field => field.Description == null))
                    {
                        foreach (var iface in implementation.ResolvedInterfaces.List)
                        {
                            var fieldFromInterface = iface.GetField(field.Name);
                            if (fieldFromInterface?.Description != null)
                            {
                                field.Description = fieldFromInterface.Description;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__schema</c> meta-field.
        /// </summary>
        protected internal virtual FieldType SchemaMetaFieldType { get; } = new SchemaMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__type</c> meta-field.
        /// </summary>
        protected internal virtual FieldType TypeMetaFieldType { get; } = new TypeMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__typename</c> meta-field.
        /// </summary>
        protected internal virtual FieldType TypeNameMetaFieldType { get; } = new TypeNameMetaFieldType();
    }
}
