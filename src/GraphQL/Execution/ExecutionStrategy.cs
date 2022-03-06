using System.Buffers;
using System.Collections;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// The base class for the included serial and parallel execution strategies.
    /// </summary>
    public abstract class ExecutionStrategy : IExecutionStrategy
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result. The default implementation builds the root node
        /// and passes execution to <see cref="ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>.
        /// Once complete, the values are collected into an object that is ready to be serialized and returned
        /// within an <see cref="ExecutionResult"/>.
        /// </summary>
        public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context);
            var rootNode = BuildExecutionRootNode(context, rootType);

            await ExecuteNodeTreeAsync(context, rootNode).ConfigureAwait(false);

            // After the entire node tree has been executed, get the values
            object? data = rootNode.PropagateNull() ? null : rootNode;

            return new ExecutionResult
            {
                Executed = true,
                Data = data,
                Query = context.Document.Source,
                Document = context.Document,
                Operation = context.Operation,
                Extensions = context.OutputExtensions
            };
        }

        /// <summary>
        /// Executes an execution node and all of its child nodes. This is typically only executed upon
        /// the root execution node.
        /// </summary>
        protected abstract Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode);

        /// <summary>
        /// Returns the root graph type for the execution -- for a specified schema and operation type.
        /// </summary>
        protected virtual IObjectGraphType GetOperationRootType(ExecutionContext context)
        {
            IObjectGraphType? type;

            switch (context.Operation.Operation)
            {
                case OperationType.Query:
                    type = context.Schema.Query;
                    break;

                case OperationType.Mutation:
                    type = context.Schema.Mutation;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for mutations").AddLocation(context.Operation, context.Document);
                    break;

                case OperationType.Subscription:
                    type = context.Schema.Subscription;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for subscriptions").AddLocation(context.Operation, context.Document);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"{nameof(context)}.{nameof(ExecutionContext.Operation)}", "Can only execute queries, mutations and subscriptions.");
            }

            return type;
        }

        /// <summary>
        /// Builds the root execution node.
        /// </summary>
        protected virtual RootExecutionNode BuildExecutionRootNode(ExecutionContext context, IObjectGraphType rootType)
        {
            var root = new RootExecutionNode(rootType, context.Operation.SelectionSet)
            {
                Result = context.RootValue
            };

            SetSubFieldNodes(context, root);

            return root;
        }

        /// <summary>
        /// Builds an execution node with the specified parameters.
        /// </summary>
        protected virtual ExecutionNode BuildExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode = null)
        {
            if (graphType is NonNullGraphType nonNullFieldType)
                graphType = nonNullFieldType.ResolvedType!;

            return graphType switch
            {
                ListGraphType _ => new ArrayExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                IObjectGraphType _ => new ObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                IAbstractGraphType _ => new ObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                ScalarGraphType scalarGraphType => new ValueExecutionNode(parent, scalarGraphType, field, fieldDefinition, indexInParentNode),
                _ => throw new InvalidOperationException($"Unexpected type: {graphType}")
            };
        }

        /// <summary>
        /// Examines @skip and @include directives for a node and returns a value indicating if the node should be included or not.
        /// <br/><br/>
        /// Note: Neither @skip nor @include has precedence over the other. In the case that both the @skip and @include
        /// directives are provided on the same field or fragment, it must be queried only if the @skip condition
        /// is <see langword="false"/> and the @include condition is <see langword="true"/>. Stated conversely, the field or
        /// fragment must not be queried if either the @skip condition is <see langword="true"/> or the @include condition is <see langword="false"/>.
        /// </summary>
        protected virtual bool ShouldIncludeNode(ExecutionContext context, IHasDirectivesNode node)
        {
            var directives = node.Directives;

            if (directives != null)
            {
                var directive = directives.Find(context.Schema.Directives.Skip.Name);
                if (directive != null)
                {
                    var arg = context.Schema.Directives.Skip.Arguments!.Find("if")!;

#pragma warning disable CS8605 // Unboxing a possibly null value.
                    if ((bool)ExecutionHelper.CoerceValue(arg.ResolvedType!, directive.Arguments?.ValueFor(arg.Name), context.Variables, arg.DefaultValue).Value)
#pragma warning restore CS8605 // Unboxing a possibly null value.
                        return false;
                }

                directive = directives.Find(context.Schema.Directives.Include.Name);
                if (directive != null)
                {
                    var arg = context.Schema.Directives.Include.Arguments!.Find("if")!;

#pragma warning disable CS8605 // Unboxing a possibly null value.
                    return (bool)ExecutionHelper.CoerceValue(arg.ResolvedType!, directive.Arguments?.ValueFor(arg.Name), context.Variables, arg.DefaultValue).Value;
#pragma warning restore CS8605 // Unboxing a possibly null value.
                }
            }

            return true;
        }

        /// <summary>
        /// Creates execution nodes for child fields of an object execution node. Only run if
        /// the object execution node result is not <see langword="null"/>.
        /// </summary>
        protected virtual void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent)
        {
            var parentType = parent.GetObjectGraphType(context.Schema)!;
            var fields = Interlocked.Exchange(ref context.ReusableFields, null);
            fields = CollectFieldsFrom(context, parentType, parent.SelectionSet!, fields);

            var subFields = new ExecutionNode[fields.Count];

            int i = 0;
            foreach (var kvp in fields)
            {
                (var field, var fieldDefinition) = kvp.Value;
                var node = BuildExecutionNode(parent, fieldDefinition.ResolvedType!, field, fieldDefinition);
                subFields[i++] = node;
            }

            parent.SubFields = subFields;

            fields.Clear();
            Interlocked.CompareExchange(ref context.ReusableFields, fields, null);
        }

        /// <summary>
        /// Returns a <see cref="FieldType"/> for the specified AST <see cref="GraphQLField"/> within a specified parent
        /// output graph type within a given schema. For meta-fields, returns the proper meta-field field type.
        /// </summary>
        protected FieldType? GetFieldDefinition(ISchema schema, IComplexGraphType parentType, GraphQLField field)
        {
            if (field.Name == schema.SchemaMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.SchemaMetaFieldType;
            }
            if (field.Name == schema.TypeMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.TypeMetaFieldType;
            }
            if (field.Name == schema.TypeNameMetaFieldType.Name)
            {
                return schema.TypeNameMetaFieldType;
            }

            if (parentType == null)
            {
                throw new ArgumentNullException(nameof(parentType), $"Schema is not configured correctly to fetch field '{field.Name}'. Are you missing a root type?");
            }

            return parentType.GetField(field.Name);
        }

        /// <inheritdoc/>
        public virtual Dictionary<string, (GraphQLField field, FieldType fieldType)>? GetSubFields(ExecutionContext context, ExecutionNode node)
        {
            return node.Field?.SelectionSet?.Selections?.Count > 0
                ? CollectFieldsFrom(context, node.FieldDefinition!.ResolvedType!, node.Field.SelectionSet, null)
                : null;
        }

        /// <summary>
        /// Before execution, the selection set is converted to a grouped field set by calling CollectFields().
        /// Each entry in the grouped field set is a list of fields that share a response key (the alias if defined,
        /// otherwise the field name). This ensures all fields with the same response key included via referenced
        /// fragments are executed at the same time.
        /// <br/><br/>
        /// <see href="http://spec.graphql.org/June2018/#sec-Field-Collection"/> and <see href="http://spec.graphql.org/June2018/#CollectFields()"/>
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="specificType">The graph type to compare the selection set against. May be <see langword="null"/> for root execution node.</param>
        /// <param name="selectionSet">The selection set from the document.</param>
        /// <param name="fields">A dictionary to append the collected list of fields to; if <see langword="null"/>, a new dictionary will be created.</param>
        /// <returns>A list of collected fields</returns>
        protected virtual Dictionary<string, (GraphQLField field, FieldType fieldType)> CollectFieldsFrom(ExecutionContext context, IGraphType specificType, GraphQLSelectionSet selectionSet, Dictionary<string, (GraphQLField field, FieldType fieldType)>? fields)
        {
            fields ??= new();

            // optimization for majority of cases: 0 or 1 fragment spread in selection set, so nothing to track
            int countOfSpreads = GetFragmentSpreads(selectionSet);
            ROM[]? visitedFragmentNames = null;
            if (countOfSpreads > 1)
                visitedFragmentNames = ArrayPool<ROM>.Shared.Rent(countOfSpreads);

            CollectFields(context, specificType.GetNamedType(), selectionSet, fields, visitedFragmentNames, 0);

            if (visitedFragmentNames != null)
                ArrayPool<ROM>.Shared.Return(visitedFragmentNames, true);

            return fields;

            void CollectFields(ExecutionContext context, IGraphType specificType, GraphQLSelectionSet selectionSet, Dictionary<string, (GraphQLField field, FieldType fieldType)> fields, ROM[]? visitedFragmentNames, int foundFragments)
            {
                static bool Contains(ROM[] array, ROM item, int count)
                {
                    for (int i = 0; i < count; ++i)
                    {
                        if (array[i] == item)
                            return true;
                    }
                    return false;
                }

                if (selectionSet != null)
                {
                    foreach (var selection in selectionSet.Selections)
                    {
                        if (selection is GraphQLField field)
                        {
                            if (ShouldIncludeNode(context, field))
                            {
                                var fieldType = GetFieldDefinition(context.Schema, (IComplexGraphType)specificType, field);
                                if (fieldType == null)
                                    throw new InvalidOperationException($"Schema is not configured correctly to fetch field '{field.Name}' from type '{specificType.Name}'.");

                                Add(fields, field, fieldType);
                            }
                        }
                        else if (selection is GraphQLFragmentSpread spread)
                        {
                            if ((visitedFragmentNames == null || !Contains(visitedFragmentNames, spread.FragmentName.Name, foundFragments)) && ShouldIncludeNode(context, spread))
                            {
                                if (visitedFragmentNames != null)
                                    visitedFragmentNames[foundFragments++] = spread.FragmentName.Name;

                                var fragment = context.Document.FindFragmentDefinition(spread.FragmentName.Name);
                                if (fragment != null && ShouldIncludeNode(context, fragment) && DoesFragmentConditionMatch(context, fragment.TypeCondition.Type.Name, specificType))
                                    CollectFields(context, specificType, fragment.SelectionSet, fields, visitedFragmentNames, foundFragments);
                            }
                        }
                        else if (selection is GraphQLInlineFragment inline)
                        {
                            // inline.Type may be null
                            // See [2.8.2] Inline Fragments: If the TypeCondition is omitted, an inline fragment is considered to be of the same type as the enclosing context.
                            if (ShouldIncludeNode(context, inline) && DoesFragmentConditionMatch(context, inline.TypeCondition != null ? inline.TypeCondition.Type.Name : specificType.Name, specificType))
                                CollectFields(context, specificType, inline.SelectionSet, fields, visitedFragmentNames, foundFragments);
                        }
                    }
                }
            }

            static void Add(Dictionary<string, (GraphQLField Field, FieldType FieldType)> fields, GraphQLField field, FieldType fieldType)
            {
                string name = field.Alias != null ? field.Alias.Name.StringValue : fieldType.Name; //ISSUE:allocation in case of alias

                fields[name] = fields.TryGetValue(name, out var original)
                    ? (new GraphQLField // Sets a new field selection node with the child field selection nodes merged with another field's child field selection nodes.
                    {
                        Alias = original.Field.Alias,
                        Name = original.Field.Name,
                        Arguments = original.Field.Arguments,
                        SelectionSet = Merge(original.Field.SelectionSet, field.SelectionSet),
                        Directives = original.Field.Directives,
                        Location = original.Field.Location,
                    }, original.FieldType)
                    : (field, fieldType);
            }

            // Returns a new selection set node with the contents merged with another selection set node's contents.
            static GraphQLSelectionSet? Merge(GraphQLSelectionSet? selection, GraphQLSelectionSet? otherSelection)
            {
                if (selection == null)
                    return otherSelection;

                if (otherSelection == null)
                    return selection;

                return new GraphQLSelectionSet
                {
                    Selections = selection.Selections.Union(otherSelection.Selections).ToList()
                };
            }

            static int GetFragmentSpreads(GraphQLSelectionSet selectionSet)
            {
                int count = 0;

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection is GraphQLFragmentSpread)
                        ++count;
                }

                return count;
            }
        }

        /// <summary>
        /// This method calculates the criterion for matching fragment definition (spread or inline) to a given graph type.
        /// This criterion determines the need to fill the resulting selection set with fields from such a fragment.
        /// <br/><br/>
        /// <see href="http://spec.graphql.org/June2018/#DoesFragmentTypeApply()"/>
        /// </summary>
        protected bool DoesFragmentConditionMatch(ExecutionContext context, ROM fragmentName, IGraphType type /* should be named type*/)
        {
            if (fragmentName.IsEmpty)
                throw new ArgumentException("Fragment name can not be empty", nameof(fragmentName));

            var conditionalType = context.Schema.AllTypes[fragmentName];

            if (conditionalType == null)
            {
                return false;
            }

            if (conditionalType.Equals(type))
            {
                return true;
            }

            if (conditionalType is IAbstractGraphType abstractType)
            {
                return abstractType.IsPossibleType(type);
            }

            return false;
        }

        /// <summary>
        /// Creates execution nodes for array elements of an array execution node. Only run if
        /// the array execution node result is not <see langword="null"/>.
        /// </summary>
        protected virtual async Task SetArrayItemNodesAsync(ExecutionContext context, ArrayExecutionNode parent)
        {
            var listType = (ListGraphType)parent.GraphType!;
            var itemType = listType.ResolvedType!;

            if (itemType is NonNullGraphType nonNullGraphType)
                itemType = nonNullGraphType.ResolvedType!;

            if (parent.Result is not IEnumerable data)
            {
                throw new InvalidOperationException($"Expected an IEnumerable list though did not find one. Found: {parent.Result?.GetType().Name}");
            }

            int index = 0;
            var arrayItems = (data is ICollection collection)
                ? new List<ExecutionNode>(collection.Count)
                : new List<ExecutionNode>();

            if (data is IList list)
            {
                for (int i = 0; i < list.Count; ++i)
                    await SetArrayItemNode(list[i]).ConfigureAwait(false);
            }
            else
            {
                foreach (object? d in data)
                    await SetArrayItemNode(d).ConfigureAwait(false);
            }

            parent.Items = arrayItems;

            // local function uses 'struct closure' without heap allocation
            async Task SetArrayItemNode(object? d)
            {
                var node = BuildExecutionNode(parent, itemType, parent.Field!, parent.FieldDefinition!, index++);
                node.Result = d;

                if (d is not IDataLoaderResult)
                {
                    await CompleteNodeAsync(context, node).ConfigureAwait(false);
                }

                arrayItems.Add(node);
            }
        }

        /// <summary>
        /// Executes a single node. If the node does not return an <see cref="IDataLoaderResult"/>,
        /// it will pass execution to <see cref="CompleteNodeAsync(ExecutionContext, ExecutionNode)"/>.
        /// </summary>
        protected virtual async Task ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // these are the only conditions upon which a node has already been executed when this method is called
            if (node is RootExecutionNode || node.Parent is ArrayExecutionNode)
                return;

            try
            {
                ReadonlyResolveFieldContext? resolveContext = Interlocked.Exchange(ref context.ReusableReadonlyResolveFieldContext, null);
                resolveContext = resolveContext != null ? resolveContext.Reset(node, context) : new ReadonlyResolveFieldContext(node, context);

                var resolver = node.FieldDefinition!.Resolver ?? NameFieldResolver.Instance;
                var result = await resolver.ResolveAsync(resolveContext).ConfigureAwait(false);

                node.Result = result;

                if (result is not IDataLoaderResult)
                {
                    await CompleteNodeAsync(context, node).ConfigureAwait(false);
                    // for non-dataloader nodes that completed without throwing an error, we can re-use the context,
                    // except for enumerable lists - in case the user returned a value with LINQ that is based on the context source
                    if (result is not IEnumerable || result is string)
                    {
                        // also see FuncFieldResolver.GetResolverFor as it relates to context re-use
                        resolveContext.Reset(null, null);
                        Interlocked.CompareExchange(ref context.ReusableReadonlyResolveFieldContext, resolveContext, null);
                    }
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (await ProcessNodeUnhandledExceptionAsync(context, node, ex).ConfigureAwait(false))
                    throw;
            }
        }

        /// <summary>
        /// Completes a pending data loader node. If the node does not return an <see cref="IDataLoaderResult"/>,
        /// it will pass execution to <see cref="CompleteNodeAsync(ExecutionContext, ExecutionNode)"/>.
        /// </summary>
        protected virtual async Task CompleteDataLoaderNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            if (node.Result is not IDataLoaderResult dataLoaderResult)
                throw new InvalidOperationException("This execution node is not pending completion");

            try
            {
                node.Result = await dataLoaderResult.GetResultAsync(context.CancellationToken).ConfigureAwait(false);

                if (node.Result is not IDataLoaderResult)
                {
                    await CompleteNodeAsync(context, node).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (await ProcessNodeUnhandledExceptionAsync(context, node, ex).ConfigureAwait(false))
                    throw;
            }
        }

        /// <summary>
        /// Validates a node result. Builds child nodes via <see cref="SetSubFieldNodes(ExecutionContext, ObjectExecutionNode)">SetSubFieldNodes</see>
        /// and <see cref="SetArrayItemNodesAsync(ExecutionContext, ArrayExecutionNode)">SetArrayItemNodes</see>, but does not execute them. For value
        /// execution nodes, it will run <see cref="ScalarGraphType.Serialize(object)"/> to serialize the result.
        /// </summary>
        protected virtual async Task CompleteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            try
            {
                if (node is ValueExecutionNode valueNode)
                {
                    node.Result = valueNode.GraphType.Serialize(node.Result);
                }

                ValidateNodeResult(context, node);

                // Build child nodes
                if (node.Result != null)
                {
                    if (node is ObjectExecutionNode objectNode)
                    {
                        SetSubFieldNodes(context, objectNode);
                    }
                    else if (node is ArrayExecutionNode arrayNode)
                    {
                        await SetArrayItemNodesAsync(context, arrayNode).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (await ProcessNodeUnhandledExceptionAsync(context, node, ex).ConfigureAwait(false))
                    throw;
            }
        }

        /// <summary>
        /// Sets the location and path information to the error and adds it to the document. Sets the node result to <see langword="null"/>.
        /// </summary>
        protected virtual void SetNodeError(ExecutionContext context, ExecutionNode node, ExecutionError error)
        {
            error.AddLocation(node.Field, context.Document);
            error.Path = node.ResponsePath;
            context.Errors.Add(error);

            node.Result = null;
        }

        /// <summary>
        /// Processes unhandled field resolver exceptions.
        /// </summary>
        /// <returns>A value that indicates when the exception should be rethrown.</returns>
        protected virtual async Task<bool> ProcessNodeUnhandledExceptionAsync(ExecutionContext context, ExecutionNode node, Exception ex)
        {
            if (context.ThrowOnUnhandledException)
                return true;

            UnhandledExceptionContext? exceptionContext = null;
            if (context.UnhandledExceptionDelegate != null)
            {
                // be sure not to re-use this instance of `IResolveFieldContext`
                var resolveContext = new ReadonlyResolveFieldContext(node, context);
                exceptionContext = new UnhandledExceptionContext(context, resolveContext, ex);
                await context.UnhandledExceptionDelegate(exceptionContext).ConfigureAwait(false);
                ex = exceptionContext.Exception;
            }

            var error = ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? $"Error trying to resolve field '{node.Name}'.", ex);

            SetNodeError(context, node, error);

            return false;
        }

        /// <summary>
        /// Validates the <see cref="ExecutionNode.Result"/> to ensure that it is valid for the node.
        /// Errors typically occur when a null value is returned for a non-null graph type. Also validates the
        /// object type when <see cref="IObjectGraphType.IsTypeOf"/> is assigned, or when the graph type
        /// is an <see cref="IAbstractGraphType"/>.
        /// </summary>
        protected virtual void ValidateNodeResult(ExecutionContext context, ExecutionNode node)
        {
            var result = node.Result;

            IGraphType? fieldType = node.ResolvedType;
            var objectType = fieldType as IObjectGraphType;

            if (fieldType is NonNullGraphType nonNullType)
            {
                if (result == null)
                {
                    throw new InvalidOperationException("Cannot return null for a non-null type."
                        + $" Field: {node.Name}, Type: {nonNullType}.");
                }

                objectType = nonNullType.ResolvedType as IObjectGraphType;
            }

            if (result == null)
            {
                return;
            }

            if (fieldType is IAbstractGraphType abstractType)
            {
                objectType = abstractType.GetObjectType(result, context.Schema);

                if (objectType == null)
                {
                    throw new InvalidOperationException(
                        $"Abstract type {abstractType.Name} must resolve to an Object type at " +
                        $"runtime for field {node.Parent?.GraphType?.Name}.{node.Name} " +
                        $"with value '{result}', received 'null'.");
                }

                if (!abstractType.IsPossibleType(objectType))
                {
                    throw new InvalidOperationException($"Runtime Object type '{objectType}' is not a possible type for '{abstractType}'.");
                }
            }

            if (objectType?.IsTypeOf != null && !objectType.IsTypeOf(result))
            {
                throw new InvalidOperationException($"'{result}' value of type '{result.GetType()}' is not allowed for '{objectType.Name}'. Either change IsTypeOf method of '{objectType.Name}' to accept this value or return another value from your resolver.");
            }
        }
    }
}
