using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class MicrosoftDIGraphQLBuilderExtensions
{
    /// <summary>
    /// Configures a GraphQL pipeline using the configuration delegate passed into
    /// <paramref name="configure"/> for the specified service collection and
    /// registers a default set of services required by GraphQL if they have not already been registered.
    /// <br/><br/>
    /// Does not include <see cref="IGraphQLSerializer"/>, and the default <see cref="IDocumentExecuter"/>
    /// implementation does not support subscriptions.
    /// </summary>
    public static IServiceCollection AddGraphQL(this IServiceCollection services, Action<IGraphQLBuilder>? configure)
    {
        _ = new GraphQLBuilder(services, configure);
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TSchema"/> within the dependency injection framework. <see cref="ISchema"/> is also
    /// registered if it is not already registered within the dependency injection framework. Services required by
    /// <typeparamref name="TSchema"/> are instantiated directly if not registered within the dependency injection framework.
    /// This can eliminate the need to register each of the graph types with the dependency injection framework, either
    /// manually or via <see cref="GraphQLBuilderExtensions.AddGraphTypes(IGraphQLBuilder)"/>. Singleton and scoped
    /// lifetimes are supported.
    /// </summary>
    /// <remarks>
    /// Schemas that implement <see cref="IDisposable"/> of a transient lifetime are not supported, as this will cause a
    /// memory leak if requested from the root service provider.
    /// </remarks>
    public static IGraphQLBuilder AddSelfActivatingSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
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
        builder.Services.Register(provider =>
        {
            var selfActivatingServices = new SelfActivatingServiceProvider(provider);
            return ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
        }, serviceLifetime);

        // Now register the service as ISchema if not already registered.
        builder.Services.TryRegister<ISchema>(provider => provider.GetRequiredService<TSchema>(), serviceLifetime);

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="ScopedSubscriptionExecutionStrategy"/> for use with subscription operations,
    /// which will create a dedicated service scope during execution of data events after (but not including)
    /// the initial subscription request execution. Be sure to also execute the initial subscription request via
    /// the <see cref="IDocumentExecuter"/> with a dedicated service scope for proper execution with the common
    /// WebSockets protocols, as multiple subscriptions may execute concurrently over a single HTTP context.
    /// </summary>
    public static IGraphQLBuilder AddScopedSubscriptionExecutionStrategy(this IGraphQLBuilder builder, bool serialExecution = true)
    {
        builder.AddExecutionStrategy(
            provider =>
            {
                var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                // create a new instance of the underlying execution strategy so reusable fields are not shared
                IExecutionStrategy underlyingExecutionStrategy = serialExecution ? new SerialExecutionStrategy() : new ParallelExecutionStrategy();
                return new ScopedSubscriptionExecutionStrategy(serviceScopeFactory, underlyingExecutionStrategy);
            },
            GraphQLParser.AST.OperationType.Subscription);
        return builder;
    }
}
