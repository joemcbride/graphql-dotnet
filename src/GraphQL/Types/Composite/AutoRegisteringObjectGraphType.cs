using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// </summary>
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            _excludedProperties = excludedProperties;
            Name = typeof(TSourceType).GraphQLName();
            ConfigureGraph();
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
        }

        /// <summary>
        /// Applies default configuration settings to this graph type along with any <see cref="GraphQLAttribute"/> attributes marked on <typeparamref name="TSourceType"/>.
        /// Allows the ability to override the default naming convention used by this class without affecting attributes applied directly to <typeparamref name="TSourceType"/>.
        /// </summary>
        protected virtual void ConfigureGraph()
        {
            AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
        }

        /// <summary>
        /// Returns a list of <see cref="FieldType"/> instances representing the fields ready to be
        /// added to the graph type.
        /// </summary>
        protected virtual IEnumerable<FieldType> ProvideFields()
        {
            foreach (var memberInfo in GetRegisteredMembers())
            {
                bool include = true;
                foreach (var attr in memberInfo.GetCustomAttributes<GraphQLAttribute>())
                {
                    include = attr.ShouldInclude(memberInfo, false);
                    if (!include)
                        break;
                }
                if (!include)
                    continue;
                var fieldType = CreateField(memberInfo);
                if (fieldType != null)
                    yield return fieldType;
            }
        }

        /// <summary>
        /// Processes the specified member and returns a <see cref="FieldType"/>.
        /// May return <see langword="null"/> to skip a member.
        /// </summary>
        protected virtual FieldType? CreateField(MemberInfo memberInfo)
        {
            var typeInformation = GetTypeInformation(memberInfo);
            var graphType = typeInformation.ConstructGraphType();
            var fieldType = AutoRegisteringHelper.CreateField(memberInfo, graphType, false);
            var (queryArguments, resolver) = BuildFieldResolver(memberInfo, fieldType);
            fieldType.Arguments = queryArguments;
            fieldType.Resolver = resolver;
            // apply field attributes after resolver has been set
            AutoRegisteringHelper.ApplyFieldAttributes(memberInfo, fieldType, false);
            return fieldType;
        }

        /// <summary>
        /// Retuns a list of query arguments within a <see cref="QueryArguments"/> instance, and a
        /// <see cref="IFieldResolver"/> instance, for the specified field.
        /// </summary>
        protected (QueryArguments? QueryArguments, IFieldResolver Resolver) BuildFieldResolver(MemberInfo memberInfo, FieldType fieldType)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                var getMethod = propertyInfo.GetMethod ?? throw new InvalidOperationException("No 'get' method for the supplied property.");
                var resolver = new MethodResolver(getMethod, BuildMemberInstanceExpression(memberInfo), Array.Empty<LambdaExpression>());
                return (null, resolver);
            }
            else if (memberInfo is MethodInfo methodInfo)
            {
                List<LambdaExpression> expressions = new();
                QueryArguments queryArguments = new();
                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    var argumentInfo = (ArgumentInformation?)_getArgumentInformationInternalMethodInfo
                        .MakeGenericMethod(parameterInfo.ParameterType)
                        .Invoke(this, new object[] { fieldType, parameterInfo })!;
                    var (queryArgument, expression) = argumentInfo.ConstructQueryArgument();
                    if (queryArgument != null)
                    {
                        ApplyArgumentAttributes(parameterInfo, queryArgument);
                        queryArguments.Add(queryArgument);
                    }
                    expression ??= AutoRegisteringHelper.GetParameterExpression(
                        parameterInfo.ParameterType,
                        queryArgument ?? throw new InvalidOperationException("Invalid response from ConstructQueryArgument: queryArgument and expression cannot both be null"));
                    expressions.Add(expression);
                }
                var resolver = new MethodResolver(methodInfo, BuildMemberInstanceExpression(memberInfo), expressions);
                return (queryArguments, resolver);
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                var sourceExpression = BuildMemberInstanceExpression(memberInfo);
                var param = sourceExpression.Parameters[0];
                var body = Expression.Convert(
                    Expression.MakeMemberAccess(sourceExpression.Body, fieldInfo),
                    typeof(object));
                var lambda = Expression.Lambda<Func<IResolveFieldContext, object?>>(body, param);
                var func = lambda.Compile();
                return (null, new FuncFieldResolver<object>(func));
            }
            else if (memberInfo == null)
            {
                throw new ArgumentNullException(nameof(memberInfo));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(memberInfo), "Member must be a field, property or method.");
            }
        }

        /// <summary>
        /// Returns a lambda expression that will be used by the field resolver to access the member.
        /// <br/><br/>
        /// Typically this is a lambda expression of type <see cref="Func{T, TResult}">Func</see>&lt;<see cref="IResolveFieldContext"/>, <typeparamref name="TSourceType"/>&gt;.
        /// <br/><br/>
        /// By default this returns the <see cref="IResolveFieldContext.Source"/> property, or a new instance if the source is <see langword="null"/>.
        /// </summary>
        /// <param name="memberInfo">The member being called or accessed.</param>
        protected virtual LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
            => _sourceExpression;

        private static readonly Expression<Func<IResolveFieldContext, TSourceType>> _sourceExpression = BuildSourceExpression();
        private static Expression<Func<IResolveFieldContext, TSourceType>> BuildSourceExpression()
        {
            // determine if there is an empty constructor for TSourceType
            var defaultConstructor = typeof(TSourceType).GetConstructor(Array.Empty<Type>());
            // if not, just cast context.Source
            if (defaultConstructor == null)
            {
                return context => (TSourceType)context.Source!;
            }
            // if so, return context => (TSourceType)context.Source ?? new TSourceType();
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var sourceVariable = Expression.Variable(typeof(TSourceType), "source");
            var resolveFieldContextSourceParameter = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;
            var sourceExpression = Expression.MakeMemberAccess(param, resolveFieldContextSourceParameter);
            var setVariable = Expression.Assign(
                sourceVariable,
                Expression.Convert(
                    sourceExpression,
                    typeof(TSourceType)));
            var body = Expression.Condition(
                Expression.Equal(sourceVariable, Expression.Constant(null, typeof(object))),
                Expression.New(defaultConstructor),
                sourceVariable);
            var block = Expression.Block(
                typeof(TSourceType),
                new ParameterExpression[] { sourceVariable },
                setVariable,
                body);
            var lambda = Expression.Lambda(block, param);
            return (Expression<Func<IResolveFieldContext, TSourceType>>)lambda;
        }

        private static readonly MethodInfo _getArgumentInformationInternalMethodInfo = typeof(AutoRegisteringObjectGraphType<TSourceType>).GetMethod(nameof(GetArgumentInformationInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private ArgumentInformation GetArgumentInformationInternal<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
            => GetArgumentInformation<TParameterType>(fieldType, parameterInfo);

        /// <summary>
        /// Applies <see cref="GraphQLAttribute"/> attributes defined on the supplied <see cref="ParameterInfo"/>
        /// to the specified <see cref="QueryArgument"/>.
        /// </summary>
        protected virtual void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
        {
            // Apply derivatives of GraphQLAttribute
            var attributes = parameterInfo.GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(queryArgument);
            }
        }

        /// <summary>
        /// Analyzes a method parameter and returns an instance of <see cref="ArgumentInformation"/>
        /// containing information necessary to build a <see cref="QueryArgument"/> and <see cref="IFieldResolver"/>.
        /// Also applies any <see cref="GraphQLAttribute"/> attributes defined on the <see cref="ParameterInfo"/>
        /// to the returned <see cref="ArgumentInformation"/> instance.
        /// </summary>
        protected virtual ArgumentInformation GetArgumentInformation<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        {
            var typeInformation = GetTypeInformation(parameterInfo);
            var argumentInfo = new ArgumentInformation(parameterInfo, typeof(TSourceType), fieldType, typeInformation);
            argumentInfo.ApplyAttributes();
            return argumentInfo;
        }

        /// <summary>
        /// Returns a list of properties, methods or fields that should have fields created for them.
        /// <br/><br/>
        /// Unless overridden, returns a list of public instance readable properties (including properties declared on
        /// inherited classes) and public instance methods (excluding methods declared on inherited classes) that
        /// do not return <see langword="void"/> or <see cref="Task"/>.
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
        {
            var props = AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead),
                _excludedProperties);
            var methods = typeof(TSourceType).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x =>
                    !x.ContainsGenericParameters && // exclude methods with open generics
                    !x.IsSpecialName &&             // exclude methods generated for properties
                    x.ReturnType != typeof(void) && // exclude methods which do not return a value
                    x.ReturnType != typeof(Task));  // exclude methods which do not return a value
            return props.Concat<MemberInfo>(methods);
        }

        /// <summary>
        /// Analyzes a property, method or field and returns an instance of <see cref="TypeInformation"/>
        /// containing information necessary to select a graph type. Nullable reference annotations
        /// are read, if they exist, as well as the <see cref="RequiredAttribute"/> attribute.
        /// Then any <see cref="GraphQLAttribute"/> attributes marked on the property are applied.
        /// <br/><br/>
        /// Override this method to enforce specific graph types for specific CLR types, or to implement custom
        /// attributes to change graph type selection behavior.
        /// </summary>
        protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        {
            var typeInformation = memberInfo switch
            {
                PropertyInfo propertyInfo => new TypeInformation(propertyInfo, false),
                MethodInfo methodInfo => new TypeInformation(methodInfo),
                FieldInfo fieldInfo => new TypeInformation(fieldInfo, false),
                _ => throw new ArgumentOutOfRangeException(nameof(memberInfo), "Only properties, methods and fields are supported."),
            };
            typeInformation.ApplyAttributes();
            return typeInformation;
        }

        /// <summary>
        /// Analyzes a method argument and returns an instance of <see cref="TypeInformation"/>
        /// containing information necessary to select a graph type. Nullable reference annotations
        /// are read, if they exist, as well as the <see cref="RequiredAttribute"/> attribute.
        /// Then any <see cref="GraphQLAttribute"/> attributes marked on the property are applied.
        /// <br/><br/>
        /// Override this method to enforce specific graph types for specific CLR types, or to implement custom
        /// attributes to change graph type selection behavior.
        /// </summary>
        protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
        {
            var typeInformation = new TypeInformation(parameterInfo);
            typeInformation.ApplyAttributes();
            return typeInformation;
        }
    }
}
