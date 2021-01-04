using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a variable definition node within a document.
    /// </summary>
    public class VariableDefinition : AbstractNode
    {
        /// <summary>
        /// Initializes a new variable definition node.
        /// </summary>
        public VariableDefinition()
        {
        }

        /// <summary>
        /// Initializes a new variable definition node with the specified name of the variable.
        /// </summary>
        public VariableDefinition(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the type node representing the graph type of the variable.
        /// </summary>
        public IType Type { get; set; }

        /// <summary>
        /// Returns a value node representing the default value of the variable.
        /// Returns <see langword="null"/> if the variable has no default value. 
        /// </summary>
        public IValue DefaultValue { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (DefaultValue != null)
                {
                    yield return DefaultValue;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"VariableDefinition{{name={Name},type={Type},defaultValue={DefaultValue}}}";
    }
}
