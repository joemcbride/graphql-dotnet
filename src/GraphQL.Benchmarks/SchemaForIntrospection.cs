using System.Numerics;
using GraphQL.Types;

namespace GraphQL.Benchmarks;

public class SchemaForIntrospection : Schema
{
    public SchemaForIntrospection()
    {
        Query = new MyQuery();
    }
}

public class MyQuery : ObjectGraphType
{
    public MyQuery()
    {
        Field<StringGraphType>("get")
            .Arguments(
            new QueryArgument<BigIntGraphType>
            {
                Name = "arg01",
                DefaultValue = 10
            },
            new QueryArgument<BooleanGraphType>
            {
                Name = "arg02",
                DefaultValue = true
            },
            new QueryArgument<ByteGraphType>
            {
                Name = "arg03",
                DefaultValue = 20
            },
            new QueryArgument<DateGraphType>
            {
                Name = "arg04",
                DefaultValue = new DateTime(2017, 10, 10)
            },
            new QueryArgument<DateTimeGraphType>
            {
                Name = "arg05",
                DefaultValue = new DateTime(2018, 10, 10)
            },
            new QueryArgument<DateTimeOffsetGraphType>
            {
                Name = "arg06",
                DefaultValue = new DateTime(2019, 10, 10)
            },
            new QueryArgument<DecimalGraphType>
            {
                Name = "arg07",
                DefaultValue = 10m
            },
            new QueryArgument<FloatGraphType>
            {
                Name = "arg08",
                DefaultValue = 0.2f
            },
            new QueryArgument<GuidGraphType>
            {
                Name = "arg09",
                DefaultValue = Guid.Parse("BB82E563-A64F-4978-8F0A-7D52B174A963")
            },
            new QueryArgument<IdGraphType>
            {
                Name = "arg10",
                DefaultValue = "xdf178"
            },
            new QueryArgument<IntGraphType>
            {
                Name = "arg11",
                DefaultValue = 42
            },
            new QueryArgument<LongGraphType>
            {
                Name = "arg12",
                DefaultValue = 424242
            },
            new QueryArgument<SByteGraphType>
            {
                Name = "arg13",
                DefaultValue = -81
            },
            new QueryArgument<ShortGraphType>
            {
                Name = "arg14",
                DefaultValue = 82
            },
            new QueryArgument<StringGraphType>
            {
                Name = "arg15",
                DefaultValue = "world"
            },
            new QueryArgument<TimeSpanMillisecondsGraphType>
            {
                Name = "arg16",
                DefaultValue = TimeSpan.FromMilliseconds(1700)
            },
            new QueryArgument<TimeSpanSecondsGraphType>
            {
                Name = "arg17",
                DefaultValue = TimeSpan.FromMilliseconds(3400)
            },
            new QueryArgument<UIntGraphType>
            {
                Name = "arg18",
                DefaultValue = 650_000_000
            },
            new QueryArgument<ULongGraphType>
            {
                Name = "arg19",
                DefaultValue = 410_000_000_000
            },
            new QueryArgument<UriGraphType>
            {
                Name = "arg20",
                DefaultValue = new Uri("http://www.microsoft.com")
            },
            new QueryArgument<UShortGraphType>
            {
                Name = "arg21",
                DefaultValue = 17000
            }
#if NET6_0_OR_GREATER
            , new QueryArgument<DateOnlyGraphType>
            {
                Name = "arg22",
                DefaultValue = new DateOnly(2018, 10, 10)
            }
            , new QueryArgument<TimeOnlyGraphType>
            {
                Name = "arg23",
                DefaultValue = new TimeOnly(1, 1, 3)
            }
#endif
            , new QueryArgument<VersionGraphType>
            {
                Name = "arg24",
                DefaultValue = new Version(1, 2, 3)
            }
            )
            .Resolve(ctx =>
            {
                var arg01 = ctx.GetArgument<BigInteger>("arg01");
                var arg02 = ctx.GetArgument<bool>("arg02");
                var arg03 = ctx.GetArgument<byte>("arg03");
                var arg04 = ctx.GetArgument<DateTime>("arg04");
                var arg05 = ctx.GetArgument<DateTime>("arg05");
                var arg06 = ctx.GetArgument<DateTimeOffset>("arg06");
                var arg07 = ctx.GetArgument<decimal>("arg07");
                var arg08 = ctx.GetArgument<float>("arg08");
                var arg09 = ctx.GetArgument<Guid>("arg09");
                var arg10 = ctx.GetArgument<string>("arg10");
                var arg11 = ctx.GetArgument<int>("arg11");
                var arg12 = ctx.GetArgument<long>("arg12");
                var arg13 = ctx.GetArgument<sbyte>("arg13");
                var arg14 = ctx.GetArgument<short>("arg14");
                var arg15 = ctx.GetArgument<string>("arg15");
                var arg16 = ctx.GetArgument<TimeSpan>("arg16");
                var arg17 = ctx.GetArgument<TimeSpan>("arg17");
                var arg18 = ctx.GetArgument<uint>("arg18");
                var arg19 = ctx.GetArgument<ulong>("arg19");
                var arg20 = ctx.GetArgument<Uri>("arg20");
                var arg21 = ctx.GetArgument<ushort>("arg21");
#if NET6_0_OR_GREATER
                var arg22 = ctx.GetArgument<DateOnly>("arg22");
                var arg23 = ctx.GetArgument<TimeOnly>("arg23");
#endif
                var arg24 = ctx.GetArgument<Version>("arg24");

                SuppressNoUsageWarning(arg01);
                SuppressNoUsageWarning(arg02);
                SuppressNoUsageWarning(arg03);
                SuppressNoUsageWarning(arg04);
                SuppressNoUsageWarning(arg05);
                SuppressNoUsageWarning(arg06);
                SuppressNoUsageWarning(arg07);
                SuppressNoUsageWarning(arg08);
                SuppressNoUsageWarning(arg09);
                SuppressNoUsageWarning(arg10);
                SuppressNoUsageWarning(arg11);
                SuppressNoUsageWarning(arg12);
                SuppressNoUsageWarning(arg13);
                SuppressNoUsageWarning(arg14);
                SuppressNoUsageWarning(arg15);
                SuppressNoUsageWarning(arg16);
                SuppressNoUsageWarning(arg17);
                SuppressNoUsageWarning(arg18);
                SuppressNoUsageWarning(arg19);
                SuppressNoUsageWarning(arg20);
                SuppressNoUsageWarning(arg21);
#if NET6_0_OR_GREATER
                SuppressNoUsageWarning(arg22);
                SuppressNoUsageWarning(arg23);
#endif
                SuppressNoUsageWarning(arg24);

                return "";
            });
    }

    private static void SuppressNoUsageWarning<T>(T value)
    {
        if (value == null)
            throw new InvalidOperationException();
    }
}
