using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using Xunit.Sdk;

namespace GraphQL.Tests.DI;

/// <summary>
/// Allows test methods from <see cref="QueryTestBase{TSchema, TDocumentBuilder}"/> and descendants
/// work with different DI providers. Mark your test method with [Theory] and [DependencyInjectionData]
/// attributes instead of [Fact] attribute. Also add 'string container' parameter (unused, the name of DI provider)
/// into your test method. By default all methods from <see cref="QueryTestBase{TSchema, TDocumentBuilder}"/>
/// and descendants (not marked with [DependencyInjectionData]) work with the first DI provider from
/// <see cref="DependencyInjectionAdapterData"/>.
/// </summary>
// TODO: remove or rewrite this class; should support theories in conjuction with multiple DI providers
internal sealed class PrepareDependencyInjectionAttribute : BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<MethodInfo> _currentMethod = new();
    private static readonly ConcurrentDictionary<MethodInfo, Stack<IDependencyInjectionAdapter>> _diAdapters = new();

    public override void Before(MethodInfo methodUnderTest)
    {
        Debug.Assert(_currentMethod.Value == null);

        _currentMethod.Value = methodUnderTest;

        _diAdapters.GetOrAdd(methodUnderTest, method =>
        {
            var configureMethod = methodUnderTest.DeclaringType.GetMethod(nameof(QueryTestBase<Schema>.RegisterServices), BindingFlags.Public | BindingFlags.Instance);
            var temp = Activator.CreateInstance(methodUnderTest.DeclaringType);
            Action<IServiceRegister> configure = register => configureMethod?.Invoke(temp, new object[] { register });

            var stack = new Stack<IDependencyInjectionAdapter>();
            foreach (var adapter in DependencyInjectionAdapterData.GetDependencyInjectionAdapters(configure).Reverse())
                stack.Push(adapter);

            if (temp is IDisposable d)
                d.Dispose();

            return stack;
        });
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Debug.Assert(_currentMethod.Value == methodUnderTest);

        _currentMethod.Value = null;

        if (_diAdapters.TryGetValue(methodUnderTest, out var stack))
        {
            _ = stack.Pop();
            if (stack.Count == 0)
                Debug.Assert(_diAdapters.TryRemove(methodUnderTest, out _));
        }
    }

    public static IServiceProvider CurrentServiceProvider
    {
        get
        {
            var method = _currentMethod.Value;
            return method == null || !_diAdapters.TryGetValue(method, out var stack) || stack == null || stack.Count == 0
                ? throw new InvalidOperationError("Attempt to access IServiceProvider out of prepared DependencyInjection context.")
                : stack.Peek().ServiceProvider;
        }
    }
}
