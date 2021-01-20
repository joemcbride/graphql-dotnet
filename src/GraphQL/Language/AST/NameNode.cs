using System;
using System.Collections.Generic;
using GraphQLParser;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a name within a document. This could be the name of a field, type, argument, directive, alias, etc.
    /// </summary>
    public readonly struct NameNode : INode
    {
        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(ROM name, SourceLocation location)
        {
            Name = name;
            SourceLocation = location;
        }

        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(ROM name)
        {
            Name = name;
            SourceLocation = default;
        }

        /// <summary>
        /// Returns the contained name.
        /// </summary>
        public ROM Name { get; }

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; }

        IEnumerable<INode> INode.Children => null;

        /// <inheritdoc/>
        public void Visit<TState>(Action<INode, TState> action, TState state) { }
    }
}
