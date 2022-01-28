using System;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies that a property will be mapped to <see cref="IdGraphType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, Inherited = false)]
    public class IdAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify(TypeInformation typeInformation)
            => typeInformation.GraphType = typeof(IdGraphType);
    }
}
