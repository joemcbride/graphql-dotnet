using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Indicates that the method is a GraphQL Federation resolver.
/// The method should return an instance of the resolved entity, or a data-loader or asynchronous variation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public partial class FederationResolverAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="FederationResolverAttribute"/>
    public FederationResolverAttribute()
    {
    }

    /// <inheritdoc/>
    public override bool ShouldInclude(MemberInfo memberInfo, bool? isInputType)
        => !(isInputType ?? false);

    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        // ensure that the method returns the entity type
        if (typeInformation.Type != typeInformation.MemberInfo.DeclaringType)
        {
            var (clrType, memberDescription) = GetMemberInfo();
            throw new InvalidOperationException($"The return type of the {memberDescription} must be {clrType} or an asynchronous variation.");
        }
        // ensure that the method does not return a list type
        if (typeInformation.IsList)
        {
            var (_, memberDescription) = GetMemberInfo();
            throw new InvalidOperationException($"The return type of the {memberDescription} must not be a list type.");
        }

        // helper method for exception messages
        (string ClrType, string MemberDescription) GetMemberInfo()
        {
            var clrType = typeInformation.MemberInfo.DeclaringType!.GetFriendlyName();
            var memberDescription = $"{clrType}.{typeInformation.MemberInfo.Name} method"; // only methods allowed
            return (clrType, memberDescription);
        }
    }

    /// <inheritdoc/>
    public override void Modify(IGraphType graphType, MemberInfo memberInfo, FieldType fieldType, bool isInputType, ref bool ignore)
    {
        if (!isInputType)
        {
            fieldType.IsPrivate = true;
            var isStatic =
                (memberInfo is PropertyInfo pi && (pi.GetMethod?.IsStatic ?? false)) ||
                (memberInfo is MethodInfo mi && mi.IsStatic) ||
                (memberInfo is FieldInfo fi && fi.IsStatic);

            // generate a federation resolver for this graph type
            graphType.Metadata[FederationHelper.RESOLVER_METADATA] = isStatic
                // for static members, generate an IResolveFieldContext where the arguments are the various
                //   properties provided from Apollo Router, and a null source
                ? new FederationStaticResolver(fieldType)
                // for instance members, generate an IResolveFieldContext where the source is coerced from
                //   the properties provided from Apollo Router, and null arguments
                : new FederationNonStaticResolver(fieldType, memberInfo.DeclaringType!);
        }
    }
}
