using System.ComponentModel;
using GraphQL.Conversion;
using GraphQL.Utilities;

namespace GraphQL;

// decorate any test classes which contain tests that modify these global switches with
//   [Collection("StaticTests")]
// these tests will run sequentially after all other tests are complete
// be sure to restore the global switch to its initial state with a finally block

/// <summary>
/// Global options for configuring GraphQL execution.
/// </summary>
public static class GlobalSwitches
{
    /// <summary>
    /// Enables or disables setting default values for 'defaultValue' from <see cref="DefaultValueAttribute"/>.
    /// <br/>
    /// By default enabled.
    /// </summary>
    public static bool EnableReadDefaultValueFromAttributes { get; set; } = true;

    /// <summary>
    /// Enables or disables setting default values for 'deprecationReason' from <see cref="ObsoleteAttribute"/>.
    /// <br/>
    /// By default enabled.
    /// </summary>
    public static bool EnableReadDeprecationReasonFromAttributes { get; set; } = true;

    /// <summary>
    /// Enables or disables setting default values for 'description' from <see cref="DescriptionAttribute"/>.
    /// <br/>
    /// By default enabled.
    /// </summary>
    public static bool EnableReadDescriptionFromAttributes { get; set; } = true;

    /// <summary>
    /// Enables or disables setting default values for 'description' from XML documentation.
    /// <br/>
    /// By default disabled.
    /// </summary>
    public static bool EnableReadDescriptionFromXmlDocumentation { get; set; } = false;

    /// <summary>
    /// Gets or sets current validation delegate. By default this delegate validates all names according
    /// to the GraphQL <see href="https://spec.graphql.org/October2021/#sec-Names">specification</see>.
    /// <br/><br/>
    /// Setting this delegate allows you to use names not conforming to the specification, for example
    /// 'enum-member'. Only change it when absolutely necessary. This is typically only overridden
    /// when implementing a custom <see cref="INameConverter"/> that fixes names, making them spec-compliant.
    /// <br/><br/>
    /// Keep in mind that regardless of this setting, names are validated upon schema initialization,
    /// after being processed by the <see cref="INameConverter"/>. This is due to the fact that the
    /// parser cannot parse incoming queries with invalid characters in the names, so the resulting
    /// member would become unusable.
    /// </summary>
    public static Action<string, NamedElement> NameValidation = NameValidator.ValidateDefault;

    /// <summary>
    /// Specifies whether to use the names of parent (declaring) types in case of nested graph types
    /// when calculating default graph type name.
    /// <br/>
    /// By default disabled.
    /// </summary>
    public static bool UseDeclaringTypeNames { get; set; } = false;

    /// <summary>
    /// Enable this switch to see additional debugging information in exception messages during schema initialization.
    /// <br/>
    /// By default disabled.
    /// </summary>
    public static bool TrackGraphTypeInitialization { get; set; } = false;

    /// <summary>
    /// A collection of global <see cref="GraphQLAttribute"/> instances which are applied while
    /// <see cref="Types.AutoRegisteringObjectGraphType{TSourceType}">AutoRegisteringObjectGraphType</see>,
    /// <see cref="Types.AutoRegisteringInputObjectGraphType{TSourceType}">AutoRegisteringInputObjectGraphType</see>,
    /// <see cref="Types.EnumerationGraphType{TEnum}">EnumerationGraphType</see>,
    /// <see cref="Builders.FieldBuilder{TSourceType, TReturnType}.ResolveDelegate(Delegate?)">ResolveDelegate</see>
    /// or the schema builder is building graph types, field definitions, arguments, or similar.
    /// <br/><br/>
    /// The collection is not thread-safe; instances should be added prior to schema initialization.
    /// </summary>
    public static ICollection<GraphQLAttribute> GlobalAttributes { get; } = new List<GraphQLAttribute>() { new Internals.InterfaceUsageAttribute() };

    /// <summary>
    /// Enables the schema validation rule requiring schemas to have a Query type defined.
    /// This is required by the GraphQL specification.
    /// See <see href="https://spec.graphql.org/October2021/#sec-Root-Operation-Types">Root Operation Types</see>.
    /// </summary>
    [Obsolete("The query root operation type must be provided and must be an Object type. See https://spec.graphql.org/October2021/#sec-Root-Operation-Types. This property will be removed in v9.")]
    public static bool RequireRootQueryType { get; set; } = true;

    /// <summary>
    /// Use the v7 naming strategy for graph type names.
    /// </summary>
    [Obsolete("This property will be removed in v9.")]
    public static bool UseLegacyTypeNaming { get; set; } = false;

    /// <summary>
    /// Enables caching of reflection metadata and resolvers from <see cref="Types.AutoRegisteringObjectGraphType{TSourceType}">AutoRegisteringObjectGraphType</see>;
    /// useful for scoped schemas.
    /// <br/><br/>
    /// By default disabled. <see cref="GraphQLBuilderExtensions.AddSchema{TSchema}(DI.IGraphQLBuilder, DI.ServiceLifetime)">AddSchema</see> sets
    /// this value to <see langword="true"/> when <see cref="DI.ServiceLifetime.Scoped"/> is specified.
    /// <br/><br/>
    /// Note that with reflection caching enabled, if there are two different classes derived from <see cref="Types.AutoRegisteringObjectGraphType{TSourceType}">AutoRegisteringObjectGraphType</see>
    /// that have the same TSourceType, one instance will incorrectly pull cached information stored by the other instance.
    /// </summary>
    public static bool EnableReflectionCaching { get; set; }

    /// <summary>
    /// Enables or disables the use of dynamic compliation to compile the conversion of dictionaries to objects for
    /// <see cref="GraphQL.Types.InputObjectGraphType{TSourceType}.ParseDictionary(IDictionary{string, object?})">InputObjectGraphType&lt;T&gt;.ParseDictionary</see>
    /// at runtime. Compiled code is cached in memory for the lifetime of the input object graph type.
    /// <br/><br/>
    /// By default enabled for runtimes that support dynamic compilation (i.e. not AOT).
    /// <see cref="GraphQLBuilderExtensions.AddSchema{TSchema}(DI.IGraphQLBuilder, DI.ServiceLifetime)">AddSchema</see> sets
    /// this value to <see langword="false"/> when <see cref="DI.ServiceLifetime.Scoped"/> is specified.
    /// </summary>
    public static bool DynamicallyCompileToObject { get; set; } =
#if NETSTANDARD2_0
        true;
#else
        System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

    /// <summary>
    /// Infer the field's graph type nullability from the Null Reference Type annotations of
    /// the field or property represented by the expression argument. <see langword="true"/> by default.
    /// </summary>
    public static bool InferFieldNullabilityFromNRTAnnotations { get; set; } = true;
}
