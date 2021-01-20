using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a complex value within a document that has child fields (an object).
    /// </summary>
    public class ObjectValue : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance that contains the specified field nodes.
        /// </summary>
        public ObjectValue(IEnumerable<ObjectField> fields)
        {
            ObjectFieldsList = (fields ?? throw new ArgumentNullException(nameof(fields))).ToList();
        }

        /// <summary>
        /// Initializes a new instance that contains the specified field nodes.
        /// </summary>
        public ObjectValue(List<ObjectField> fields)
        {
            ObjectFieldsList = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        /// <summary>
        /// Returns a <see cref="Dictionary{TKey, TValue}">Dictionary&lt;string, object&gt;</see>
        /// containing the values of the field nodes that this object value node contains.
        /// </summary>
        public object Value
        {
            get
            {
                var obj = new Dictionary<string, object>();
                FieldNames.Apply(name => obj.Add((string)name, Field(name).Value.Value));
                return obj;
            }
        }

        /// <summary>
        /// Returns the field value nodes that are contained within this object value node.
        /// </summary>
        public IEnumerable<ObjectField> ObjectFields => ObjectFieldsList;

        internal List<ObjectField> ObjectFieldsList { get; private set; }

        /// <summary>
        /// Returns a list of the names of the fields specified for this object value node.
        /// </summary>
        public IEnumerable<ROM> FieldNames
        {
            get
            {
                var list = new List<ROM>(ObjectFieldsList.Count);
                foreach (var item in ObjectFieldsList)
                    list.Add(item.Name);
                return list;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => ObjectFieldsList;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            foreach (var field in ObjectFieldsList)
                action(field, state);
        }

        /// <summary>
        /// Returns the first matching field node contained within this object value node that matches the specified name, or <see langword="null"/> otherwise.
        /// </summary>
        public ObjectField Field(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var field in ObjectFieldsList)
            {
                if (field.Name == name)
                    return field;
            }

            return null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string fields = string.Join(", ", ObjectFields.Select(x => x.ToString()));
            return $"ObjectValue{{objectFields={fields}}}";
        }
    }
}
