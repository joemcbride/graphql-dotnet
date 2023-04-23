using System.Reflection;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
#if NET5_0_OR_GREATER
using GraphQL.Telemetry;
#endif
using GraphQL.Types;
using GraphQL.Types.Collections;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser.AST;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods to configure GraphQL.NET services within a dependency injection framework.
    /// </summary>
    public static class GraphQLBuilderExtensions // TODO: split
    {
        // see matching list in IConfigureExecution.SortOrder xml comments
        internal const float SORT_ORDER_OPTIONS = 100;
        internal const float SORT_ORDER_CONFIGURATION = 200;

        #region - Additional overloads for Register, TryRegister and Configure -
        /// <inheritdoc cref="Register{TService}(IServiceRegister, Func{IServiceProvider, TService}, ServiceLifetime, bool)"/>
        public static IServiceRegister Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceRegister services, ServiceLifetime serviceLifetime, bool replace = false)
            where TService : class
            => services.Register(typeof(TService), typeof(TService), serviceLifetime, replace);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
        /// An instance of <typeparamref name="TImplementation"/> will be created when an instance is needed.
        /// Optionally removes any existing implementation of the same service type.
        /// </summary>
        public static IServiceRegister Register<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime, bool replace = false)
            where TService : class
            where TImplementation : class, TService
            => services.Register(typeof(TService), typeof(TImplementation), serviceLifetime, replace);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
        /// Optionally removes any existing implementation of the same service type.
        /// </summary>
        public static IServiceRegister Register<TService>(this IServiceRegister services, Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
            where TService : class
            => services.Register(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime, replace);

        /// <summary>
        /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider.
        /// Optionally removes any existing implementation of the same service type.
        /// </summary>
        public static IServiceRegister Register<TService>(this IServiceRegister services, TService implementationInstance, bool replace = false)
            where TService : class
            => services.Register(typeof(TService), implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance)), replace);

        /// <inheritdoc cref="TryRegister{TService}(IServiceRegister, Func{IServiceProvider, TService}, ServiceLifetime)"/>
        public static IServiceRegister TryRegister<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceRegister services, ServiceLifetime serviceLifetime)
            where TService : class
            => services.TryRegister(typeof(TService), typeof(TService), serviceLifetime);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency
        /// injection provider if a service of the same type (and of the same implementation type
        /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
        /// has not already been registered. An instance of <typeparamref name="TImplementation"/>
        /// will be created when an instance is needed.
        /// </summary>
        public static IServiceRegister TryRegister<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            where TService : class
            where TImplementation : class, TService
            => services.TryRegister(typeof(TService), typeof(TImplementation), serviceLifetime, mode);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        public static IServiceRegister TryRegister<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            where TService : class
            where TImplementation : class, TService
            => services.TryRegister(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime, mode);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        public static IServiceRegister TryRegister<TService>(this IServiceRegister services, Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class
            => services.TryRegister(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime);

        /// <summary>
        /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider
        /// if a service of the same type has not already been registered.
        /// </summary>
        public static IServiceRegister TryRegister<TService>(this IServiceRegister services, TService implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            where TService : class
            => services.TryRegister(typeof(TService), implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance)), mode);

        /// <inheritdoc cref="IServiceRegister.Configure{TOptions}(Action{TOptions, IServiceProvider})"/>
        public static IServiceRegister Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this IServiceRegister services, Action<TOptions>? action)
            where TOptions : class, new()
            => services.Configure<TOptions>(action == null ? null : (opt, _) => action(opt));
        #endregion

        #region - RegisterAsBoth and TryRegisterAsBoth -
        /// <summary>
        /// Calls Register for both the implementation and service
        /// </summary>
        private static IServiceRegister RegisterAsBoth<TService, TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => services.Register<TImplementation>(serviceLifetime).Register<TService, TImplementation>(serviceLifetime);

        /// <summary>
        /// Calls Register for both the implementation and service
        /// </summary>
        private static IServiceRegister RegisterAsBoth<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => services.Register(implementationFactory, serviceLifetime).Register<TService>(implementationFactory, serviceLifetime);

        /// <summary>
        /// Calls Register for both the implementation and service
        /// </summary>
        private static IServiceRegister RegisterAsBoth<TService, TImplementation>(this IServiceRegister services, TImplementation implementationInstance)
            where TService : class
            where TImplementation : class, TService
            => services.Register(implementationInstance).Register<TService>(implementationInstance);

        /// <summary>
        /// Calls Register for the implementation and TryRegister for the service
        /// </summary>
        private static IServiceRegister TryRegisterAsBoth<TService, TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => services.Register<TImplementation>(serviceLifetime).TryRegister<TService, TImplementation>(serviceLifetime);

        /// <summary>
        /// Calls Register for the implementation and TryRegister for the service
        /// </summary>
        private static IServiceRegister TryRegisterAsBoth<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => services.Register(implementationFactory, serviceLifetime).TryRegister<TService>(implementationFactory, serviceLifetime);

        /// <summary>
        /// Calls Register for the implementation and TryRegister for the service
        /// </summary>
        private static IServiceRegister TryRegisterAsBoth<TService, TImplementation>(this IServiceRegister services, TImplementation implementationInstance)
            where TService : class
            where TImplementation : class, TService
            => services.Register(implementationInstance).TryRegister<TService>(implementationInstance);
        #endregion

        #region - AddSchema -
        /// <summary>
        /// Registers <typeparamref name="TSchema"/> within the dependency injection framework. <see cref="ISchema"/> is also
        /// registered if it is not already registered within the dependency injection framework. Singleton and scoped
        /// lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Schemas that implement <see cref="IDisposable"/> of a transient lifetime are not supported, as this will cause a
        /// memory leak if requested from the root service provider.
        /// </remarks>
        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (serviceLifetime == ServiceLifetime.Transient && typeof(IDisposable).IsAssignableFrom(typeof(TSchema)))
            {
                // This scenario can cause a memory leak if the schema is requested from the root service provider.
                // If it was requested from a scoped provider, then there is no reason to register it as transient.
                // See following link:
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container
                throw new InvalidOperationException("A schema that implements IDisposable should not be registered as a transient service. " +
                    "See https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container");
            }

            // Register the service with the DI provider as TSchema, overwriting any existing registration
            // Also register the service as ISchema if not already registered.
            builder.Services.TryRegisterAsBoth<ISchema, TSchema>(serviceLifetime);

            return builder;
        }

        /// <summary>
        /// Registers <paramref name="schema"/> within the dependency injection framework as <typeparamref name="TSchema"/>. <see cref="ISchema"/> is also
        /// registered if it is not already registered within the dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, TSchema schema)
            where TSchema : class, ISchema
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            // Register the service with the DI provider as TSchema, overwriting any existing registration
            // Also register the service as ISchema if not already registered.
            builder.Services.TryRegisterAsBoth<ISchema, TSchema>(schema);

            return builder;
        }

        /// <inheritdoc cref="AddSchema{TSchema}(IGraphQLBuilder, ServiceLifetime)"/>
        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchema> schemaFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (schemaFactory == null)
                throw new ArgumentNullException(nameof(schemaFactory));

            if (serviceLifetime == ServiceLifetime.Transient && typeof(IDisposable).IsAssignableFrom(typeof(TSchema)))
            {
                // This scenario can cause a memory leak if the schema is requested from the root service provider.
                // If it was requested from a scoped provider, then there is no reason to register it as transient.
                // See following link:
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container
                throw new InvalidOperationException("A schema that implements IDisposable should not be registered as a transient service. " +
                    "See https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container");
            }

            // Register the service with the DI provider as TSchema, overwriting any existing registration
            // Also register the service as ISchema if not already registered.
            builder.Services.TryRegisterAsBoth<ISchema, TSchema>(schemaFactory, serviceLifetime);

            return builder;
        }
        #endregion

        #region - AddGraphTypeMappingProvider -
        /// <summary>
        /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
        /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/>.
        /// <br/><br/>
        /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
        /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
        /// types.
        /// </summary>
        public static IGraphQLBuilder AddGraphTypeMappingProvider<TGraphTypeMappingProvider>(this IGraphQLBuilder builder)
            where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
        {
            builder.Services.Register<IGraphTypeMappingProvider, TGraphTypeMappingProvider>(ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
        /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> using the specified factory delegate.
        /// <br/><br/>
        /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
        /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
        /// types.
        /// </summary>
        public static IGraphQLBuilder AddGraphTypeMappingProvider<TGraphTypeMappingProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TGraphTypeMappingProvider> factory)
            where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
        {
            builder.Services.Register<IGraphTypeMappingProvider>(factory, ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
        /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> using the specified instance.
        /// <br/><br/>
        /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
        /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
        /// types.
        /// </summary>
        public static IGraphQLBuilder AddGraphTypeMappingProvider<TGraphTypeMappingProvider>(this IGraphQLBuilder builder, TGraphTypeMappingProvider instance)
            where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
        {
            builder.Services.Register<IGraphTypeMappingProvider>(instance);
            return builder;
        }
        #endregion

        #region - AddAutoSchema / WithMutation / WithSubscription -
        /// <summary>
        /// Registers an instance of the <see cref="AutoSchema{TQueryClrType}"/> class within the dependency injection framework as a singleton.
        /// <see cref="ISchema"/> is also registered if it is not already registered within the dependency injection framework.
        /// <see cref="Schema.Query"/> is set to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> with
        /// <typeparamref name="TQueryClrType"/> as TSourceType.
        /// <br/><br/>
        /// Additionally, this method calls <see cref="AddAutoClrMappings(IGraphQLBuilder, bool, bool)">AddAutoClrMappings</see>
        /// so that unmapped CLR input or output types are mapped to <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>
        /// and <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> respectively.
        /// <br/><br/>
        /// To register a mutation or subscription CLR type within the schema, use the <paramref name="configure"/> delegate and
        /// call <see cref="WithMutation{TMutationClrType}(IConfigureAutoSchema)">WithMutation</see> or
        /// <see cref="WithSubscription{TSubscriptionClrType}(IConfigureAutoSchema)">WithSubscription</see>, respsectively.
        /// <br/><br/>
        /// This allows for a schema that is entirely configured with CLR types.
        /// </summary>
        [RequiresUnreferencedCode("Please ensure that the CLR types used by your schema are not trimmed by the compiler.")]
        public static IGraphQLBuilder AddAutoSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TQueryClrType>(this IGraphQLBuilder builder, Action<IConfigureAutoSchema>? configure = null)
        {
            builder.AddSchema(provider => new AutoSchema<TQueryClrType>(provider), ServiceLifetime.Singleton);
            builder.Services.TryRegister<IGraphTypeMappingProvider, AutoRegisteringGraphTypeMappingProvider>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
            configure?.Invoke(new ConfigureAutoSchema<TQueryClrType>(builder));
            return builder;
        }

        /// <summary>
        /// Configures <see cref="Schema.Mutation"/> to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
        /// with <typeparamref name="TMutationClrType"/> as TSourceType.
        /// </summary>
        public static IConfigureAutoSchema WithMutation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TMutationClrType>(this IConfigureAutoSchema builder)
        {
            builder.Builder.ConfigureSchema((schema, provider) =>
            {
                if (schema.GetType() == builder.SchemaType)
                    schema.Mutation = provider.GetRequiredService<AutoRegisteringObjectGraphType<TMutationClrType>>();
            });
            return builder;
        }

        /// <summary>
        /// Configures <see cref="Schema.Subscription"/> to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
        /// with <typeparamref name="TSubscriptionClrType"/> as TSourceType.
        /// </summary>
        public static IConfigureAutoSchema WithSubscription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TSubscriptionClrType>(this IConfigureAutoSchema builder)
        {
            builder.Builder.ConfigureSchema((schema, provider) =>
            {
                if (schema.GetType() == builder.SchemaType)
                    schema.Subscription = provider.GetRequiredService<AutoRegisteringObjectGraphType<TSubscriptionClrType>>();
            });
            return builder;
        }
        #endregion

        #region - AddDocumentExecuter -
        /// <summary>
        /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder)
            where TDocumentExecuter : class, IDocumentExecuter
        {
            builder.Services.Register<IDocumentExecuter, TDocumentExecuter>(ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="documentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, TDocumentExecuter documentExecuter)
            where TDocumentExecuter : class, IDocumentExecuter
        {
            builder.Services.Register<IDocumentExecuter>(documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter)));
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework. The supplied factory method is used to create the document executer.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentExecuter> documentExecuterFactory)
            where TDocumentExecuter : class, IDocumentExecuter
        {
            builder.Services.Register<IDocumentExecuter>(documentExecuterFactory ?? throw new ArgumentNullException(nameof(documentExecuterFactory)), ServiceLifetime.Singleton);
            return builder;
        }
        #endregion

        #region - AddComplexityAnalyzer -
        /// <summary>
        /// Enables the default complexity analyzer and configures it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityConfiguration>? action = null)
        {
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer(IGraphQLBuilder, Action{ComplexityConfiguration})"/>
        public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityConfiguration, IServiceProvider?>? action)
        {
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TAnalyzer"/> as a singleton of type <see cref="IComplexityAnalyzer"/> within the
        /// dependency injection framework, then enables and configures it with the specified configuration delegate.
        /// </summary>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Action<ComplexityConfiguration>? action = null)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer, TAnalyzer>(ServiceLifetime.Singleton);
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, Action{ComplexityConfiguration})"/>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Action<ComplexityConfiguration, IServiceProvider?>? action)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer, TAnalyzer>(ServiceLifetime.Singleton);
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="analyzer"/> as a singleton of type <see cref="IComplexityAnalyzer"/> within the
        /// dependency injection framework, then enables and configures it with the specified configuration delegate.
        /// </summary>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, TAnalyzer analyzer, Action<ComplexityConfiguration>? action = null)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer>(analyzer ?? throw new ArgumentNullException(nameof(analyzer)));
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, TAnalyzer, Action{ComplexityConfiguration})"/>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, TAnalyzer analyzer, Action<ComplexityConfiguration, IServiceProvider?>? action)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer>(analyzer ?? throw new ArgumentNullException(nameof(analyzer)));
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <summary>
        /// Registers a singleton of type <see cref="IComplexityAnalyzer"/> within the dependency injection framework
        /// using the specified factory delegate, then enables and configures it with the specified configuration delegate.
        /// </summary>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Func<IServiceProvider, TAnalyzer> analyzerFactory, Action<ComplexityConfiguration>? action = null)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer>(analyzerFactory ?? throw new ArgumentNullException(nameof(analyzerFactory)), ServiceLifetime.Singleton);
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, Func{IServiceProvider, TAnalyzer}, Action{ComplexityConfiguration})"/>
        [Obsolete("Please write a custom complexity analyzer as a validation rule. This method will be removed in v8.")]
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Func<IServiceProvider, TAnalyzer> analyzerFactory, Action<ComplexityConfiguration, IServiceProvider?>? action)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Services.Register<IComplexityAnalyzer>(analyzerFactory ?? throw new ArgumentNullException(nameof(analyzerFactory)), ServiceLifetime.Singleton);
            builder.AddValidationRule<ComplexityValidationRule>();
            builder.Services.Configure(action);
            return builder;
        }
        #endregion

        #region - AddErrorInfoProvider
        /// <summary>
        /// Configures the default error info provider with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions>? action = null)
        {
            builder.Services.Configure(action);
            return builder.AddErrorInfoProvider<ErrorInfoProvider>();
        }

        /// <inheritdoc cref="AddErrorInfoProvider(IGraphQLBuilder, Action{ErrorInfoProviderOptions})"/>
        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions, IServiceProvider>? action)
        {
            builder.Services.Configure(action);
            return builder.AddErrorInfoProvider<ErrorInfoProvider>();
        }

        /// <summary>
        /// Registers <typeparamref name="TProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder)
            where TProvider : class, IErrorInfoProvider
        {
            builder.Services.Register<IErrorInfoProvider, TProvider>(ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="errorInfoProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, TProvider errorInfoProvider)
            where TProvider : class, IErrorInfoProvider
        {
            if (errorInfoProvider == null)
                throw new ArgumentNullException(nameof(errorInfoProvider));

            builder.Services.Register<IErrorInfoProvider>(errorInfoProvider);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="IErrorInfoProvider"/> within the dependency injection framework using the supplied
        /// factory delegate.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TProvider> errorInfoProviderFactory)
            where TProvider : class, IErrorInfoProvider
        {
            if (errorInfoProviderFactory == null)
                throw new ArgumentNullException(nameof(errorInfoProviderFactory));

            builder.Services.Register<IErrorInfoProvider>(errorInfoProviderFactory, ServiceLifetime.Singleton);
            return builder;
        }
        #endregion

        #region - AddGraphTypes -
        /// <summary>
        /// Scans the calling assembly for classes that implement <see cref="IGraphType"/> and registers
        /// them as transients within the dependency injection framework. A transient lifetime ensures
        /// they are only instantiated once each time the schema is built. If the schema is a scoped schema,
        /// the graph types will effectively be scoped graph types. If the schema is a singleton schema,
        /// the graph types will effectively be singleton graph types.
        /// <br/><br/>
        /// Also registers <see cref="EnumerationGraphType{TEnum}"/>, <see cref="ConnectionType{TNodeType}"/>,
        /// <see cref="ConnectionType{TNodeType, TEdgeType}"/>, <see cref="EdgeType{TNodeType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>, and
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> as generic types.
        /// </summary>
        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder)
            => builder.AddGraphTypes(Assembly.GetCallingAssembly());

        /// <summary>
        /// Scans the supplied assembly for classes that implement <see cref="IGraphType"/> and registers
        /// them as transients within the dependency injection framework. A transient lifetime ensures
        /// they are only instantiated once each time the schema is built. If the schema is a scoped schema,
        /// the graph types will effectively be scoped graph types. If the schema is a singleton schema,
        /// the graph types will effectively be singleton graph types.
        /// <br/><br/>
        /// Skips classes where the class is marked with the <see cref="DoNotRegisterAttribute"/>.
        /// <br/><br/>
        /// Also registers <see cref="EnumerationGraphType{TEnum}"/>, <see cref="ConnectionType{TNodeType}"/>,
        /// <see cref="ConnectionType{TNodeType, TEdgeType}"/>, <see cref="EdgeType{TNodeType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>, and
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> as generic types.
        /// </summary>
        [RequiresUnreferencedCode("Types containing in the provided assembly might be removed")]
        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder, Assembly assembly)
        {
            // Graph types are always created with the transient lifetime, since they are only instantiated once
            // each time the schema is built. If the schema is a scoped schema, the graph types will effectively
            // be scoped graph types. If the schema is a singleton schema, the graph types will effectively be
            // singleton graph types. This is REQUIRED behavior and must not be changed.

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && typeof(IGraphType).IsAssignableFrom(x) && !x.IsDefined(typeof(DoNotRegisterAttribute))))
            {
                builder.Services.TryRegister(type, type, ServiceLifetime.Transient);
            }

            builder.Services.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
            builder.Services.TryRegister<PageInfoType>(ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);

            return builder;
        }
        #endregion

        #region - AddClrTypeMappings -
        /// <summary>
        /// Scans the calling assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="SchemaExtensions.RegisterTypeMappings(ISchema)"/>
        /// within the schema constructor.
        /// </remarks>
        [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
        public static IGraphQLBuilder AddClrTypeMappings(this IGraphQLBuilder builder)
            => builder.AddClrTypeMappings(Assembly.GetCallingAssembly());

        /// <summary>
        /// Scans the specified assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="SchemaExtensions.RegisterTypeMappings(ISchema, Assembly)"/>
        /// within the schema constructor.
        /// </remarks>
        [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
        public static IGraphQLBuilder AddClrTypeMappings(this IGraphQLBuilder builder, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            // retreive all of the type mappings ahead-of-time, in case of a scoped or transient schema,
            // as reflection is relatively slow
            var typeMappings = assembly.GetClrTypeMappings();
            foreach (var typeMapping in typeMappings)
            {
                builder.AddGraphTypeMappingProvider(new ManualGraphTypeMappingProvider(typeMapping.ClrType, typeMapping.GraphType));
            }

            return builder;
        }
        #endregion

        #region - AddAutoClrMappings -
        /// <summary>
        /// Registers an instance of <see cref="AutoRegisteringGraphTypeMappingProvider"/> with the dependency injection
        /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> and configures it to map input
        /// and/or output types to <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/> or
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> graph types.
        /// </summary>
        [RequiresUnreferencedCode("Please ensure that the CLR types used by your schema are not trimmed by the compiler.")]
        public static IGraphQLBuilder AddAutoClrMappings(this IGraphQLBuilder builder, bool mapInputTypes = true, bool mapOutputTypes = true)
        {
            builder.AddGraphTypeMappingProvider(new AutoRegisteringGraphTypeMappingProvider(mapInputTypes, mapOutputTypes));
            return builder;
        }
        #endregion

        #region - AddDocumentListener -
        /// <summary>
        /// Registers <typeparamref name="TDocumentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>. Configures document execution to add an instance of <typeparamref name="TDocumentListener"/> to the
        /// list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>. Singleton, scoped and transient lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(serviceLifetime);
            builder.ConfigureExecutionOptions(options => options.Listeners.Add(options.RequestServicesOrThrow().GetRequiredService<TDocumentListener>()));
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="documentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>. Configures document execution to add <paramref name="documentListener"/> to the
        /// list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, TDocumentListener documentListener)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            if (documentListener == null)
                throw new ArgumentNullException(nameof(documentListener));

            builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(documentListener);
            builder.ConfigureExecutionOptions(options => options.Listeners.Add(documentListener));
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TDocumentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>, using the supplied factory delegate. Configures document execution to add an instance of
        /// <typeparamref name="TDocumentListener"/> to the list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>.
        /// Singleton, scoped and transient lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentListener> documentListenerFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(documentListenerFactory ?? throw new ArgumentNullException(nameof(documentListenerFactory)), serviceLifetime);
            builder.ConfigureExecutionOptions(options => options.Listeners.Add(options.RequestServicesOrThrow().GetRequiredService<TDocumentListener>()));
            return builder;
        }
        #endregion

        #region - UseMiddleware -
        /// <inheritdoc cref="UseMiddleware{TMiddleware}(IGraphQLBuilder, bool, ServiceLifetime)"/>
        [Obsolete("Please use UseMiddleware. This method will be removed in v8.")]
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, bool install = true, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
            => UseMiddleware<TMiddleware>(builder, install, serviceLifetime);

        /// <inheritdoc cref="UseMiddleware{TMiddleware}(IGraphQLBuilder, Func{IServiceProvider, ISchema, bool}, ServiceLifetime)"/>
        [Obsolete("Please use UseMiddleware. This method will be removed in v8.")]
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, Func<IServiceProvider, ISchema, bool> installPredicate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
            => UseMiddleware<TMiddleware>(builder, installPredicate, serviceLifetime);

        /// <inheritdoc cref="UseMiddleware{TMiddleware}(IGraphQLBuilder, TMiddleware, bool)"/>
        [Obsolete("Please use UseMiddleware. This method will be removed in v8.")]
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, bool install = true)
            where TMiddleware : class, IFieldMiddleware
            => UseMiddleware(builder, middleware, install);

        /// <inheritdoc cref="UseMiddleware{TMiddleware}(IGraphQLBuilder, TMiddleware, Func{IServiceProvider, ISchema, bool})"/>
        [Obsolete("Please use UseMiddleware. This method will be removed in v8.")]
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, Func<IServiceProvider, ISchema, bool> installPredicate)
            where TMiddleware : class, IFieldMiddleware
            => UseMiddleware(builder, middleware, installPredicate);

        /// <summary>
        /// Registers <typeparamref name="TMiddleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. If <paramref name="install"/> is <see langword="true"/>, installs the middleware by configuring schema
        /// construction to call <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance
        /// of the middleware pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// If <paramref name="install"/> is <see langword="true"/>, do not separately install the middleware within your schema constructor or the
        /// middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, bool install = true, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            if (serviceLifetime == ServiceLifetime.Scoped)
            {
                // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), "Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
            }

            // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(serviceLifetime);
            if (install)
                builder.ConfigureSchema((schema, serviceProvider) => schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>()));
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TMiddleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. Calls the <paramref name="installPredicate"/> delegate during schema construction, and if
        /// <see langword="true"/>, installs the middleware by configuring schema construction to call
        /// <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance of the middleware
        /// pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// Do not separately install the middleware within your schema constructor or the middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, Func<IServiceProvider, ISchema, bool> installPredicate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            if (installPredicate == null)
                throw new ArgumentNullException(nameof(installPredicate));

            if (serviceLifetime == ServiceLifetime.Scoped)
            {
                // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
                throw new ArgumentOutOfRangeException("Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
            }

            // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(serviceLifetime);
            builder.ConfigureSchema((schema, serviceProvider) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>());
            });
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="middleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. If <paramref name="install"/> is <see langword="true"/>, installs the middleware by configuring schema
        /// construction to call <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance
        /// of the middleware pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// If <paramref name="install"/> is <see langword="true"/>, do not separately install the middleware within your schema constructor or the
        /// middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, bool install = true)
            where TMiddleware : class, IFieldMiddleware
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(middleware);
            if (install)
                builder.ConfigureSchema((schema, _) => schema.FieldMiddleware.Use(middleware));
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="middleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. Calls the <paramref name="installPredicate"/> delegate during schema construction, and if
        /// <see langword="true"/>, installs the middleware by configuring schema construction to call
        /// <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance of the middleware
        /// pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// Do not separately install the middleware within your schema constructor or the middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, Func<IServiceProvider, ISchema, bool> installPredicate)
            where TMiddleware : class, IFieldMiddleware
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            if (installPredicate == null)
                throw new ArgumentNullException(nameof(installPredicate));

            builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(middleware);
            builder.ConfigureSchema((schema, serviceProvider) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(middleware);
            });
            return builder;
        }
        #endregion

        #region - AddSerializer -
        /// <summary>
        /// Registers <typeparamref name="TSerializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
        /// dependency injection framework.
        /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
        /// </summary>
        public static IGraphQLBuilder AddSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>(this IGraphQLBuilder builder)
            where TSerializer : class, IGraphQLSerializer
        {
            builder.Services.Register<IGraphQLSerializer, TSerializer>(ServiceLifetime.Singleton, true);
            if (typeof(IGraphQLTextSerializer).IsAssignableFrom(typeof(TSerializer)))
                builder.Services.Register(typeof(IGraphQLTextSerializer), typeof(TSerializer), ServiceLifetime.Singleton, true);
            // builder.Services.Register(services => (IGraphQLTextSerializer)services.GetRequiredService<IGraphQLSerializer>(), ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="serializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
        /// dependency injection framework.
        /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
        /// </summary>
        public static IGraphQLBuilder AddSerializer<TSerializer>(this IGraphQLBuilder builder, TSerializer serializer)
            where TSerializer : class, IGraphQLSerializer
        {
            builder.Services.Register<IGraphQLSerializer>(serializer ?? throw new ArgumentNullException(nameof(serializer)), true);
            if (serializer is IGraphQLTextSerializer textSerializer)
                builder.Services.Register(textSerializer, true);
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TSerializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
        /// dependency injection framework. The supplied factory method is used to create the serializer.
        /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
        /// </summary>
        public static IGraphQLBuilder AddSerializer<TSerializer>(this IGraphQLBuilder builder, Func<IServiceProvider, TSerializer> serializerFactory)
            where TSerializer : class, IGraphQLSerializer
        {
            builder.Services.Register<IGraphQLSerializer>(serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory)), ServiceLifetime.Singleton, true);
            if (typeof(IGraphQLTextSerializer).IsAssignableFrom(typeof(TSerializer)))
                builder.Services.Register(typeof(IGraphQLTextSerializer), serializerFactory, ServiceLifetime.Singleton, true);
            // builder.Services.Register(services => (IGraphQLTextSerializer)services.GetRequiredService<IGraphQLSerializer>(), ServiceLifetime.Singleton);
            return builder;
        }
        #endregion

        #region - ConfigureSchema and ConfigureExecutionOptions and ConfigureExecution -
        /// <summary>
        /// Configures an action to run prior to the code within the schema's constructor.
        /// Assumes that the schema derives from <see cref="Schema"/>.
        /// </summary>
        public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema> action)
            => action == null ? throw new ArgumentNullException(nameof(action)) : builder.ConfigureSchema((schema, _) => action(schema));

        /// <inheritdoc cref="ConfigureSchema(IGraphQLBuilder, Action{ISchema})"/>
        public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema, IServiceProvider> action)
        {
            builder.Services.Register<IConfigureSchema>(new ConfigureSchema(action ?? throw new ArgumentNullException(nameof(action))));
            return builder;
        }

        /// <summary>
        /// Configures an action to configure execution options, which run prior to calls to
        /// <see cref="ConfigureExecution(IGraphQLBuilder, Func{ExecutionOptions, ExecutionDelegate, Task{ExecutionResult}})">ConfigureExecution</see>
        /// and other Use calls such as <see cref="UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>.
        /// <br/><br/>
        /// Assumes that the document executer is <see cref="DocumentExecuter"/>, or that it derives from <see cref="DocumentExecuter"/> and calls
        /// <see cref="DocumentExecuter(IDocumentBuilder, IDocumentValidator, IExecutionStrategySelector, IEnumerable{IConfigureExecution})"/>
        /// within the constructor.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used within the delegate to access the service provider for this execution.
        /// </remarks>
        public static IGraphQLBuilder ConfigureExecutionOptions(this IGraphQLBuilder builder, Action<ExecutionOptions> action)
            => ConfigureExecution(builder, new ConfigureExecutionOptions(action ?? throw new ArgumentNullException(nameof(action))));

        /// <inheritdoc cref="ConfigureExecutionOptions(IGraphQLBuilder, Action{ExecutionOptions})"/>
        public static IGraphQLBuilder ConfigureExecutionOptions(this IGraphQLBuilder builder, Func<ExecutionOptions, Task> action)
            => ConfigureExecution(builder, new ConfigureExecutionOptions(action ?? throw new ArgumentNullException(nameof(action))));

        /// <summary>
        /// Configures an action that can modify or replace document execution behavior, which runs after options configuration
        /// and immediately prior to document execution along with other calls to Use methods such as <see cref="UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used within the delegate to access the service provider for this execution.
        /// </remarks>
        public static IGraphQLBuilder ConfigureExecution(this IGraphQLBuilder builder, Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> action)
            => ConfigureExecution(builder, new ConfigureExecution(action));

        /// <summary>
        /// Configures an action that can modify or replace document execution behavior.
        /// </summary>
        public static IGraphQLBuilder ConfigureExecution<TConfigureExecution>(this IGraphQLBuilder builder)
            where TConfigureExecution : class, IConfigureExecution
        {
            builder.Services.Register<IConfigureExecution, TConfigureExecution>(ServiceLifetime.Singleton);
            return builder;
        }

        /// <inheritdoc cref="ConfigureExecution{TConfigureExecution}(IGraphQLBuilder)"/>
        public static IGraphQLBuilder ConfigureExecution<TConfigureExecution>(this IGraphQLBuilder builder, TConfigureExecution instance)
            where TConfigureExecution : class, IConfigureExecution
        {
            builder.Services.Register<IConfigureExecution>(instance);
            return builder;
        }

        /// <inheritdoc cref="ConfigureExecution{TConfigureExecution}(IGraphQLBuilder)"/>
        public static IGraphQLBuilder ConfigureExecution<TConfigureExecution>(this IGraphQLBuilder builder, Func<IServiceProvider, TConfigureExecution> factory)
            where TConfigureExecution : class, IConfigureExecution
        {
            builder.Services.Register<IConfigureExecution>(factory, ServiceLifetime.Singleton);
            return builder;
        }
        #endregion

        #region - AddValidationRule -
        /// <summary>
        /// Registers <typeparamref name="TValidationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/>.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
        {
            builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(ServiceLifetime.Singleton);
            builder.ConfigureExecutionOptions(options =>
            {
                var rule = options.RequestServicesOrThrow().GetRequiredService<TValidationRule>();
                options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(rule);
                if (useForCachedDocuments)
                {
                    options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(rule);
                }
            });
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="validationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/>.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, TValidationRule validationRule, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
        {
            builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(validationRule ?? throw new ArgumentNullException(nameof(validationRule)));
            builder.ConfigureExecutionOptions(options =>
            {
                options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(validationRule);
                if (useForCachedDocuments)
                {
                    options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(validationRule);
                }
            });
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TValidationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/> using the specified factory delegate.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, Func<IServiceProvider, TValidationRule> validationRuleFactory, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
        {
            builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(validationRuleFactory ?? throw new ArgumentNullException(nameof(validationRuleFactory)), ServiceLifetime.Singleton);
            builder.ConfigureExecutionOptions(options =>
            {
                var rule = options.RequestServicesOrThrow().GetRequiredService<TValidationRule>();
                options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(rule);
                if (useForCachedDocuments)
                {
                    options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(rule);
                }
            });
            return builder;
        }
        #endregion

        #region - UseApolloTracing -
        /// <inheritdoc cref="UseApolloTracing(IGraphQLBuilder, bool)"/>
        [Obsolete("Please use UseApolloTracing. This method will be removed in v8.")]
        public static IGraphQLBuilder AddApolloTracing(this IGraphQLBuilder builder, bool enableMetrics = true)
            => UseApolloTracing(builder, enableMetrics);

        /// <inheritdoc cref="UseApolloTracing(IGraphQLBuilder, Func{ExecutionOptions, bool})"/>
        [Obsolete("Please use UseApolloTracing. This method will be removed in v8.")]
        public static IGraphQLBuilder AddApolloTracing(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enableMetricsPredicate)
            => UseApolloTracing(builder, enableMetricsPredicate);

        /// <summary>
        /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
        /// configures it to be installed within the schema, and configures responses to include Apollo
        /// Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
        /// When <paramref name="enableMetrics"/> is <see langword="true"/>, configures execution to set
        /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
        /// </summary>
        public static IGraphQLBuilder UseApolloTracing(this IGraphQLBuilder builder, bool enableMetrics = true)
            => UseApolloTracing(builder, _ => enableMetrics);

        /// <summary>
        /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
        /// configures it to be installed within the schema, and configures responses to include Apollo
        /// Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
        /// Configures execution to run <paramref name="enableMetricsPredicate"/> and when <see langword="true"/>, sets
        /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
        /// </summary>
        public static IGraphQLBuilder UseApolloTracing(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enableMetricsPredicate)
        {
            if (enableMetricsPredicate == null)
                throw new ArgumentNullException(nameof(enableMetricsPredicate));

            builder.UseMiddleware<InstrumentFieldsMiddleware>();
            builder.ConfigureExecution(async (options, next) =>
            {
                if (enableMetricsPredicate(options))
                    options.EnableMetrics = true;
                var start = DateTime.UtcNow;
                var ret = await next(options).ConfigureAwait(false);
                if (options.EnableMetrics)
                {
                    ret.EnrichWithApolloTracing(start);
                }
                return ret;
            });
            return builder;
        }
        #endregion

        #region - AddExecutionStrategySelector -
        /// <summary>
        /// Registers <typeparamref name="TExecutionStrategySelector"/> with the dependency injection framework as
        /// a singleton of type <see cref="IExecutionStrategySelector"/>.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategySelector<TExecutionStrategySelector>(this IGraphQLBuilder builder)
            where TExecutionStrategySelector : class, IExecutionStrategySelector
        {
            builder.Services.Register<IExecutionStrategySelector, TExecutionStrategySelector>(ServiceLifetime.Singleton);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="executionStrategySelector"/> with the dependency injection framework as
        /// a singleton of type <see cref="IExecutionStrategySelector"/>.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategySelector<TExecutionStrategySelector>(this IGraphQLBuilder builder, TExecutionStrategySelector executionStrategySelector)
            where TExecutionStrategySelector : class, IExecutionStrategySelector
        {
            if (executionStrategySelector == null)
                throw new ArgumentNullException(nameof(executionStrategySelector));

            builder.Services.Register<IExecutionStrategySelector>(executionStrategySelector);
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TExecutionStrategySelector"/> with the dependency injection framework as
        /// a singleton of type <see cref="IExecutionStrategySelector"/>, using the supplied factory delegate.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategySelector<TExecutionStrategySelector>(this IGraphQLBuilder builder, Func<IServiceProvider, TExecutionStrategySelector> executionStrategySelectorFactory)
            where TExecutionStrategySelector : class, IExecutionStrategySelector
        {
            builder.Services.Register<IExecutionStrategySelector>(executionStrategySelectorFactory ?? throw new ArgumentNullException(nameof(executionStrategySelectorFactory)), ServiceLifetime.Singleton);
            return builder;
        }
        #endregion

        #region - AddExecutionStrategy -
        /// <summary>
        /// Registers <typeparamref name="TExecutionStrategy"/> with the dependency injection framework as
        /// a singleton, and registers an <see cref="ExecutionStrategyRegistration"/> for this <typeparamref name="TExecutionStrategy"/>
        /// configured for the selected <paramref name="operationType"/>.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategy<TExecutionStrategy>(this IGraphQLBuilder builder, OperationType operationType)
            where TExecutionStrategy : class, IExecutionStrategy
        {
            builder.Services.Register<TExecutionStrategy>(ServiceLifetime.Singleton);
            builder.Services.Register(
                provider => new ExecutionStrategyRegistration(
                    provider.GetRequiredService<TExecutionStrategy>(),
                    operationType),
                ServiceLifetime.Singleton);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="ExecutionStrategyRegistration"/> with the dependency injection framework
        /// for the specified <paramref name="executionStrategy"/> and <paramref name="operationType"/>.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategy<TExecutionStrategy>(this IGraphQLBuilder builder, TExecutionStrategy executionStrategy, OperationType operationType)
            where TExecutionStrategy : class, IExecutionStrategy
        {
            if (executionStrategy == null)
                throw new ArgumentNullException(nameof(executionStrategy));

            builder.Services.Register(
                new ExecutionStrategyRegistration(
                    executionStrategy,
                    operationType));

            return builder;
        }

        /// <summary>
        /// Registers an <see cref="ExecutionStrategyRegistration"/> with the dependency injection framework
        /// as a singleton for the specified <typeparamref name="TExecutionStrategy"/> and <paramref name="operationType"/>,
        /// using the supplied factory delegate.
        /// </summary>
        public static IGraphQLBuilder AddExecutionStrategy<TExecutionStrategy>(this IGraphQLBuilder builder, Func<IServiceProvider, TExecutionStrategy> executionStrategyFactory, OperationType operationType)
            where TExecutionStrategy : class, IExecutionStrategy
        {
            if (executionStrategyFactory == null)
                throw new ArgumentNullException(nameof(executionStrategyFactory));

            builder.Services.Register(
                provider => new ExecutionStrategyRegistration(
                    executionStrategyFactory(provider),
                    operationType),
                ServiceLifetime.Singleton);

            return builder;
        }
        #endregion

        #region - UseTelemetry -
#if NET5_0_OR_GREATER
        /// <summary>
        /// Configures the GraphQL engine to collect traces via the <see cref="System.Diagnostics.Activity">System.Diagnostics.Activity API</see> and records events that match the
        /// <see href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/instrumentation/graphql.md">OpenTelemetry recommendations</see>.
        /// Trace data contains the GraphQL operation name, the operation type, and the optionally the document.
        /// </summary>
        /// <remarks>
        /// When applicable, place after calls to UseAutomaticPersistedQueries to ensure that the query document is recorded properly.
        /// </remarks>
        public static IGraphQLBuilder UseTelemetry(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions>? configure = null)
            => UseTelemetry(builder, configure != null ? (opts, _) => configure(opts) : null);

        /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
        public static IGraphQLBuilder UseTelemetry(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions, IServiceProvider>? configure)
        {
            builder.Services.Configure(configure);
            builder.ConfigureExecution<GraphQLTelemetryProvider>();
            return builder;
        }
#endif
        #endregion
    }
}
