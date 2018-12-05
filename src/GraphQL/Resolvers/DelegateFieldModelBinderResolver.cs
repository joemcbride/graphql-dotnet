using System;
using System.Reflection;
using GraphQL.Execution;
using GraphQL.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver : IFieldResolver
    {
        private readonly Delegate _resolver;
        private readonly ParameterInfo[] _parameters;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            _parameters = _resolver.GetMethodInfo().GetParameters();
        }

        public object Resolve(ResolveFieldContext context)
        {
            var arguments = ReflectionHelper.BuildArguments(_parameters, context);
            return _resolver.DynamicInvoke(arguments);
        }

        public object Resolve(ExecutionContext context, ExecutionNode node)
        {
            var resolveContext = context.CreateResolveFieldContext(node);
            return Resolve(resolveContext);
        }
    }
}
