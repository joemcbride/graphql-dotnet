using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Byte scalar graph type represents an unsigned 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="byte"/> .NET values to this scalar graph type.
    /// </summary>
    public class ByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue ? (byte?)intValue.Value : null,
            LongValue longValue => byte.MinValue <= longValue.Value && longValue.Value <= byte.MaxValue ? (byte?)longValue.Value : null,
            BigIntValue bigIntValue => byte.MinValue <= bigIntValue.Value && bigIntValue.Value <= byte.MaxValue ? (byte?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(byte));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue,
            LongValue longValue => byte.MinValue <= longValue.Value && longValue.Value <= byte.MaxValue,
            BigIntValue bigIntValue => byte.MinValue <= bigIntValue.Value && bigIntValue.Value <= byte.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToByte(value));
    }
}
