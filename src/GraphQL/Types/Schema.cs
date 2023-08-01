using System.Diagnostics;
using System.Runtime.ExceptionServices;
using GraphQL.Conversion;
using GraphQL.DI;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <inheritdoc cref="ISchema"/>
    [DebuggerTypeProxy(typeof(SchemaDebugView))]
    public class Schema : MetadataProvider, ISchema, IServiceProvider, IDisposable
    {
        private sealed class SchemaDebugView
        {
            private readonly Schema _schema;

            public SchemaDebugView(Schema schema)
            {
                _schema = schema;
            }

            public Dictionary<string, object?> Metadata => _schema.Metadata;

            public ExperimentalFeatures Features => _schema.Features;

            public INameConverter NameConverter => _schema.NameConverter;

            public IFieldMiddlewareBuilder FieldMiddleware => _schema.FieldMiddleware;

            public bool Initialized => _schema.Initialized;

            public string? Description => _schema.Description;

            public IObjectGraphType Query => _schema.Query;

            public IObjectGraphType? Mutation => _schema.Mutation;

            public IObjectGraphType? Subscription => _schema.Subscription;

            public ISchemaFilter Filter => _schema.Filter;

            /// <inheritdoc/>
            public ISchemaComparer Comparer => _schema.Comparer;

            /// <inheritdoc/>
            public SchemaDirectives Directives => _schema.Directives;

            /// <inheritdoc/>
            public SchemaTypes? AllTypes => _schema._allTypes;

            public string AllTypesMessage => _schema._allTypes == null ? "AllTypes property too early initialization was suppressed to prevent unforeseen consequences. You may click Raw View in debugger window to evaluate all properties." : string.Empty;

            public IEnumerable<Type> AdditionalTypes => _schema.AdditionalTypes;

            public IEnumerable<IGraphType> AdditionalTypeInstances => _schema.AdditionalTypeInstances;

            /// <inheritdoc/>
            public FieldType? SchemaMetaFieldType => AllTypes?.SchemaMetaFieldType;

            /// <inheritdoc/>
            public FieldType? TypeMetaFieldType => AllTypes?.TypeMetaFieldType;

            /// <inheritdoc/>
            public FieldType? TypeNameMetaFieldType => AllTypes?.TypeNameMetaFieldType;

            public IEnumerable<(Type clrType, Type graphType)> TypeMappings => _schema.TypeMappings;

            /// <inheritdoc/>
            public IEnumerable<(Type clrType, Type graphType)> BuiltInTypeMappings => _schema.BuiltInTypeMappings;
        }

        private bool _disposed;
        private IServiceProvider _services;
        private SchemaTypes? _allTypes;
        private ExceptionDispatchInfo? _initializationException;
        private readonly object _allTypesInitializationLock = new();

        private List<Type>? _additionalTypes;
        private List<IGraphType>? _additionalInstances;

        private List<Type>? _visitorTypes;
        private List<ISchemaNodeVisitor>? _visitors;

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with the <see cref="DefaultServiceProvider"/>, which
        /// uses <see cref="Activator.CreateInstance(Type)"/> to create required objects.
        /// </summary>
        public Schema()
            : this(new DefaultServiceProvider())
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with a specified <see cref="IServiceProvider"/>, used
        /// to create required objects.
        /// Pulls registered <see cref="IConfigureSchema"/> instances from <paramref name="services"/> and
        /// executes them.
        /// </summary>
        public Schema(IServiceProvider services)
            : this(services, true)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with a specified <see cref="IServiceProvider"/>, used
        /// to create required objects.
        /// If <paramref name="runConfigurations"/> is <see langword="true"/>, pulls registered
        /// <see cref="IConfigureSchema"/> instances from <paramref name="services"/> and executes them.
        /// </summary>
        public Schema(IServiceProvider services, bool runConfigurations = true)
            : this(services, (runConfigurations ? services.GetService(typeof(IEnumerable<IConfigureSchema>)) as IEnumerable<IConfigureSchema> : null)!)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with a specified <see cref="IServiceProvider"/>, used
        /// to create required objects.
        /// Executes the specified <see cref="IConfigureSchema"/> instances on the schema, if any.
        /// </summary>
        public Schema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
        {
            _services = services;

            Directives = new SchemaDirectives();
            Directives.Register(Directives.Include, Directives.Skip, Directives.Deprecated);

            foreach (var configuration in configurations ?? Array.Empty<IConfigureSchema>())
            {
                configuration.Configure(this, services);
            }
        }

        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For(string typeDefinitions, Action<SchemaBuilder>? configure = null)
            => For<SchemaBuilder>(typeDefinitions, configure);

        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <typeparam name="TSchemaBuilder">The type of <see cref="SchemaBuilder"/> that will create the schema.</typeparam>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For<TSchemaBuilder>(string typeDefinitions, Action<TSchemaBuilder>? configure = null)
            where TSchemaBuilder : SchemaBuilder, new()
        {
            var builder = new TSchemaBuilder();
            configure?.Invoke(builder);
            return builder.Build(typeDefinitions);
        }

        /// <inheritdoc/>
        public ExperimentalFeatures Features { get; set; } = new ExperimentalFeatures();

        /// <inheritdoc/>
        public INameConverter NameConverter { get; set; } = CamelCaseNameConverter.Instance;

        /// <inheritdoc/>
        public IFieldMiddlewareBuilder FieldMiddleware { get; internal set; } = new FieldMiddlewareBuilder();

        /// <inheritdoc/>
        public bool Initialized { get; private set; }

        // TODO: It would be worthwhile to think at all about how to redo the design so that such a situation does not arise.
        private void CheckInitialized([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (Initialized)
                throw new InvalidOperationException($"Schema is already initialized and sealed for modifications. You should call '{name}' only when Schema.Initialized = false.");
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            CheckDisposed();

            if (Initialized)
                return;

            lock (_allTypesInitializationLock)
            {
                if (Initialized)
                    return;

                _initializationException?.Throw();

                try
                {
                    CreateAndInitializeSchemaTypes();

                    Initialized = true;
                }
                catch (Exception ex)
                {
                    _initializationException = ExceptionDispatchInfo.Capture(ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType Query { get; set; } = null!;

        /// <inheritdoc/>
        public IObjectGraphType? Mutation { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType? Subscription { get; set; }

        /// <summary>
        /// Gets the service object of the specified type. Schema itself acts as a service provider used to
        /// create objects, such as graph types, requested by the schema.
        /// <br/><br/>
        /// Note that most objects are created during schema initialization, which then have the same lifetime
        /// as the schema's lifetime.
        /// <br/><br/>
        /// Other types created by the service provider may include directive visitors, middlewares, validation
        /// rules, and name converters, among others.
        /// <br/><br/>
        /// Explicit implementation of the <see cref="IServiceProvider.GetService"/> method makes this method
        /// less visible to the calling code, which reduces the likelihood of using it as so called ServiceLocator
        /// anti-pattern. However, in some advanced scenarios this may be necessary.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/> or <see langword="null"/> if there is no service
        /// object of type serviceType.
        /// </returns>
        object? IServiceProvider.GetService(Type serviceType) => _services.GetService(serviceType);

        /// <inheritdoc/>
        public ISchemaFilter Filter { get; set; } = new DefaultSchemaFilter();

        /// <inheritdoc/>
        public ISchemaComparer Comparer { get; set; } = new DefaultSchemaComparer();

        /// <inheritdoc/>
        public SchemaDirectives Directives { get; }

        /// <inheritdoc/>
        public SchemaTypes AllTypes
        {
            get
            {
                if (_allTypes == null)
                    Initialize();

                return _allTypes!;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Type> AdditionalTypes => _additionalTypes ?? Enumerable.Empty<Type>();

        /// <inheritdoc/>
        public IEnumerable<IGraphType> AdditionalTypeInstances => _additionalInstances ?? Enumerable.Empty<IGraphType>();

        /// <inheritdoc/>
        public FieldType SchemaMetaFieldType => AllTypes.SchemaMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeMetaFieldType => AllTypes.TypeMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeNameMetaFieldType => AllTypes.TypeNameMetaFieldType;

        /// <inheritdoc/>
        public void RegisterVisitor(ISchemaNodeVisitor visitor)
        {
            CheckDisposed();
            CheckInitialized();

            (_visitors ??= new()).Add(visitor ?? throw new ArgumentNullException(nameof(visitor)));
        }

        /// <inheritdoc/>
        public void RegisterVisitor(Type type)
        {
            CheckDisposed();
            CheckInitialized();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(ISchemaNodeVisitor).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Type must be of {nameof(ISchemaNodeVisitor)}.");
            }

            if (!(_visitorTypes ??= new()).Contains(type))
                _visitorTypes.Add(type);
        }

        /// <inheritdoc/>
        public void RegisterType(IGraphType type)
        {
            CheckDisposed();
            CheckInitialized();

            (_additionalInstances ??= new()).Add(type ?? throw new ArgumentNullException(nameof(type)));
        }

        /// <inheritdoc/>
        public void RegisterType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            CheckDisposed();
            CheckInitialized();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of IGraphType.");
            }

            _additionalTypes ??= new();

            if (!_additionalTypes.Contains(type))
                _additionalTypes.Add(type);
        }

        /// <inheritdoc/>
        public void RegisterTypes(params Type[] types)
        {
            CheckDisposed();
            CheckInitialized();

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                RegisterType(type);
            }
        }

        private List<(Type clrType, Type graphType)>? _clrToGraphTypeMappings;

        /// <inheritdoc/>
        public void RegisterTypeMapping(
            Type clrType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type graphType)
        {
            (_clrToGraphTypeMappings ??= new()).Add((
                CheckClrType(clrType ?? throw new ArgumentNullException(nameof(clrType))),
                CheckGraphType(graphType ?? throw new ArgumentNullException(nameof(graphType)))
            ));

            Type CheckClrType(Type clrType)
            {
                return typeof(IGraphType).IsAssignableFrom(clrType)
                    ? throw new ArgumentOutOfRangeException(nameof(clrType), $"{clrType.FullName}' is already a GraphType (i.e. not CLR type like System.DateTime or System.String). You must specify CLR type instead of GraphType.")
                    : clrType;
            }

            Type CheckGraphType(Type graphType)
            {
                return typeof(IGraphType).IsAssignableFrom(graphType)
                    ? graphType
                    : throw new ArgumentOutOfRangeException(nameof(graphType), $"{graphType.FullName}' must be a GraphType (i.e. not CLR type like System.DateTime or System.String). You must specify GraphType type instead of CLR type.");
            }
        }

        /// <inheritdoc/>
        public IEnumerable<(Type clrType, Type graphType)> TypeMappings => _clrToGraphTypeMappings ?? Enumerable.Empty<(Type, Type)>();

        /// <inheritdoc/>
        public IEnumerable<(Type clrType, Type graphType)> BuiltInTypeMappings
        {
            get
            {
                foreach (var pair in SchemaTypes.BuiltInScalarMappings)
                    yield return (pair.Key, pair.Value);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _services = null!;
                    Query = null!;
                    Mutation = null;
                    Subscription = null;
                    Filter = null!;

                    _additionalInstances?.Clear();
                    _additionalTypes?.Clear();
                    Directives.List.Clear();
                    _visitors?.Clear();
                    _visitorTypes?.Clear();

                    _allTypes?.Dictionary.Clear();
                    _allTypes = null;

                    _disposed = true;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Schema));
        }

        private void CreateAndInitializeSchemaTypes()
        {
            IEnumerable<ISchemaNodeVisitor> GetVisitors()
            {
                if (_visitors != null)
                {
                    foreach (var visitor in _visitors)
                        yield return visitor;
                }

                if (_visitorTypes != null)
                {
                    foreach (var type in _visitorTypes)
                        yield return (ISchemaNodeVisitor)_services.GetRequiredService(type);
                }
            }

            _allTypes = CreateSchemaTypes();

            try
            {
                // At this point, Initialized will return false, and Initialize will still lock while waiting for initialization to complete.
                // However, AllTypes and similar properties will return a reference to SchemaTypes without waiting for a lock.
                _allTypes.ApplyMiddleware(FieldMiddleware);

                foreach (var visitor in GetVisitors())
                    visitor.Run(this);

                Validate();
            }
            catch
            {
                _allTypes = null;
                throw;
            }
        }

        /// <summary>
        /// Creates and returns a new instance of <see cref="SchemaTypes"/> for this schema.
        /// Does not apply middleware, apply schema visitors, or validate the schema.
        /// </summary>
        /// <remarks>
        /// This executes within a lock in <see cref="Initialize"/>.
        /// </remarks>
        protected virtual SchemaTypes CreateSchemaTypes()
        {
            return new SchemaTypes(this, _services);
        }

        /// <summary>
        /// Validates correctness of the created schema. This method is called only once - during schema initialization.
        /// </summary>
        protected virtual void Validate()
        {
            //TODO: add different validations, also see SchemaBuilder.Validate
            //TODO: checks for parsed SDL may be expanded in the future, see https://github.com/graphql/graphql-spec/issues/653
            // Do not change the order of these validations.
            CoerceInputTypeDefaultValues();
            SchemaValidationVisitor.Instance.Run(this);
            AppliedDirectivesValidationVisitor.Instance.Run(this);
            if (!GlobalSwitches.IsDynamicCodeSupported)
                AotValidationVisitor.Instance.Run(this);
        }

        /// <summary>
        /// Coerces input types' default values when those values are <see cref="GraphQLValue"/> nodes.
        /// This is applicable when the <see cref="SchemaBuilder"/> is used to build the schema.
        /// </summary>
        protected virtual void CoerceInputTypeDefaultValues()
        {
            var completed = new HashSet<IInputObjectGraphType>();
            var inProcess = new Stack<IInputObjectGraphType>();
            foreach (var type in AllTypes.Dictionary.Values)
            {
                if (type is IInputObjectGraphType inputType)
                    ExamineType(inputType, completed, inProcess);
            }

            static void ExamineType(IInputObjectGraphType inputType, HashSet<IInputObjectGraphType> completed, Stack<IInputObjectGraphType> inProcess)
            {
                if (completed.Contains(inputType))
                    return;
                if (inProcess.Contains(inputType))
                    throw new InvalidOperationException($"Default values in input types cannot contain a circular dependency loop. Please resolve dependency loop between the following types: {string.Join(", ", inProcess.Select(x => $"'{x.Name}'"))}.");
                inProcess.Push(inputType);
                foreach (var field in inputType.Fields)
                {
                    if (field.DefaultValue is GraphQLValue value)
                    {
                        var baseType = field.ResolvedType!.GetNamedType();
                        if (baseType is IInputObjectGraphType inputFieldType)
                            ExamineType(inputFieldType, completed, inProcess);
                        field.DefaultValue = Execution.ExecutionHelper.CoerceValue(field.ResolvedType!, value).Value;
                    }
                }
                inProcess.Pop();
                completed.Add(inputType);
            }
        }
    }
}
