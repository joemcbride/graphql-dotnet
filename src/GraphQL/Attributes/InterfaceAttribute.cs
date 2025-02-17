using System.Reflection;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Marks an interface as a GraphQL interface.
    /// When used on an interface implemented by a CLR type that is automatically registered with the schema
    /// via <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>, the GraphQL type will implement
    /// the GraphQL interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class InterfaceAttribute : Attribute
    {
    }
}

namespace GraphQL.Internals
{
    /// <summary>
    /// Scans the object model for referenced interfaces marked with <see cref="InterfaceAttribute"/> and registers them.
    /// </summary>
    public class InterfaceUsageAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify(IGraphType graphType, Type sourceType)
        {
            if (graphType is IImplementInterfaces hasInterfacesType)
            {
                foreach (var interfaceType in sourceType.GetInterfaces())
                {
                    if (interfaceType.GetCustomAttribute<InterfaceAttribute>() != null)
                    {
                        hasInterfacesType.Interfaces.Add(interfaceType);
                    }
                }
            }
        }
    }
}
