using GraphQL.DataLoader;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>
    public class SerialExecutionStrategy : ExecutionStrategy
    {
        // frequently reused objects
        private Stack<ExecutionNode>? _reusableNodes;
        private Queue<ExecutionNode>? _reusableDataLoaderNodes;
        private Stack<ExecutionNode>? _reusableAddlNodes;

        /// <summary>
        /// Gets a static instance of <see cref="SerialExecutionStrategy"/> strategy.
        /// </summary>
        public static SerialExecutionStrategy Instance { get; } = new SerialExecutionStrategy();

        /// <summary>
        /// Executes document nodes serially. Nodes that return a <see cref="IDataLoaderResult"/> will
        /// execute once all other pending nodes have been completed.
        /// </summary>
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            // Use a stack to track all nodes in the tree that need to be executed
            var nodes = Interlocked.Exchange(ref _reusableNodes, null) ?? new Stack<ExecutionNode>();
            nodes.Push(rootNode);
            var dataLoaderNodes = Interlocked.Exchange(ref _reusableDataLoaderNodes, null) ?? new Queue<ExecutionNode>();
            var addlNodes = Interlocked.Exchange(ref _reusableAddlNodes, null) ?? new Stack<ExecutionNode>();

            try
            {
                // Process each node on the stack one by one
                while (nodes.Count > 0 || dataLoaderNodes.Count > 0)
                {
                    while (nodes.Count > 0)
                    {
                        var node = nodes.Pop();
                        await ExecuteNodeAsync(context, node).ConfigureAwait(false);

                        // Push any child nodes on top of the stack
                        if (node.Result is IDataLoaderResult)
                        {
                            dataLoaderNodes.Enqueue(node);
                        }
                        else if (node is IParentExecutionNode parentNode)
                        {
                            // Add in reverse order so fields are executed in the correct order
                            parentNode.ApplyToChildren((node, state) => state.Push(node), nodes, reverse: true);
                        }
                    }

                    while (dataLoaderNodes.Count > 0)
                    {
                        var node = dataLoaderNodes.Dequeue();
                        await CompleteDataLoaderNodeAsync(context, node).ConfigureAwait(false);

                        // Push any child nodes on top of the stack
                        if (node.Result is IDataLoaderResult)
                        {
                            dataLoaderNodes.Enqueue(node);
                        }
                        else if (node is IParentExecutionNode parentNode)
                        {
                            // Do not reverse the order of the nodes here
                            parentNode.ApplyToChildren((node, state) => state.Push(node), addlNodes, reverse: false);
                        }
                    }

                    // Reverse order of queued nodes from data loader nodes so they are executed in the correct order
                    while (addlNodes.Count > 0)
                    {
                        nodes.Push(addlNodes.Pop());
                    }
                }
            }
            finally
            {
                nodes.Clear();
                dataLoaderNodes.Clear();
                addlNodes.Clear();

                Interlocked.CompareExchange(ref _reusableNodes, nodes, null);
                Interlocked.CompareExchange(ref _reusableDataLoaderNodes, dataLoaderNodes, null);
                Interlocked.CompareExchange(ref _reusableAddlNodes, addlNodes, null);
            }
        }
    }
}
