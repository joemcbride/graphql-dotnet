# Schema Types

## Scalars

A GraphQL object type has a name and fields, but at some point those fields have to resolve
to some concrete data. That's where the scalar types come in: they represent the leaves of the query.

These are the scalars provided by the [GraphQL Specification](https://spec.graphql.org/October2021/#sec-Scalars).

| GraphQL | GraphQL.NET        | .NET                    |
|----------|---------------------|-------------------------|
| `String`   | `StringGraphType`   | `string`                |
| `Int`       | `IntGraphType`      | `int`                   |
| `Float`     | `FloatGraphType`    | `double`                |
| `Boolean`   | `BooleanGraphType`  | `bool`                  |
| `ID`        | `IdGraphType`       | `int`, `long`, `string` |

> Note that you can use a `Guid` with `ID`.  It will just be serialized to a `string` and
> should be sent to your GraphQL Server as a `string`.

These are additional scalars provided by this project.

| GraphQL          | GraphQL.NET                    | .NET               | Format | Remarks |
|------------------|---------------------------------|--------------------|-------|---------|
| `BigInt` | `BigIntGraphType` | `BigInteger` | number |
| `Byte` | `ByteGraphType` | `byte` | number |
| `Date`           | `DateGraphType`                 | `DateTime`         | ISO-8601: yyyy-MM-dd |
| `DateOnly` | `DateOnlyGraphType` | `DateOnly`                           | ISO-8601: yyyy-MM-dd | .NET6 and higher |
| `DateTime`       | `DateTimeGraphType`             | `DateTime`         | ISO-8601, assume UTC |
| `DateTimeOffset` | `DateTimeOffsetGraphType`       | `DateTimeOffset`   | ISO-8601 |
| `Decimal` | `DecimalGraphType` | `decimal` | number |
| `Guid` | `GuidGraphType` | `Guid` | string |
| `Half` | `HalfGraphType` | `Half` | number | .NET5 and higher |
| `Long` | `LongGraphType` | `long` | number |
| `Milliseconds`   | `TimeSpanMillisecondsGraphType` | `TimeSpan`         | number |
| `SByte` | `SByteGraphType` | `sbyte` | number |
| `Seconds`        | `TimeSpanSecondsGraphType`      | `TimeSpan`         | number |
| `Short` | `ShortGraphType` | `short` | number |
| `TimeOnly` | `TimeOnlyGraphType` | `TimeOnly` | ISO-8601: HH:mm:ss.FFFFFFF | .NET6 and higher |
| `UInt` | `UIntGraphType` | `uint` | number |
| `ULong` | `ULongGraphType` | `ulong` | number |
| `Uri` | `UriGraphType` | `Uri` | RFC 2396/2732/3986/3987 |
| `UShort` | `UShortGraphType` | `ushort` | number |
| `Version` | `VersionGraphType` | `Version` | major.minor.build.revision |

Lists of data are also supported with any Scalar or Object types.

| GraphQL    | GraphQL.NET                        | .NET           |
| -----------|-------------------------------------|----------------|
| `[String]` | `ListGraphType<StringGraphType>`    | `List<string>` |
| `[Boolean]` | `ListGraphType<BooleanGraphType>`    | `List<bool>` |

## Objects

Objects are composed of scalar types and other objects.

**GraphQL**

```graphql
type Droid {
  name: String
  appearsIn: [Episode]
}
```

**GraphQL.NET**

```csharp
public class DroidType : ObjectGraphType<Droid>
{
    public DroidType()
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";
        Field(d => d.Name, nullable: true).Description("The name of the droid.");
        Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
    }
}
```

**.NET**

```csharp
public class Droid
{
  public string Name { get; set; }
  public List<Episode> AppearsIn { get; set; }
}
```

## Enumerations

Enumerations, or enums, define a finite set of discrete values. Like scalars, they represent a leaf in the query.

**GraphQL**

This enum defines the first three Star Wars films using GraphQL schema language:

```graphql
enum Episode {
  NEWHOPE
  EMPIRE
  JEDI
}
```

**.NET**

Consider the equivalent `enum` in .NET:

```csharp
public enum Episodes
{
    NEWHOPE = 4,
    EMPIRE = 5,
    JEDI = 6
}
```

Compare the two implementations. GraphQL does not specify backing values for members of its enums.
The name of each member _is_ the value.

**GraphQL.NET**

GraphQL.NET provides two methods of defining GraphQL enums.

1. You can use `EnumerationGraphType<TEnum>` to automatically generate values by providing a .NET `enum` for `TEnum`.

- The `Name` will default to the .NET type name, which you can override in the constructor.
- The `Description` will default to any `System.ComponentModel.DescriptionAttribute` applied to the enum type.
- The `DeprecationReason` will default to any `System.ObsoleteAttribute` applied to the enum type.
- Apply a `DescriptionAttribute` to an enum member to set the GraphQL `Description`.
- Apply an `ObsoleteAttribute` to an enum member to set the GraphQL `DeprecationReason`.

By default, the name of each enum member will be converted to CONSTANT_CASE. If you want to change
this behavior, you can make it in two ways.

a. Inherit from `EnumerationGraphType<TEnum>` and override `ChangeEnumCase` method.

```csharp
public class CamelCaseEnumerationGraphType<T> : EnumerationGraphType<T> where T : Enum
{
    protected override string ChangeEnumCase(string val) => val.ToCamelCase();
}
```

and then inheriting this class instead of `EnumerationGraphType`

```csharp
public class MediaTypeEnum : CamelCaseEnumerationGraphType<MediaTypeViewModel>
{
}
```
 
b. Mark your .NET enum with one of the `EnumCaseAttribute` descendants (`PascalCase`,  `CamelCase`, `ConstantCase`, `AsIsCase` or your own).

```csharp
[CamelCase]
public enum CamelCaseEnum
{
    FirstValue,
    SecondValue
}
```

```csharp
[Description("The Star Wars movies.")]
[Obsolete("Optional. Sets the GraphQL DeprecationReason for the whole enum.")]
public enum Episodes
{
    [Description("Episode 1: The Phantom Menace")]
    [Obsolete("Optional. Sets the GraphQL DeprecationReason for this member.")]
    PHANTOMMENACE = 1,

    [Description("Episode 4: A New Hope")]
    NEWHOPE  = 4,

    [Description("Episode 5: The Empire Strikes Back")]
    EMPIRE  = 5,

    [Description("Episode 6: Return of the Jedi")]
    JEDI  = 6
}

public class EpisodeEnum : EnumerationGraphType<Episodes>
{
}
```

When defining a field via an expression syntax as in the following example, GraphQL.NET
will automatically map enumeration types to `EnumerationGraphType<TEnum>`, unless otherwise
mapped via `Schema.RegisterTypeMapping`:

```csharp
Field(x => x.MyEnum);
```

2. You can also manually create the `EnumerationGraphType`. Advantages of this method:

- The GraphQL enum need not map to a specific .NET `enum`. You could, for instance, build the enum from one of the alternate methods of defining discrete sets of values in .NET, such as classes of constants or static properties.
- You can manually add descriptions and deprecation reasons. This may be useful if you do not control the source code for the enum.
- Backing enum values may be of any type, primitive or not.

```csharp
public class EpisodeEnum : EnumerationGraphType
{
    public EpisodeEnum()
    {
        Name = "Episode";
        Description = "One of the films in the Star Wars Trilogy.";
        Add("NEWHOPE", 4, "Released in 1977.");
        Add("EMPIRE", 5, "Released in 1980.");
        Add("JEDI", 6, "Released in 1983.");
    }
}
```

Note that although GraphQL has no use for backing values for enum members, GraphQL.NET uses
them anyway. This allows for a more natural mapping to .NET `enum`s or other collections of
constants, and avoids coupling your business logic to GraphQL semantics. The backing values
are strictly for use on the back end - the client will never see them.

**Resolving Enumerations**

Fields typed as enumerations are resolved by returning the backing value of
one of the enum members. Lists of enumerations are resolved by returning collections of enum
members. In the below examples, notice the identical implementations of the `appearsIn` field
for both human graph types. In both implementations, the client receives the GraphQL enum member
names in response to queries on the `appearsIn` field.

If the field resolves a value which cannot be mapped to one of the enum's legal values,
GraphQL.NET will trigger a [Processing Error](errors#ProcessingErrors).

```csharp
public class HumanString
{
    //i.e. "NEWHOPE", "EMPIRE", "JEDI"
    public string[] AppearsIn { get; set; }
}

public class HumanStringType: ObjectGraphType<HumanString>
{
    public HumanStringType()
    {
        Name = "HumanString";
        Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
    }
}

public class HumanInt
{
    //i.e. 4, 5, 6
    public int[] AppearsIn { get; set; }
}

public class HumanIntType: ObjectGraphType<HumanInt>
{
    public HumanIntType()
    {
        Name = "HumanInt";
        Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
    }
}
```

**Enumeration Arguments**

Enumerations can be used as arguments in GraphQL queries. Consider a query which gets the humans appearing in a specific film:

```graphql
query HumansAppearingIn($episode: Episode!){
    humans(appearsIn: $episode){
        id
        name
        appearsIn
    }
}

# example query variables:
# {
#   "episode":"NEWHOPE"
# }
```

When GraphQL.NET receives an enum member name as a query argument, the queried field's
`ResolveFieldContext` stores the backing value associated with the enum member name
in its arguments list. The GraphQL.NET query type which handles the example query may
be implemented as:

```csharp
    public class StarWarsQuery : ObjectGraphType<object>
    {
        public StarWarsQuery()
        {
            Name = "Query";

            Field<ListGraphType<HumanType>>("humans")
                .Argument<NonNullGraphType<EpisodeEnum>>("appearsIn", "An episode the human appears in.")
                .Resolve(context => 
                {
                    // episode = 4
                    var episode = context.GetArgument<int>("appearsIn");

                    // Alternatively, get the argument as an enum. episodeFromEnum = Episodes.NEWHOPE
                    var episodeFromEnum = context.GetArgument<Episodes>("appearsIn");

                    // full implementation would access data store to get humans by episode.
                    return default(Human);
                }
            );
        }
    }
```

**Schema-First Enumeration Types**

If you have defined your schema with the schema-first syntax, the backing value of each of the enumeration
values will default to a string matching the name of the enumeration value. If you wish to use a C#
enumeration type instead, configure the type as demonstrated in one of the following examples:

```csharp
var schema = Schema.For(definitions, c =>
{
    // example 1: define the "Animal" schema enumeration type to use the C# type Animal
    c.Types.Include<Animal>();

    // example 2: define the "AnimalType" schema enumeration type to use the C# type Animal
    c.Types.Include<Animal>("AnimalType");

    // example 3: define the "Animal" schema enumeration type to use the C# type Animal
    c.Types.For("Animal").Type = typeof(Animal);
});
```

## Type Mapping

When specifying a field using the shortcut syntax `Field(x => x.Parent)`, which does not specify
a specific graph type, GraphQL.NET will first look at the data model to see if it has an `[InputType]`
or `[OutputType]` attribute specified on it indicating the graph type to use for the data model. For
instance, you can specify the graph type for a `Widget` class in the following manner:

```csharp
[InputType(typeof(WidgetInputGraphType))]
[OutputType(typeof(WidgetGraphType))]
public class Widget
{
    ...
}
```

If no attribute is specified on the type, it will search a list of CLR mappings to graph type classes.
All of the intrinsic and supplemental scalar graph types included with GraphQL.NET will be searched,
and lists are handled automatically as well.

You can also specify additional mappings during the schema initialization, which will be searched
when the schema is initialized. These mappings can be for input objects, output objects, or scalars.
A single CLR type can be mapped separately for both input and output objects.

You can override default mappings of built-in scalars by registering your own mapping.
To add a mapping, call the `RegisterTypeMapping` method on the `Schema`. Below is a sample of how
to add mappings:

```csharp
public class MySchema
{
    public void MySchema()
    {
        ...

        // For output graphs, map the 'User' data model class to the output object graph type 'UserGraphType'
        this.RegisterTypeMapping<User, UserGraphType>();

        // For input graphs, map the 'User' data model class to the input object graph type 'UserInputGraphType'
        this.RegisterTypeMapping<User, UserInputGraphType>();

        // For input or output graphs, map the 'Vector' class/struct to the scalar graph type 'VectorGraphType'
        this.RegisterTypeMapping<Vector, VectorGraphType>();

        // Override Guid default mapping to use the custom scalar graph type 'MyGuidGraphType'
        this.RegisterTypeMapping<Guid, MyGuidGraphType>()
    }
}
```

There is no limitation on the CLR type of registered mappings -- for instance, scalar graph types
can map to .NET objects or value types such as structs. However, mapping a list type such as `byte[]`
is not supported, as the GraphQL.NET infrastructure will change this into a list graph type
automatically and only search the registered mappings for a registration for `byte`.

In order to implement these type mappings, GraphQL.NET will build the field or argument using a
pseudo-type of either `GraphQLClrOutputTypeReference<T>` or `GraphQLClrInputTypeReference<T>`.
These are resolved automatically during schema initialization. If you are writing your own field
builders, you may use these pseudo-graphtype classes as placeholders for .NET-type-mapped fields
or arguments.

## Type references

If you are writing your own dynamic schema-builder or field-builder code, you may have a need to
have a placeholder graph type that is resolved during schema initialization. There are three type
reference types available for this purpose:

- `GraphQLTypeReference` can be used as a placeholder for another named graph type within the schema.
- `GraphQLClrOutputTypeReference<T>` can be used as a placeholder for a CLR-mapped output graph type.
- `GraphQLClrInputTypeReference<T>` can be used as a placeholder for a CLR-mapped input graph type.

These type references will be resolved during schema initialization. Please refer to the source
code for implementation and usage details.
