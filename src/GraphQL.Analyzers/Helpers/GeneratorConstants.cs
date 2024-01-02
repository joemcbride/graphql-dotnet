namespace GraphQL.Analyzers.Helpers;

public static class GeneratorConstants
{
    // Workaround to get platform specific new line
    // because Environment.NewLine is banned in analyzers
    // https://github.com/dotnet/roslyn-analyzers/issues/6467
    public static readonly string NewLine = @"
";

    public const string SINGLE_INDENTATION = "    ";
    public const string DOUBLE_INDENTATION = SINGLE_INDENTATION + SINGLE_INDENTATION;
    public const string TRIPLE_INDENTATION = DOUBLE_INDENTATION + SINGLE_INDENTATION;
}
