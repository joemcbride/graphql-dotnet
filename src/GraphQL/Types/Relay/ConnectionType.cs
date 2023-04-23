namespace GraphQL.Types.Relay
{
    /// <summary>
    /// A connection graph type for the specified node graph type. The GraphQL type name
    /// defaults to {NodeType}Connection where {NodeType} is the GraphQL type name of
    /// the node graph type. This graph type assumes that the source (the result of
    /// the parent field's resolver) is <see cref="ConnectionType{TNodeType, TEdgeType}"/>
    /// or <see cref="ConnectionType{TNodeType}"/> or has the same members.
    /// </summary>
    /// <typeparam name="TNodeType">The graph type of the result data set's data type.</typeparam>
    /// <typeparam name="TEdgeType">The edge graph type of node, typically <see cref="EdgeType{TNodeType}"/>.</typeparam>
    public class ConnectionType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEdgeType> : ObjectGraphType<object>
        where TNodeType : IGraphType
        where TEdgeType : EdgeType<TNodeType>
    {
        /// <inheritdoc/>
        public ConnectionType()
        {
            string graphQLTypeName = typeof(TNodeType).GraphQLName();
            Name = $"{graphQLTypeName}Connection";
            Description =
                $"A connection from an object to a list of objects of type `{graphQLTypeName}`.";

            Field<IntGraphType>("totalCount")
                .Description(
                    "A count of the total number of objects in this connection, ignoring pagination. " +
                    "This allows a client to fetch the first five objects by passing \"5\" as the argument " +
                    "to `first`, then fetch the total count so it could display \"5 of 83\", for example. " +
                    "In cases where we employ infinite scrolling or don't have an exact count of entries, " +
                    "this field will return `null`.");

            Field<NonNullGraphType<PageInfoType>>("pageInfo")
                .Description("Information to aid in pagination.");

            Field<ListGraphType<TEdgeType>>("edges")
                .Description("A list of all of the edges returned in the connection.");

            Field<ListGraphType<TNodeType>>("items")
                .Description(
                    "A list of all of the objects returned in the connection. This is a convenience field provided " +
                    "for quickly exploring the API; rather than querying for \"{ edges { node } }\" when no edge data " +
                    "is needed, this field can be used instead. Note that when clients like Relay need to fetch " +
                    "the \"cursor\" field on the edge to enable efficient pagination, this shortcut cannot be used, " +
                    "and the full \"{ edges { node } } \" version should be used instead.");
        }
    }

    /// <summary>
    /// A connection graph type for the specified node type. The GraphQL type name
    /// defaults to {NodeType}Connection where {NodeType} is the GraphQL type name of
    /// the node graph type. The edge graph type used is <see cref="EdgeType{TNodeType}"/>.
    /// </summary>
    /// <typeparam name="TNodeType">The graph type of the result data set's data type.</typeparam>
    public class ConnectionType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TNodeType> : ConnectionType<TNodeType, EdgeType<TNodeType>>
        where TNodeType : IGraphType
    {
    }
}
