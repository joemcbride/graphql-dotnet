using System;
using System.Numerics;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class AllScalarGraphTypeTests
    {
        /*  This class tests the following scalar types:
         *    ByteGraphType
         *    SByteGraphType
         *    ShortGraphType
         *    UShortGraphType
         *    IntGraphType
         *    UIntGraphType
         *    LongGraphType
         *    ULongGraphType
         *    
         *    BooleanGraphType
         *    FloatGraphType
         *    StringGraphType
         *    
         *  Does not test:
         *    DecimalGraphType
         *    UriGraphType
         *    date/time graph types
         *    enumeration graph types
         *    
         *  Does test ALL scalars' handling of null
         *  
         */

        [Theory]
        [InlineData(typeof(BooleanGraphType))]
        [InlineData(typeof(ByteGraphType))]
        [InlineData(typeof(SByteGraphType))]
        [InlineData(typeof(ShortGraphType))]
        [InlineData(typeof(UShortGraphType))]
        [InlineData(typeof(IntGraphType))]
        [InlineData(typeof(UIntGraphType))]
        [InlineData(typeof(LongGraphType))]
        [InlineData(typeof(ULongGraphType))]
        [InlineData(typeof(BigIntGraphType))]
        [InlineData(typeof(DateGraphType))]
        [InlineData(typeof(DateTimeGraphType))]
        [InlineData(typeof(DateTimeOffsetGraphType))]
        [InlineData(typeof(TimeSpanSecondsGraphType))]
        [InlineData(typeof(TimeSpanMillisecondsGraphType))]
        [InlineData(typeof(IdGraphType))]
        [InlineData(typeof(StringGraphType))]
        [InlineData(typeof(UriGraphType))]
        [InlineData(typeof(GuidGraphType))]
        [InlineData(typeof(FloatGraphType))]
        [InlineData(typeof(DecimalGraphType))]
        [InlineData(typeof(EnumerationGraphType))]
        public void allow_null(Type graphType)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            g.ParseValue(null).ShouldBeNull();
            g.CanParseValue(null).ShouldBeTrue();
            g.ParseLiteral(new NullValue()).ShouldBeNull();
            g.CanParseLiteral(new NullValue()).ShouldBeTrue();
            g.Serialize(null).ShouldBeNull();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType))]
        [InlineData(typeof(ByteGraphType))]
        [InlineData(typeof(SByteGraphType))]
        [InlineData(typeof(ShortGraphType))]
        [InlineData(typeof(UShortGraphType))]
        [InlineData(typeof(IntGraphType))]
        [InlineData(typeof(UIntGraphType))]
        [InlineData(typeof(LongGraphType))]
        [InlineData(typeof(ULongGraphType))]
        [InlineData(typeof(BigIntGraphType))]
        [InlineData(typeof(FloatGraphType))]
        [InlineData(typeof(DecimalGraphType))]
        public void does_not_coerce_string(Type graphType)
        {
            // if string to coercion were possible, all would pass, as the string is "0"
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            g.CanParseLiteral(new StringValue("0")).ShouldBeFalse();
            Should.Throw<ArgumentException>(() => g.ParseLiteral(new StringValue("0")));
            g.CanParseValue("0").ShouldBeFalse();
            Should.Throw<ArgumentException>(() => g.ParseValue("0"));
            Should.Throw<ArgumentException>(() => g.Serialize("0"));
        }

        [Theory]
        [InlineData(typeof(ByteGraphType), (byte)0)]
        [InlineData(typeof(ByteGraphType), (byte)1)]
        [InlineData(typeof(ByteGraphType), (byte)255)]
        [InlineData(typeof(SByteGraphType), (sbyte)-128)]
        [InlineData(typeof(SByteGraphType), (sbyte)0)]
        [InlineData(typeof(SByteGraphType), (sbyte)127)]
        [InlineData(typeof(ShortGraphType), short.MinValue)]
        [InlineData(typeof(ShortGraphType), (short)default)]
        [InlineData(typeof(ShortGraphType), short.MaxValue)]
        [InlineData(typeof(UShortGraphType), ushort.MinValue)]
        [InlineData(typeof(UShortGraphType), ushort.MaxValue)]
        [InlineData(typeof(IntGraphType), int.MinValue)]
        [InlineData(typeof(IntGraphType), (int)default)]
        [InlineData(typeof(IntGraphType), int.MaxValue)]
        [InlineData(typeof(UIntGraphType), uint.MinValue)]
        [InlineData(typeof(UIntGraphType), uint.MaxValue)]
        [InlineData(typeof(LongGraphType), long.MinValue)]
        [InlineData(typeof(LongGraphType), (long)default)]
        [InlineData(typeof(LongGraphType), long.MaxValue)]
        [InlineData(typeof(ULongGraphType), ulong.MinValue)]
        [InlineData(typeof(ULongGraphType), ulong.MaxValue)]
        [InlineData(typeof(FloatGraphType), -2.0)]
        [InlineData(typeof(FloatGraphType), 2.0)]
        public void parseValue_ok(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var types = new Type[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(BigInteger)
            };

            foreach (var type in types)
            {
                object converted;
                try
                {
                    converted = Convert.ChangeType(value, type);
                }
                catch
                {
                    continue;
                }
                g.CanParseValue(converted).ShouldBeTrue();
                var parsed = g.ParseValue(converted);
                parsed.ShouldBeOfType(value.GetType()); // be sure that the correct type is returned
                parsed.ShouldBe(value);
            }
        }

        [Theory]
        [InlineData(typeof(ByteGraphType), (byte)0)]
        [InlineData(typeof(ByteGraphType), (byte)1)]
        [InlineData(typeof(ByteGraphType), (byte)255)]
        [InlineData(typeof(SByteGraphType), (sbyte)-128)]
        [InlineData(typeof(SByteGraphType), (sbyte)0)]
        [InlineData(typeof(SByteGraphType), (sbyte)127)]
        [InlineData(typeof(ShortGraphType), short.MinValue)]
        [InlineData(typeof(ShortGraphType), (short)default)]
        [InlineData(typeof(ShortGraphType), short.MaxValue)]
        [InlineData(typeof(UShortGraphType), ushort.MinValue)]
        [InlineData(typeof(UShortGraphType), ushort.MaxValue)]
        [InlineData(typeof(IntGraphType), int.MinValue)]
        [InlineData(typeof(IntGraphType), (int)default)]
        [InlineData(typeof(IntGraphType), int.MaxValue)]
        [InlineData(typeof(UIntGraphType), uint.MinValue)]
        [InlineData(typeof(UIntGraphType), uint.MaxValue)]
        [InlineData(typeof(LongGraphType), long.MinValue)]
        [InlineData(typeof(LongGraphType), (long)default)]
        [InlineData(typeof(LongGraphType), long.MaxValue)]
        [InlineData(typeof(ULongGraphType), ulong.MinValue)]
        [InlineData(typeof(ULongGraphType), ulong.MaxValue)]
        [InlineData(typeof(FloatGraphType), -2.0)]
        [InlineData(typeof(FloatGraphType), 2.0)]
        public void parseLiteral_ok(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var valueCasts = new Func<object, IValue>[]
            {
                n => new IntValue(Convert.ToInt32(n)),
                n => new LongValue(Convert.ToInt64(n)),
                n => n is ulong ul ? new BigIntValue(ul) : new BigIntValue(Convert.ToInt64(n))
            };

            foreach (var getValue in valueCasts)
            {
                IValue astValue;
                try
                {
                    astValue = getValue(value);
                }
                catch
                {
                    continue;
                }

                g.CanParseLiteral(astValue).ShouldBeTrue();
                var parsed = g.ParseLiteral(astValue);
                parsed.ShouldBeOfType(value.GetType()); // be sure that the correct type is returned
                parsed.ShouldBe(value);
            }
        }

        [Theory]
        [InlineData(typeof(ByteGraphType), (byte)0)]
        [InlineData(typeof(ByteGraphType), (byte)1)]
        [InlineData(typeof(ByteGraphType), (byte)255)]
        [InlineData(typeof(SByteGraphType), (sbyte)-128)]
        [InlineData(typeof(SByteGraphType), (sbyte)0)]
        [InlineData(typeof(SByteGraphType), (sbyte)127)]
        [InlineData(typeof(ShortGraphType), short.MinValue)]
        [InlineData(typeof(ShortGraphType), (short)default)]
        [InlineData(typeof(ShortGraphType), short.MaxValue)]
        [InlineData(typeof(UShortGraphType), ushort.MinValue)]
        [InlineData(typeof(UShortGraphType), ushort.MaxValue)]
        [InlineData(typeof(IntGraphType), int.MinValue)]
        [InlineData(typeof(IntGraphType), (int)default)]
        [InlineData(typeof(IntGraphType), int.MaxValue)]
        [InlineData(typeof(UIntGraphType), uint.MinValue)]
        [InlineData(typeof(UIntGraphType), uint.MaxValue)]
        [InlineData(typeof(LongGraphType), long.MinValue)]
        [InlineData(typeof(LongGraphType), (long)default)]
        [InlineData(typeof(LongGraphType), long.MaxValue)]
        [InlineData(typeof(ULongGraphType), ulong.MinValue)]
        [InlineData(typeof(ULongGraphType), ulong.MaxValue)]
        [InlineData(typeof(FloatGraphType), -2.0)]
        [InlineData(typeof(FloatGraphType), 2.0)]
        public void serialize_ok(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var types = new Type[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(BigInteger)
            };

            foreach (var type in types)
            {
                object converted;
                try
                {
                    converted = Convert.ChangeType(value, type);
                }
                catch
                {
                    continue;
                }
                var parsed = g.Serialize(converted);
                parsed.ShouldBeOfType(value.GetType()); // be sure that the correct type is returned
                parsed.ShouldBe(value);
            }
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), true, true)]
        [InlineData(typeof(BooleanGraphType), false, false)]
        [InlineData(typeof(StringGraphType), "abc", "abc")]
        [InlineData(typeof(IdGraphType), 2, 2)]
        [InlineData(typeof(IdGraphType), "3", "3")]
        [InlineData(typeof(FloatGraphType), 3.5, 3.5)]
        public void parseValue_other_ok(Type graphType, object value, object parsed)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var ret = g.ParseValue(value);
            ret.ShouldBeOfType(parsed.GetType());
            ret.ShouldBe(parsed);
            g.CanParseValue(value).ShouldBeTrue();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), true, true)]
        [InlineData(typeof(BooleanGraphType), false, false)]
        [InlineData(typeof(StringGraphType), "abc", "abc")]
        [InlineData(typeof(IdGraphType), 2, 2)]
        [InlineData(typeof(IdGraphType), "3", "3")]
        [InlineData(typeof(FloatGraphType), 3.5, 3.5)]
        public void parseLiteral_other_ok(Type graphType, object value, object parsed)
        {
            IValue astValue = value switch
            {
                int i => new IntValue(i),
                long l => new LongValue(l),
                bool b => new BooleanValue(b),
                double f => new FloatValue(f),
                string s => new StringValue(s),
                _ => null
            };

            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var ret = g.ParseLiteral(astValue);
            ret.ShouldBeOfType(parsed.GetType());
            ret.ShouldBe(parsed);
            g.CanParseLiteral(astValue).ShouldBeTrue();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), true, true)]
        [InlineData(typeof(BooleanGraphType), false, false)]
        [InlineData(typeof(StringGraphType), "abc", "abc")]
        [InlineData(typeof(IdGraphType), 2, "2")]
        [InlineData(typeof(IdGraphType), "3", "3")]
        [InlineData(typeof(FloatGraphType), 3.5, 3.5)]
        public void serialize_other_ok(Type graphType, object value, object serialized)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var ret = g.Serialize(value);
            ret.ShouldBeOfType(serialized.GetType());
            ret.ShouldBe(serialized);
            g.CanParseValue(value).ShouldBeTrue();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(BooleanGraphType), "false")]
        [InlineData(typeof(BooleanGraphType), "False")]
        [InlineData(typeof(BooleanGraphType), "true")]
        [InlineData(typeof(BooleanGraphType), "True")]
        [InlineData(typeof(StringGraphType), 0)]
        [InlineData(typeof(StringGraphType), false)]
        [InlineData(typeof(ByteGraphType), false)]
        [InlineData(typeof(SByteGraphType), false)]
        [InlineData(typeof(ShortGraphType), false)]
        [InlineData(typeof(UShortGraphType), false)]
        [InlineData(typeof(IntGraphType), false)]
        [InlineData(typeof(UIntGraphType), false)]
        [InlineData(typeof(LongGraphType), false)]
        [InlineData(typeof(ULongGraphType), false)]
        [InlineData(typeof(BigIntGraphType), false)]
        [InlineData(typeof(StringGraphType), 1.5)]
        [InlineData(typeof(ByteGraphType), 1.5)]
        [InlineData(typeof(SByteGraphType), 1.5)]
        [InlineData(typeof(ShortGraphType), 1.5)]
        [InlineData(typeof(UShortGraphType), 1.5)]
        [InlineData(typeof(IntGraphType), 1.5)]
        [InlineData(typeof(UIntGraphType), 1.5)]
        [InlineData(typeof(LongGraphType), 1.5)]
        [InlineData(typeof(ULongGraphType), 1.5)]
        [InlineData(typeof(BigIntGraphType), 1.5)]
        public void parseValue_other_fail(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            Should.Throw<Exception>(() => g.ParseValue(value));
            g.CanParseValue(value).ShouldBeFalse();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(BooleanGraphType), "false")]
        [InlineData(typeof(BooleanGraphType), "False")]
        [InlineData(typeof(BooleanGraphType), "true")]
        [InlineData(typeof(BooleanGraphType), "True")]
        [InlineData(typeof(StringGraphType), 0)]
        [InlineData(typeof(StringGraphType), false)]
        [InlineData(typeof(ByteGraphType), false)]
        [InlineData(typeof(SByteGraphType), false)]
        [InlineData(typeof(ShortGraphType), false)]
        [InlineData(typeof(UShortGraphType), false)]
        [InlineData(typeof(IntGraphType), false)]
        [InlineData(typeof(UIntGraphType), false)]
        [InlineData(typeof(LongGraphType), false)]
        [InlineData(typeof(ULongGraphType), false)]
        [InlineData(typeof(BigIntGraphType), false)]
        [InlineData(typeof(StringGraphType), 1.5)]
        [InlineData(typeof(ByteGraphType), 1.5)]
        [InlineData(typeof(SByteGraphType), 1.5)]
        [InlineData(typeof(ShortGraphType), 1.5)]
        [InlineData(typeof(UShortGraphType), 1.5)]
        [InlineData(typeof(IntGraphType), 1.5)]
        [InlineData(typeof(UIntGraphType), 1.5)]
        [InlineData(typeof(LongGraphType), 1.5)]
        [InlineData(typeof(ULongGraphType), 1.5)]
        [InlineData(typeof(BigIntGraphType), 1.5)]
        public void parseLiteral_other_fail(Type graphType, object value)
        {
            IValue astValue = value switch
            {
                int i => new IntValue(i),
                long l => new LongValue(l),
                bool b => new BooleanValue(b),
                double d => new FloatValue(d),
                string s => new StringValue(s),
                _ => null
            };

            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            Should.Throw<Exception>(() => g.ParseLiteral(astValue));
            g.CanParseLiteral(astValue).ShouldBeFalse();
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(BooleanGraphType), "false")]
        [InlineData(typeof(BooleanGraphType), "False")]
        [InlineData(typeof(BooleanGraphType), "true")]
        [InlineData(typeof(BooleanGraphType), "True")]
        [InlineData(typeof(StringGraphType), 0)]
        [InlineData(typeof(StringGraphType), false)]
        [InlineData(typeof(ByteGraphType), false)]
        [InlineData(typeof(SByteGraphType), false)]
        [InlineData(typeof(ShortGraphType), false)]
        [InlineData(typeof(UShortGraphType), false)]
        [InlineData(typeof(IntGraphType), false)]
        [InlineData(typeof(UIntGraphType), false)]
        [InlineData(typeof(LongGraphType), false)]
        [InlineData(typeof(ULongGraphType), false)]
        [InlineData(typeof(BigIntGraphType), false)]
        [InlineData(typeof(StringGraphType), 1.5)]
        [InlineData(typeof(ByteGraphType), 1.5)]
        [InlineData(typeof(SByteGraphType), 1.5)]
        [InlineData(typeof(ShortGraphType), 1.5)]
        [InlineData(typeof(UShortGraphType), 1.5)]
        [InlineData(typeof(IntGraphType), 1.5)]
        [InlineData(typeof(UIntGraphType), 1.5)]
        [InlineData(typeof(LongGraphType), 1.5)]
        [InlineData(typeof(ULongGraphType), 1.5)]
        [InlineData(typeof(BigIntGraphType), 1.5)]
        public void serialize_other_fail(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            Should.Throw<Exception>(() => g.Serialize(value));
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(SByteGraphType), -129)]
        [InlineData(typeof(SByteGraphType), 128)]
        [InlineData(typeof(ByteGraphType), -1)]
        [InlineData(typeof(ByteGraphType), 256)]
        [InlineData(typeof(ShortGraphType), -32769)]
        [InlineData(typeof(ShortGraphType), 32768)]
        [InlineData(typeof(UShortGraphType), -1)]
        [InlineData(typeof(UShortGraphType), 65536)]
        [InlineData(typeof(IntGraphType), long.MinValue)]
        [InlineData(typeof(IntGraphType), uint.MaxValue)]
        [InlineData(typeof(UIntGraphType), -1)]
        [InlineData(typeof(UIntGraphType), long.MaxValue)]
        [InlineData(typeof(LongGraphType), ulong.MaxValue)]
        public void parseValue_out_of_range(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var types = new Type[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(BigInteger)
            };

            foreach (var type in types)
            {
                object converted;
                try
                {
                    converted = Convert.ChangeType(value, type);
                }
                catch
                {
                    continue;
                }

                if (graphType == typeof(BooleanGraphType))
                {
                    Should.Throw<ArgumentException>(() => g.ParseValue(converted));
                }
                else
                {
                    Should.Throw<OverflowException>(() => g.ParseValue(converted));
                }
                g.CanParseValue(converted).ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(SByteGraphType), -129)]
        [InlineData(typeof(SByteGraphType), 128)]
        [InlineData(typeof(ByteGraphType), -1)]
        [InlineData(typeof(ByteGraphType), 256)]
        [InlineData(typeof(ShortGraphType), -32769)]
        [InlineData(typeof(ShortGraphType), 32768)]
        [InlineData(typeof(UShortGraphType), -1)]
        [InlineData(typeof(UShortGraphType), 65536)]
        [InlineData(typeof(IntGraphType), long.MinValue)]
        [InlineData(typeof(IntGraphType), uint.MaxValue)]
        [InlineData(typeof(UIntGraphType), -1)]
        [InlineData(typeof(UIntGraphType), long.MaxValue)]
        [InlineData(typeof(LongGraphType), ulong.MaxValue)]
        public void parseLiteral_out_of_range(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var valueCasts = new Func<object, IValue>[]
            {
                n => new IntValue(Convert.ToInt32(n)),
                n => new LongValue(Convert.ToInt64(n)),
                n => n is ulong ul ? new BigIntValue(ul) : new BigIntValue(Convert.ToInt64(n))
            };

            foreach (var getValue in valueCasts)
            {
                IValue astValue;
                try
                {
                    astValue = getValue(value);
                }
                catch
                {
                    continue;
                }

                if (graphType == typeof(BooleanGraphType))
                {
                    Should.Throw<ArgumentException>(() => g.ParseLiteral(astValue));
                }
                else
                {
                    Should.Throw<OverflowException>(() => g.ParseLiteral(astValue));
                }
                g.CanParseLiteral(astValue).ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData(typeof(BooleanGraphType), 0)]
        [InlineData(typeof(BooleanGraphType), 1)]
        [InlineData(typeof(SByteGraphType), -129)]
        [InlineData(typeof(SByteGraphType), 128)]
        [InlineData(typeof(ByteGraphType), -1)]
        [InlineData(typeof(ByteGraphType), 256)]
        [InlineData(typeof(ShortGraphType), -32769)]
        [InlineData(typeof(ShortGraphType), 32768)]
        [InlineData(typeof(UShortGraphType), -1)]
        [InlineData(typeof(UShortGraphType), 65536)]
        [InlineData(typeof(IntGraphType), long.MinValue)]
        [InlineData(typeof(IntGraphType), uint.MaxValue)]
        [InlineData(typeof(UIntGraphType), -1)]
        [InlineData(typeof(UIntGraphType), long.MaxValue)]
        [InlineData(typeof(LongGraphType), ulong.MaxValue)]
        public void serialize_out_of_range(Type graphType, object value)
        {
            var g = (ScalarGraphType)graphType.GetConstructor(Type.EmptyTypes).Invoke(null);
            var types = new Type[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(BigInteger)
            };

            foreach (var type in types)
            {
                object converted;
                try
                {
                    converted = Convert.ChangeType(value, type);
                }
                catch
                {
                    continue;
                }

                if (graphType == typeof(BooleanGraphType))
                {
                    Should.Throw<ArgumentException>(() => g.Serialize(converted));
                }
                else
                {
                    Should.Throw<OverflowException>(() => g.Serialize(converted));
                }
            }
        }

        [Fact]
        public void verify_integer_boxing()
        {
            var g = new IntGraphType();
            for (int i = -100; i < 100; ++i)
            {
                g.ParseLiteral(new IntValue(i)).ShouldBe(i);
            }
        }
    }
}
