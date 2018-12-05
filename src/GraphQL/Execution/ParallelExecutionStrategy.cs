using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            var pendingNodes = new List<ExecutionNode>
            {
                rootNode
            };

            while (pendingNodes.Count > 0)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var currentTasks = pendingNodes
                    .Select(p => ExecuteNodeAsync(context, p))
                    .ToArray();

                pendingNodes.Clear();

                await OnBeforeExecutionStepAwaitedAsync(context)
                    .ConfigureAwait(false);

                // Await tasks for this execution step
                var completedNodes = await Task.WhenAll(currentTasks)
                    .ConfigureAwait(false);

                // Add child nodes to pending nodes to execute the next level in parallel
                var childNodes = completedNodes
                    .OfType<IParentExecutionNode>()
                    .SelectMany(x => x.GetChildNodes());

                pendingNodes.AddRange(childNodes);
            }
        }
    }
}
