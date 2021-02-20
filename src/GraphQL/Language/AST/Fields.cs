using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of field nodes within a document.
    /// </summary>
    public class Fields : Dictionary<string, Field>
    {
        private sealed class DefaultExecutionStrategy : IExecutionStrategy
        {
            public static readonly DefaultExecutionStrategy Instance = new DefaultExecutionStrategy();

            public Task<ExecutionResult> ExecuteAsync(ExecutionContext context) => throw new NotImplementedException();

            public bool ShouldIncludeNode(ExecutionContext context, IHaveDirectives directives) => ExecutionHelper.ShouldIncludeNode(context, directives.Directives);
        }

        /// <summary>
        /// Before execution, the selection set is converted to a grouped field set by calling CollectFields().
        /// Each entry in the grouped field set is a list of fields that share a response key (the alias if defined,
        /// otherwise the field name). This ensures all fields with the same response key included via referenced
        /// fragments are executed at the same time.
        /// <br/><br/>
        /// See http://spec.graphql.org/June2018/#sec-Field-Collection and http://spec.graphql.org/June2018/#CollectFields()
        /// </summary>
        public Fields CollectFrom(ExecutionContext context, IGraphType specificType, SelectionSet selectionSet)
        {
            List<string> visitedFragmentNames = null;
            CollectFields(context, specificType, selectionSet, context.ExecutionStrategy ?? DefaultExecutionStrategy.Instance, ref visitedFragmentNames);
            return this;
        }

        private void CollectFields(ExecutionContext context, IGraphType specificType, SelectionSet selectionSet, IExecutionStrategy strategy, ref List<string> visitedFragmentNames) //TODO: can be completely eliminated? see Fields.Add
        {
            if (selectionSet != null)
            {
                foreach (var selection in selectionSet.SelectionsList)
                {
                    if (selection is Field field)
                    {
                        if (!strategy.ShouldIncludeNode(context, field))
                        {
                            continue;
                        }

                        Add(field);
                    }
                    else if (selection is FragmentSpread spread)
                    {
                        if ((visitedFragmentNames != null && visitedFragmentNames.Contains(spread.Name))
                            || !strategy.ShouldIncludeNode(context, spread))
                        {
                            continue;
                        }

                        (visitedFragmentNames ??= new List<string>()).Add(spread.Name);

                        var fragment = context.Fragments.FindDefinition(spread.Name);
                        if (fragment == null
                            || !strategy.ShouldIncludeNode(context, fragment)
                            || !ExecutionHelper.DoesFragmentConditionMatch(context, fragment.Type.Name, specificType))
                        {
                            continue;
                        }

                        CollectFields(context, specificType, fragment.SelectionSet, strategy, ref visitedFragmentNames);
                    }
                    else if (selection is InlineFragment inline)
                    {
                        var name = inline.Type != null ? inline.Type.Name : specificType.Name;

                        if (!strategy.ShouldIncludeNode(context, inline)
                          || !ExecutionHelper.DoesFragmentConditionMatch(context, name, specificType))
                        {
                            continue;
                        }

                        CollectFields(context, specificType, inline.SelectionSet, strategy, ref visitedFragmentNames);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a field node to the list.
        /// </summary>
        public void Add(Field field)
        {
            string name = field.Alias ?? field.Name;

            if (TryGetValue(name, out Field original))
            {
                // Sets a new field selection node with the child field selection nodes merged with another field's child field selection nodes.
                this[name] = new Field(original.AliasNode, original.NameNode)
                {
                    Arguments = original.Arguments,
                    SelectionSet = original.SelectionSet.Merge(field.SelectionSet),
                    Directives = original.Directives,
                    SourceLocation = original.SourceLocation,
                };
            }
            else
            {
                this[name] = field;
            }
        }
    }
}

