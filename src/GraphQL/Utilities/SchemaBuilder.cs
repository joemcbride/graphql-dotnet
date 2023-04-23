using System.Diagnostics;
using GraphQL.DI;
using GraphQL.Reflection;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Builds schema from string.
    /// </summary>
    public class SchemaBuilder
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected readonly Dictionary<string, IGraphType> _types = new();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        private GraphQLSchemaDefinition? _schemaDef;

        private IgnoreOptions CreateIgnoreOptions()
        {
            var options = IgnoreOptions.None;
            if (IgnoreComments)
                options |= IgnoreOptions.Comments;
            if (IgnoreLocations)
                options |= IgnoreOptions.Locations;
            return options;
        }

        /// <summary>
        /// This <see cref="IServiceProvider"/> is used to create required objects during building schema.
        /// <br/><br/>
        /// By default equals to <see cref="DefaultServiceProvider"/>.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; } = new DefaultServiceProvider();

        /// <summary>
        /// Specifies whether to ignore comments when parsing GraphQL document.
        /// By default, all comments are ignored.
        /// </summary>
        public bool IgnoreComments { get; set; } = true;

        /// <summary>
        /// Specifies whether to ignore token locations when parsing GraphQL document.
        /// By default, all token locations are taken into account.
        /// </summary>
        public bool IgnoreLocations { get; set; }

        /// <summary>
        /// Allows to successfully build the schema even if types are found that are not registered int <see cref="Types"/>.
        /// <br/>
        /// By default <see langword="true"/>.
        /// </summary>
        public bool AllowUnknownTypes { get; set; } = true;

        /// <summary>
        /// Allows to successfully build the schema even if fields are found that have no resolvers.
        /// <br/>
        /// By default <see langword="true"/>.
        /// </summary>
        public bool AllowUnknownFields { get; set; } = true;

        /// <inheritdoc cref="TypeSettings" />
        public TypeSettings Types { get; } = new TypeSettings();

        /// <summary>
        /// If <see langword="true"/>, pulls registered <see cref="IConfigureSchema"/>
        /// instances from <see cref="ServiceProvider"/> and executes them.
        /// </summary>
        public bool RunConfigurations { get; set; } = true;

        /// <summary>
        /// Builds schema from string.
        /// </summary>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <returns>Created schema.</returns>
        public virtual Schema Build(string typeDefinitions)
        {
            var document = Parser.Parse(typeDefinitions, new ParserOptions { Ignore = CreateIgnoreOptions() });
            Validate(document);
            return BuildSchemaFrom(document);
        }

        /// <summary>
        /// Validate the specified SDL.
        /// </summary>
        protected virtual void Validate(GraphQLDocument document)
        {
            var definitionsByName = document.Definitions.OfType<GraphQLTypeDefinition>().ToLookup(def => def.Name!.Value);
            var duplicates = definitionsByName.Where(grouping => grouping.Count() > 1).ToArray();
            if (duplicates.Length > 0)
            {
                throw new ArgumentException(@$"All types within a GraphQL schema must have unique names. No two provided types may have the same name.
Schema contains a redefinition of these types: {string.Join(", ", duplicates.Select(item => item.Key))}", nameof(document));
            }

            //TODO: checks for parsed SDL may be expanded in the future, see https://github.com/graphql/graphql-spec/issues/653
            // Also see Schema.Validate
        }

        /// <summary>
        /// Returns a new <see cref="Schema"/> instance.
        /// </summary>
        protected virtual Schema CreateSchema() => new(ServiceProvider, runConfigurations: RunConfigurations);

        private Schema BuildSchemaFrom(GraphQLDocument document)
        {
            var schema = CreateSchema();

            PreConfigure(schema);

            var directives = new List<Directive>();

            foreach (var def in document.Definitions)
            {
                if (def is GraphQLSchemaDefinition schemaDef)
                {
                    _schemaDef = schemaDef;
                    schema.SetAstType(schemaDef);
                }
                else if (def is GraphQLObjectTypeDefinition objDef)
                {
                    var type = ToObjectGraphType(objDef);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLObjectTypeExtension ext)
                {
                    //TODO: rewrite and add support for other extensions
                    var typeDef = new GraphQLObjectTypeDefinition(ext.Name)
                    {
                        Comments = ext.Comments,
                        Description = null,
                        Directives = ext.Directives,
                        Fields = ext.Fields,
                        Interfaces = ext.Interfaces,
                        Location = ext.Location,
                    };
                    var type = ToObjectGraphType(typeDef, true);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLInterfaceTypeDefinition ifaceDef)
                {
                    var type = ToInterfaceType(ifaceDef);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLEnumTypeDefinition enumDef)
                {
                    var type = ToEnumerationType(enumDef);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLUnionTypeDefinition unionDef)
                {
                    var type = ToUnionType(unionDef);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLInputObjectTypeDefinition inputDef)
                {
                    var type = ToInputObjectType(inputDef);
                    _types[type.Name] = type;
                }
                else if (def is GraphQLDirectiveDefinition directiveDef)
                {
                    var directive = ToDirective(directiveDef);
                    directives.Add(directive);
                }
            }

            if (_schemaDef != null)
            {
                schema.Description = _schemaDef.MergeComments();

                foreach (var operationTypeDef in _schemaDef.OperationTypes!)
                {
                    var typeName = (string)operationTypeDef.Type!.Name; //TODO:alloc
                    var type = GetType(typeName) as IObjectGraphType;

                    switch (operationTypeDef.Operation)
                    {
                        case OperationType.Query:
                            schema.Query = type!;
                            break;

                        case OperationType.Mutation:
                            schema.Mutation = type;
                            break;

                        case OperationType.Subscription:
                            schema.Subscription = type;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Unknown operation type {operationTypeDef.Operation}");
                    }
                }
            }
            else
            {
                schema.Query = (GetType("Query") as IObjectGraphType)!;
                schema.Mutation = GetType("Mutation") as IObjectGraphType;
                schema.Subscription = GetType("Subscription") as IObjectGraphType;
            }

            foreach (var type in _types)
                schema.RegisterType(type.Value);

            foreach (var directive in directives)
                schema.Directives.Register(directive);

            Debug.Assert(schema.Initialized == false);
            return schema;
        }

        /// <summary>
        /// Configures the <paramref name="schema"/> prior to adding any types.
        /// </summary>
        protected virtual void PreConfigure(Schema schema)
        {
        }

        /// <summary>
        /// Returns the graph type built for the specified graph type name.
        /// </summary>
        protected virtual IGraphType? GetType(string name)
        {
            return _types.TryGetValue(name, out var type) ? type : null;
        }

        private bool IsSubscriptionType(ObjectGraphType type)
        {
            var operationDefinition = _schemaDef?.OperationTypes?.FirstOrDefault(o => o.Operation == OperationType.Subscription);
            return operationDefinition == null
                ? type.Name == "Subscription"
                : type.Name == operationDefinition.Type!.Name;
        }

        private void AssertKnownType(TypeConfig typeConfig)
        {
            if (typeConfig.Type == null && !AllowUnknownTypes)
                throw new InvalidOperationException($"Unknown type '{typeConfig.Name}'. Verify that you have configured SchemaBuilder correctly.");
        }

        private void AssertKnownField(FieldConfig fieldConfig, TypeConfig typeConfig)
        {
            if (fieldConfig.Resolver == null && !AllowUnknownFields)
                throw new InvalidOperationException($"Unknown field '{typeConfig.Name}.{fieldConfig.Name}' has no resolver. Verify that you have configured SchemaBuilder correctly.");
        }

        private void OverrideDeprecationReason(IProvideDeprecationReason element, string? reason)
        {
            if (reason != null)
                element.DeprecationReason = reason;
        }

        /// <summary>
        /// Returns an <see cref="IObjectGraphType"/> from the specified <see cref="GraphQLObjectTypeDefinition"/>.
        /// </summary>
        protected virtual IObjectGraphType ToObjectGraphType(GraphQLObjectTypeDefinition astType, bool isExtensionType = false)
        {
            var name = (string)astType.Name; //TODO:alloc
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = _types.TryGetValue(name, out var t)
                ? t as ObjectGraphType ?? throw new InvalidOperationException($"Type '{name} should be ObjectGraphType")
                : new ObjectGraphType { Name = name };

            if (!isExtensionType)
            {
                type.Description = typeConfig.Description ?? astType.Description?.Value.ToString() ?? astType.MergeComments();
                type.IsTypeOf = typeConfig.IsTypeOfFunc;
            }

            typeConfig.CopyMetadataTo(type);

            Func<string, GraphQLFieldDefinition, FieldType> constructFieldType = IsSubscriptionType(type)
                ? ToSubscriptionFieldType
                : ToFieldType;

            if (astType.Fields != null)
            {
                foreach (var f in astType.Fields)
                    type.AddField(constructFieldType(type.Name, f));
            }

            if (astType.Interfaces != null)
            {
                foreach (var i in astType.Interfaces)
                    type.AddResolvedInterface(new GraphQLTypeReference((string)i.Name)); //TODO:alloc
            }

            if (isExtensionType)
            {
                type.AddExtensionAstType(astType);
            }
            else
            {
                type.SetAstType(astType);
                OverrideDeprecationReason(type, typeConfig.DeprecationReason);
            }

            return type;
        }

        private void InitializeField(FieldConfig config, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type? parentType)
        {
            config.ResolverAccessor ??= parentType.ToAccessor(config.Name, ResolverType.Resolver);

            if (config.ResolverAccessor != null)
            {
                config.Resolver = AutoRegisteringHelper.BuildFieldResolver(
                    config.ResolverAccessor.MethodInfo,
                    null, // unknown source type
                    null, // unknown FieldType
                    AutoRegisteringHelper.BuildInstanceExpressionForSchemaBuilder(config.ResolverAccessor.DeclaringType, ServiceProvider));

                var attrs = config.ResolverAccessor.GetAttributes<GraphQLAttribute>();
                if (attrs != null)
                {
                    foreach (var a in attrs)
                        a.Modify(config);
                }
            }
        }

        private void InitializeSubscriptionField(FieldConfig config, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type? parentType)
        {
            config.ResolverAccessor ??= parentType.ToAccessor(config.Name, ResolverType.Resolver);
            config.StreamResolverAccessor ??= parentType.ToAccessor(config.Name, ResolverType.StreamResolver);

            if (config.ResolverAccessor != null && config.StreamResolverAccessor != null)
            {
                config.Resolver = AutoRegisteringHelper.BuildFieldResolver(
                    config.ResolverAccessor.MethodInfo,
                    null, // unknown source type
                    null, // unknown FieldType
                    AutoRegisteringHelper.BuildInstanceExpressionForSchemaBuilder(config.ResolverAccessor.DeclaringType, ServiceProvider));

                var attrs = config.ResolverAccessor.GetAttributes<GraphQLAttribute>();
                if (attrs != null)
                {
                    foreach (var a in attrs)
                        a.Modify(config);
                }

                config.StreamResolver = AutoRegisteringHelper.BuildSourceStreamResolver(
                    config.StreamResolverAccessor.MethodInfo,
                    null, // unknown source type
                    null, // unknown FieldType
                    AutoRegisteringHelper.BuildInstanceExpressionForSchemaBuilder(config.ResolverAccessor.DeclaringType, ServiceProvider));
            }
        }

        /// <summary>
        /// Returns a <see cref="FieldType"/> from the specified <see cref="GraphQLFieldDefinition"/>.
        /// </summary>
        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var fieldConfig = typeConfig.FieldFor((string)fieldDef.Name); //TODO:alloc
            InitializeField(fieldConfig, typeConfig.Type);

            AssertKnownField(fieldConfig, typeConfig);

            var field = new FieldType
            {
                Name = fieldConfig.Name,
                Description = fieldConfig.Description ?? fieldDef.Description?.Value.ToString() ?? fieldDef.MergeComments(),
                ResolvedType = ToGraphType(fieldDef.Type!),
                Resolver = fieldConfig.Resolver
            };

            fieldConfig.CopyMetadataTo(field);

            field.Arguments = ToQueryArguments(fieldConfig, fieldDef.Arguments?.Items);

            field.SetAstType(fieldDef);
            OverrideDeprecationReason(field, fieldConfig.DeprecationReason);

            return field;
        }

        /// <summary>
        /// Returns a subscription <see cref="FieldType"/> from the specified <see cref="GraphQLFieldDefinition"/>.
        /// </summary>
        protected virtual FieldType ToSubscriptionFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var fieldConfig = typeConfig.FieldFor((string)fieldDef.Name); //TODO:alloc
            InitializeSubscriptionField(fieldConfig, typeConfig.Type);

            AssertKnownField(fieldConfig, typeConfig);

            var field = new FieldType
            {
                Name = fieldConfig.Name,
                Description = fieldConfig.Description ?? fieldDef.Description?.Value.ToString() ?? fieldDef.MergeComments(),
                ResolvedType = ToGraphType(fieldDef.Type!),
                Resolver = fieldConfig.Resolver,
                StreamResolver = fieldConfig.StreamResolver,
            };

            fieldConfig.CopyMetadataTo(field);

            field.Arguments = ToQueryArguments(fieldConfig, fieldDef.Arguments?.Items);

            field.SetAstType(fieldDef);
            OverrideDeprecationReason(field, fieldConfig.DeprecationReason);

            return field;
        }

        /// <summary>
        /// Returns a <see cref="FieldType"/> from the specified <see cref="GraphQLInputValueDefinition"/>.
        /// </summary>
        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLInputValueDefinition inputDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var fieldConfig = typeConfig.FieldFor((string)inputDef.Name); //TODO:alloc
            InitializeField(fieldConfig, typeConfig.Type);

            AssertKnownField(fieldConfig, typeConfig);

            var field = new FieldType
            {
                Name = fieldConfig.Name,
                Description = fieldConfig.Description ?? inputDef.Description?.Value.ToString() ?? inputDef.MergeComments(),
                ResolvedType = ToGraphType(inputDef.Type!),
                DefaultValue = fieldConfig.DefaultValue ?? inputDef.DefaultValue
            }.SetAstType(inputDef);

            OverrideDeprecationReason(field, fieldConfig.DeprecationReason);

            return field;
        }

        /// <summary>
        /// Returns a <see cref="InterfaceGraphType"/> from the specified <see cref="GraphQLInterfaceTypeDefinition"/>.
        /// </summary>
        protected virtual InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var name = (string)interfaceDef.Name; //TODO:alloc
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new InterfaceGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? interfaceDef.Description?.Value.ToString() ?? interfaceDef.MergeComments(),
                ResolveType = typeConfig.ResolveType,
            }.SetAstType(interfaceDef);

            OverrideDeprecationReason(type, typeConfig.DeprecationReason);

            typeConfig.CopyMetadataTo(type);

            if (interfaceDef.Fields != null)
            {
                foreach (var f in interfaceDef.Fields)
                    type.AddField(ToFieldType(type.Name, f));
            }

            return type;
        }

        /// <summary>
        /// Returns a <see cref="UnionGraphType"/> from the specified <see cref="GraphQLUnionTypeDefinition"/>.
        /// </summary>
        protected virtual UnionGraphType ToUnionType(GraphQLUnionTypeDefinition unionDef)
        {
            var name = (string)unionDef.Name; //TODO:alloc
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new UnionGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? unionDef.Description?.Value.ToString() ?? unionDef.MergeComments(),
                ResolveType = typeConfig.ResolveType,
            }.SetAstType(unionDef);

            OverrideDeprecationReason(type, typeConfig.DeprecationReason);

            typeConfig.CopyMetadataTo(type);

            if (unionDef.Types?.Count > 0) // just in case
            {
                foreach (var x in unionDef.Types)
                {
                    string n = (string)x.Name; //TODO:alloc
                    type.AddPossibleType(((GetType(n) ?? new GraphQLTypeReference(n)) as IObjectGraphType)!);
                }
            }

            return type;
        }

        /// <summary>
        /// Returns an <see cref="InputObjectGraphType"/> from the specified <see cref="GraphQLInputObjectTypeDefinition"/>.
        /// </summary>
        protected virtual InputObjectGraphType ToInputObjectType(GraphQLInputObjectTypeDefinition inputDef)
        {
            var name = (string)inputDef.Name; //TODO:alloc
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new InputObjectGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? inputDef.Description?.Value.ToString() ?? inputDef.MergeComments(),
            }.SetAstType(inputDef);

            OverrideDeprecationReason(type, typeConfig.DeprecationReason);

            typeConfig.CopyMetadataTo(type);

            if (inputDef.Fields != null)
            {
                foreach (var f in inputDef.Fields)
                    type.AddField(ToFieldType(type.Name, f));
            }

            return type;
        }

        /// <summary>
        /// Returns an <see cref="EnumerationGraphType"/> from the specified <see cref="GraphQLEnumTypeDefinition"/>.
        /// </summary>
        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var name = (string)enumDef.Name; //TODO:alloc
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new EnumerationGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? enumDef.Description?.Value.ToString() ?? enumDef.MergeComments(),
            }.SetAstType(enumDef);

            OverrideDeprecationReason(type, typeConfig.DeprecationReason);

            if (enumDef.Values?.Count > 0) // just in case
            {
                foreach (var value in enumDef.Values)
                    type.Add(ToEnumValue(value, typeConfig.Type!));
            }

            return type;
        }

        /// <summary>
        /// Returns a <see cref="Directive"/> from the specified <see cref="GraphQLDirectiveDefinition"/>.
        /// </summary>
        protected virtual Directive ToDirective(GraphQLDirectiveDefinition directiveDef)
        {
            var result = new Directive(directiveDef.Name.StringValue) //ISSUE:allocation
            {
                Description = directiveDef.Description?.Value.ToString() ?? directiveDef.MergeComments(),
                Repeatable = directiveDef.Repeatable,
                Arguments = ToQueryArguments(directiveDef.Arguments?.Items)
            };

            if (directiveDef.Locations.Items.Count > 0) // just in case
            {
                foreach (var location in directiveDef.Locations.Items)
                {
                    result.Locations.Add(location);
                }
            }

            return result;
        }

        private EnumValueDefinition ToEnumValue(GraphQLEnumValueDefinition valDef, Type enumType)
        {
            var name = (string)valDef.Name; //TODO:alloc
            return new EnumValueDefinition(name, enumType == null ? name : Enum.Parse(enumType, name, true))
            {
                Description = valDef.Description?.Value.ToString() ?? valDef.MergeComments()
                // TODO: SchemaFirst configuration (TypeConfig/FieldConfig) does not allow to specify DeprecationReason for enum values
                //DeprecationReason = ???
            }.SetAstType(valDef);
        }

        /// <summary>
        /// Returns a <see cref="QueryArgument"/> from the specified <see cref="GraphQLInputValueDefinition"/>.
        /// </summary>
        protected virtual QueryArgument ToArgument(ArgumentConfig argumentConfig, GraphQLInputValueDefinition inputDef)
        {
            var argument = new QueryArgument(ToGraphType(inputDef.Type!))
            {
                Name = argumentConfig.Name,
                DefaultValue = argumentConfig.DefaultValue ?? inputDef.DefaultValue,
                Description = argumentConfig.Description ?? inputDef.Description?.Value.ToString() ?? inputDef.MergeComments()
            }.SetAstType(inputDef);

            argumentConfig.CopyMetadataTo(argument);

            return argument;
        }

        private IGraphType ToGraphType(GraphQLType astType)
        {
            switch (astType.Kind)
            {
                case ASTNodeKind.NonNullType:
                {
                    var type = ToGraphType(((GraphQLNonNullType)astType).Type!);
                    return new NonNullGraphType(type);
                }

                case ASTNodeKind.ListType:
                {
                    var type = ToGraphType(((GraphQLListType)astType).Type!);
                    return new ListGraphType(type);
                }

                case ASTNodeKind.NamedType:
                {
                    var namedType = (GraphQLNamedType)astType;
                    var name = (string)namedType.Name; //TODO:alloc
                    var type = GetType(name);
                    return type ?? new GraphQLTypeReference(name);
                }

                default:
                    throw new ArgumentOutOfRangeException($"Unknown GraphQL type {astType.Kind}");
            }
        }

        //TODO: add support for directive arguments
        private QueryArguments ToQueryArguments(List<GraphQLInputValueDefinition>? arguments)
        {
            return arguments == null ? new QueryArguments() : new QueryArguments(arguments.Select(a => ToArgument(new ArgumentConfig((string)a.Name), a))); //TODO:alloc
        }

        private QueryArguments ToQueryArguments(FieldConfig fieldConfig, List<GraphQLInputValueDefinition>? arguments)
        {
            return arguments == null ? new QueryArguments() : new QueryArguments(arguments.Select(a => ToArgument(fieldConfig.ArgumentFor((string)a.Name), a))); //TODO:alloc
        }
    }
}
