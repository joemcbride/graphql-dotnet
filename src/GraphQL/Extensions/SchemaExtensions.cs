using System.Reflection;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for schemas.
    /// </summary>
    public static class SchemaExtensions
    {
        /// <summary>
        /// Adds the specified visitor type to the schema. When initializing a schema, all
        /// registered visitors will be executed on each schema element when it is traversed.
        /// </summary>
        public static void RegisterVisitor<TVisitor>(this ISchema schema)
            where TVisitor : ISchemaNodeVisitor
        {
            schema.RegisterVisitor(typeof(TVisitor));
        }

        /// <summary>
        /// Adds the specified graph type to the schema.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        public static void RegisterType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ISchema schema)
            where T : IGraphType
        {
            schema.RegisterType(typeof(T));
        }

        /// <summary>
        /// Adds the specified graph types to the schema. Each type must implement <see cref="IGraphType"/>.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        [RequiresUnreferencedCode("Please ensure that the graph types' constructors are not trimmed by the compiler.")]
        public static TSchema RegisterTypes<TSchema>(this TSchema schema, params Type[] types)
            where TSchema : ISchema
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                schema.RegisterType(type);
            }

            return schema;
        }

        /// <summary>
        /// Adds the specified instances of <see cref="IGraphType"/>s to the schema.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        public static void RegisterTypes<TSchema>(this TSchema schema, params IGraphType[] types)
            where TSchema : ISchema
        {
            foreach (var type in types)
                schema.RegisterType(type);
        }

        /// <summary>
        /// Registers type mapping from CLR type to GraphType.
        /// <br/>
        /// These mappings are used for type inference when constructing fields using expressions:
        /// <br/>
        /// <c>
        /// Field(x => x.Filters);
        /// </c>
        /// </summary>
        /// <typeparam name="TClrType">The CLR property type from which to infer the GraphType.</typeparam>
        /// <typeparam name="TGraphType">Inferred GraphType.</typeparam>
        public static void RegisterTypeMapping<TClrType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(this ISchema schema)
            where TGraphType : IGraphType
        {
            Preserve<GraphQLClrInputTypeReference<TClrType>>();
            Preserve<GraphQLClrOutputTypeReference<TClrType>>();
            schema.RegisterTypeMapping(typeof(TClrType), typeof(TGraphType));
        }

        /// <summary>
        /// Prevents <typeparamref name="T"/> from being trimmed by the linker.
        /// </summary>
        private static void Preserve<T>() => GC.KeepAlive(typeof(T));

        /// <summary>
        /// Registers type mapping from CLR type to <see cref="AutoRegisteringObjectGraphType{T}"/> and/or <see cref="AutoRegisteringInputObjectGraphType{T}"/>.
        /// <br/>
        /// These mappings are used for type inference when constructing fields using expressions:
        /// <br/>
        /// <c>
        /// Field(x => x.Filters);
        /// </c>
        /// </summary>
        /// <param name="schema">The schema for which the mapping is registered.</param>
        /// <param name="clrType">The CLR property type from which to infer the GraphType.</param>
        /// <param name="mode">Which registering mode to use - input only, output only or both.</param>
        [RequiresUnreferencedCode("Please ensure that the CLR type and the related auto-registering graph type are not trimmed by the compiler.")]
        public static void AutoRegister(this ISchema schema, Type clrType, AutoRegisteringMode mode = AutoRegisteringMode.Both)
        {
            if (mode.HasFlag(AutoRegisteringMode.Output))
                schema.RegisterTypeMapping(clrType, typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType));
            if (mode.HasFlag(AutoRegisteringMode.Input))
                schema.RegisterTypeMapping(clrType, typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType));
        }

        /// <summary>
        /// Registers type mapping from CLR type to <see cref="AutoRegisteringObjectGraphType{T}"/> and/or <see cref="AutoRegisteringInputObjectGraphType{T}"/>.
        /// <br/>
        /// These mappings are used for type inference when constructing fields using expressions:
        /// <br/>
        /// <c>
        /// Field(x => x.Filters);
        /// </c>
        /// </summary>
        /// <param name="schema">The schema for which the mapping is registered.</param>
        /// <typeparam name="TClrType">The CLR property type from which to infer the GraphType.</typeparam>
        /// <param name="mode">Which registering mode to use - input only, output only or both.</param>
        public static void AutoRegister<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] TClrType>(this ISchema schema, AutoRegisteringMode mode = AutoRegisteringMode.Both)
        {
            if (mode.HasFlag(AutoRegisteringMode.Output))
                schema.RegisterTypeMapping<TClrType, AutoRegisteringObjectGraphType<TClrType>>();
            if (mode.HasFlag(AutoRegisteringMode.Input))
                schema.RegisterTypeMapping<TClrType, AutoRegisteringInputObjectGraphType<TClrType>>();
        }

        /// <summary>
        /// Scans the calling assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This method uses reflection and therefor is inheritly slow, especially with a large assembly.
        /// When using a scoped schema, it is faster to call
        /// <see cref="GraphQLBuilderExtensions.AddClrTypeMappings(DI.IGraphQLBuilder)"/> as it will precompute
        /// the mappings prior to execution.
        /// </remarks>
        [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
        public static void RegisterTypeMappings(this ISchema schema)
            => schema.RegisterTypeMappings(Assembly.GetCallingAssembly());

        /// <summary>
        /// Scans the specified assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This method uses reflection and therefor is inheritly slow, especially with a large assembly.
        /// When using a scoped schema, it is faster to call
        /// <see cref="GraphQLBuilderExtensions.AddClrTypeMappings(DI.IGraphQLBuilder, Assembly)"/> as it will
        /// precompute the mappings prior to execution.
        /// </remarks>
        [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
        public static void RegisterTypeMappings(this ISchema schema, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var typeMapping in assembly.GetClrTypeMappings())
            {
                schema.RegisterTypeMapping(typeMapping.ClrType, typeMapping.GraphType);
            }
        }

        /// <summary>
        /// Enables some experimental features that are not in the official specification, i.e. ability to expose
        /// user-defined meta-information via introspection. See https://github.com/graphql/graphql-spec/issues/300
        /// for more information. This method must be called before schema initialization.
        /// <br/><br/>
        /// Keep in mind that the implementation of experimental features can change over time, up to their complete
        /// removal, if the official specification is supplemented with all the missing features.
        /// </summary>
        /// <typeparam name="TSchema">Type of the schema.</typeparam>
        /// <param name="schema">The schema for which the features are enabled.</param>
        /// <param name="mode">Experimental features mode.</param>
        /// <returns>Reference to the provided <paramref name="schema"/>.</returns>
        public static TSchema EnableExperimentalIntrospectionFeatures<TSchema>(this TSchema schema, ExperimentalIntrospectionFeaturesMode mode = ExperimentalIntrospectionFeaturesMode.ExecutionOnly)
            where TSchema : ISchema
        {
            if (schema.Initialized)
                throw new InvalidOperationException("Schema is already initialized");

            schema.Features.AppliedDirectives = true;
            schema.Features.RepeatableDirectives = true;
            schema.Features.DeprecationOfInputValues = true;

            if (mode == ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution)
                schema.Filter = new ExperimentalIntrospectionFeaturesSchemaFilter();

            return schema;
        }

        /// <summary>
        /// Executes a GraphQL request with the default <see cref="DocumentExecuter"/>, serializes the result using the specified <see cref="IGraphQLTextSerializer"/>, and returns the result
        /// </summary>
        /// <param name="schema">An instance of <see cref="ISchema"/> to use to execute the query</param>
        /// <param name="serializer">An instance of <see cref="IGraphQLTextSerializer"/> to use to serialize the result</param>
        /// <param name="configure">A delegate which configures the execution options</param>
        public static async Task<string> ExecuteAsync(this ISchema schema, IGraphQLTextSerializer serializer, Action<ExecutionOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var executor = new DocumentExecuter();
            var result = await executor.ExecuteAsync(options =>
            {
                options.Schema = schema;
                configure(options);
            }).ConfigureAwait(false);

            return serializer.Serialize(result);
        }

        /// <summary>
        /// Runs the specified visitor on the specified schema. This method traverses
        /// all the schema elements and calls the appropriate visitor methods.
        /// </summary>
        public static void Run(this ISchemaNodeVisitor visitor, ISchema schema)
        {
            visitor.VisitSchema(schema);

            foreach (var directive in schema.Directives.List)
            {
                visitor.VisitDirective(directive, schema);

                if (directive.Arguments?.Count > 0)
                {
                    foreach (var argument in directive.Arguments.List!)
                        visitor.VisitDirectiveArgumentDefinition(argument, directive, schema);
                }
            }

            foreach (var item in schema.AllTypes.Dictionary)
            {
                switch (item.Value)
                {
                    case EnumerationGraphType e:
                        visitor.VisitEnum(e, schema);
                        foreach (var value in e.Values) //ISSUE:allocation
                            visitor.VisitEnumValue(value, e, schema);
                        break;

                    case ScalarGraphType scalar:
                        visitor.VisitScalar(scalar, schema);
                        break;

                    case UnionGraphType union:
                        visitor.VisitUnion(union, schema);
                        break;

                    case IInterfaceGraphType iface:
                        visitor.VisitInterface(iface, schema);
                        foreach (var field in iface.Fields.List) // List is always non-null
                        {
                            visitor.VisitInterfaceFieldDefinition(field, iface, schema);
                            if (field.Arguments?.Count > 0)
                            {
                                foreach (var argument in field.Arguments.List!)
                                    visitor.VisitInterfaceFieldArgumentDefinition(argument, field, iface, schema);
                            }
                        }
                        break;

                    case IObjectGraphType output:
                        visitor.VisitObject(output, schema);
                        foreach (var field in output.Fields.List) // List is always non-null
                        {
                            visitor.VisitObjectFieldDefinition(field, output, schema);
                            if (field.Arguments?.Count > 0)
                            {
                                foreach (var argument in field.Arguments.List!)
                                    visitor.VisitObjectFieldArgumentDefinition(argument, field, output, schema);
                            }
                        }
                        break;

                    case IInputObjectGraphType input:
                        visitor.VisitInputObject(input, schema);
                        foreach (var field in input.Fields.List) // List is always non-null
                            visitor.VisitInputObjectFieldDefinition(field, input, schema);
                        break;
                }
            }
        }

        /// <summary>
        /// Replaces one scalar in the schema to another with the same name.
        /// </summary>
        /// <typeparam name="TSchema">Type of the schema.</typeparam>
        /// <param name="schema">The schema for which to replace the scalar.</param>
        /// <param name="scalar">New scalar. The replacement occurs by its name.</param>
        /// <returns>Reference to the provided <paramref name="schema"/>.</returns>
        public static TSchema ReplaceScalar<TSchema>(this TSchema schema, ScalarGraphType scalar)
            where TSchema : ISchema
        {
            new ReplaceScalarVisitor(scalar).Run(schema);
            return schema;
        }

        private sealed class ReplaceScalarVisitor : BaseSchemaNodeVisitor
        {
            private readonly ScalarGraphType _replacement;

            public ReplaceScalarVisitor(ScalarGraphType replacement)
            {
                _replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));
            }

            public override void VisitSchema(ISchema schema)
            {
                if (schema.AllTypes.Dictionary.TryGetValue(_replacement.Name, out var type))
                {
                    schema.AllTypes.Dictionary[_replacement.Name] = type is ScalarGraphType
                        ? _replacement
                        : throw new InvalidOperationException($"The scalar should be replaced only by another scalar. You are trying to replace non scalar type '{type.GetType().Name}' with name '{type.Name}' to scalar type '{_replacement.GetType().Name}'.");
                }
            }

            public override void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema) => Replace(argument);

            public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => Replace(field);

            public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema) => Replace(argument);

            public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema) => Replace(field);

            public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => Replace(argument);

            public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => Replace(field);

            private void Replace(IProvideResolvedType provider)
            {
                if (provider.ResolvedType is IProvideResolvedType wrappedProvider)
                    Replace(wrappedProvider);
                else if (provider.ResolvedType is ScalarGraphType scalar && scalar.Name == _replacement.Name)
                    provider.ResolvedType = _replacement;
            }
        }
    }

    /// <summary>
    /// A way to use experimental features.
    /// </summary>
    public enum ExperimentalIntrospectionFeaturesMode
    {
        /// <summary>
        /// Allow experimental features only for client queries but not for standard introspection
        /// request. This means that the client, in response to a standard introspection request,
        /// receives a standard response without any new fields and types. However, client CAN
        /// make requests to the server using the new fields and types. This mode is needed in order
        /// to bypass the problem of tools such as GraphQL Playground, Voyager, GraphiQL that require
        /// a standard response to an introspection request and refuse to work correctly if there are
        /// any unknown fields or types in the response.
        /// </summary>
        ExecutionOnly,

        /// <summary>
        /// Allow experimental features for both standard introspection query and client queries.
        /// This means that the client, in response to a standard introspection request, receives
        /// a response augmented with the new fields and types. Client can make requests to the
        /// server using the new fields and types.
        /// </summary>
        IntrospectionAndExecution
    }

    /// <summary>
    /// Mode used for <see cref="SchemaExtensions.AutoRegister"/> method.
    /// </summary>
    [Flags]
    public enum AutoRegisteringMode
    {
        /// <summary>
        /// Register only input type mapping.
        /// </summary>
        Input = 1,

        /// <summary>
        /// Register only output type mapping.
        /// </summary>
        Output = 2,

        /// <summary>
        /// Register both input and output type mappings.
        /// </summary>
        Both = Input | Output,
    }
}
