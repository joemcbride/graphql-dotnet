namespace GraphQL.Validation.Complexity
{
    public class ComplexityConfiguration
    {
        public int? MaxDepth { get; set; }

        public int? MaxComplexity { get; set; }

        /// <summary>
        /// Hardcoded maximum number of objects returned by each field.
        /// If there is no hardcoded maximum then use the average number of rows/objects returned by each field.
        /// </summary>
        public double? FieldImpact { get; set; }

        /// <summary>
        /// Max number of times to traverse tree nodes. GraphiQL queries take ~95 iterations, adjust as needed.
        /// </summary>
        public int MaxRecursionCount { get; set; } = 250;
    }
}
