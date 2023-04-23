using System.ComponentModel;
using System.Reflection;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// Also called Enums, enumeration types are a special kind of scalar that is restricted to a
/// particular set of allowed values. This allows you to:
/// 1. Validate that any arguments of this type are one of the allowed values.
/// 2. Communicate through the type system that a field will always be one of a finite set of values.
/// </summary>
public class EnumerationGraphType : ScalarGraphType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationGraphType"/> class.
    /// </summary>
    public EnumerationGraphType()
    {
        Values = CreateValues();
    }

    /// <summary>
    /// Adds a value to the allowed set of enumeration values.
    /// </summary>
    /// <param name="name">The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").</param>
    /// <param name="value">The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).</param>
    /// <param name="description">A description of the enumeration member.</param>
    /// <param name="deprecationReason">The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.</param>
    public void Add(string name, object? value, string? description = null, string? deprecationReason = null)
    {
        Add(new EnumValueDefinition(name, value)
        {
            Description = description,
            DeprecationReason = deprecationReason
        });
    }

    /// <summary>
    /// Adds a value to the allowed set of enumeration values.
    /// </summary>
    public void Add(EnumValueDefinition value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        NameValidator.ValidateName(value.Name, NamedElement.EnumValue);
        Values.Add(value);
    }

    /// <summary>
    /// Returns the allowed set of enumeration values.
    /// </summary>
    public EnumValuesBase Values { get; }

    /// <summary>
    /// Returns a new instance of <see cref="EnumValues"/>.
    /// </summary>
    protected virtual EnumValuesBase CreateValues() => new EnumValues();

    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLEnumValue enumValue => Values.FindByName(enumValue.Name)?.Value ?? ThrowLiteralConversionError(value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLEnumValue enumValue => Values.FindByName(enumValue.Name) != null,
        GraphQLNullValue _ => true,
        _ => false
    };

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        string s => Values.FindByName(s)?.Value ?? ThrowValueConversionError(value),
        null => null,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value switch
    {
        string s => Values.FindByName(s) != null,
        null => true,
        _ => false
    };

    /// <inheritdoc/>
    public override object? Serialize(object? value)
    {
        var foundByValue = Values.FindByValue(value);

        return foundByValue == null
            ? value == null ? null : ThrowSerializationError(value)
            : foundByValue.Name;
    }

    /// <inheritdoc/>
    public override GraphQLValue ToAST(object? value)
    {
        var foundByValue = Values.FindByValue(value);

        return foundByValue == null
            ? value == null ? GraphQLValuesCache.Null : ThrowASTConversionError(value)
            : new GraphQLEnumValue(new GraphQLName(foundByValue.Name));
    }
}

/// <summary>
/// Allows you to automatically register the necessary enumeration members for the specified enum.
/// Supports <see cref="DescriptionAttribute"/> and <see cref="ObsoleteAttribute"/>.
/// Also it can get descriptions for enum fields from the XML comments.
/// </summary>
/// <typeparam name="TEnum"> The enum to take values from. </typeparam>
public class EnumerationGraphType<[DynamicallyAccessedMembers(GET_ALL_MEMBERS)] TEnum> : EnumerationGraphType
    where TEnum : Enum
{
    private static readonly EnumCaseAttribute? _caseAttr = typeof(TEnum).GetCustomAttribute<EnumCaseAttribute>();

    private const DynamicallyAccessedMemberTypes GET_ALL_MEMBERS = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
           DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
           DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
           DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
           DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
           DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationGraphType"/> class.
    /// </summary>
    public EnumerationGraphType()
    {
        var type = typeof(TEnum);
        var names = Enum.GetNames(type);
        var enumMembers = names.Select(n => (name: n, member: type
                .GetMember(n, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .First()));
        var enumGraphData = enumMembers.Select(e => (
            name: ChangeEnumCase(e.name),
            value: Enum.Parse(type, e.name),
            description: e.member.Description(),
            deprecation: e.member.ObsoleteMessage(),
            member: e.member
        ));

        Name = type.Name.ToPascalCase();
        Description ??= typeof(TEnum).Description();
        DeprecationReason ??= typeof(TEnum).ObsoleteMessage();

        foreach (var (name, value, description, deprecation, member) in enumGraphData)
        {
            var enumValue = new EnumValueDefinition(name, value)
            {
                Description = description,
                DeprecationReason = deprecation,
            };
            bool ignore = false;
            foreach (var attr in member.GetGraphQLAttributes())
            {
                if (!attr.ShouldInclude(member, null))
                {
                    ignore = true;
                    break;
                }
                attr.Modify(enumValue);
            }
            if (ignore)
                continue;
            Add(enumValue);
        }

        foreach (var attr in type.GetGraphQLAttributes())
        {
            attr.Modify(this);
        }
    }

    /// <summary>
    /// Returns a new instance of <see cref="EnumValues{TEnum}"/>.
    /// </summary>
    protected override EnumValuesBase CreateValues() => new EnumValues<TEnum>();

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        TEnum _ => Values.FindByValue(value)?.Value ?? ThrowValueConversionError(value), // no boxing
        _ => base.ParseValue(value)
    };

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value switch
    {
        TEnum _ => Enum.IsDefined(typeof(TEnum), value), // no boxing
        _ => base.CanParseValue(value)
    };

    /// <summary>
    /// Changes the case of the specified enum name.
    /// By default changes it to constant case (uppercase, using underscores to separate words).
    /// </summary>
    protected virtual string ChangeEnumCase(string val)
        => _caseAttr == null ? val.ToConstantCase() : _caseAttr.ChangeEnumCase(val);
}
