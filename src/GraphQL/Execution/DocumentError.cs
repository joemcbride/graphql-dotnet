using System;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error generated while parsing or validating the document or its associated variables.
    /// </summary>
    public abstract class DocumentError : ExecutionError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentError"/> class with a specified error message.
        /// </summary>
        public DocumentError(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentError"/> class with a specified error message. Sets the
        /// <see cref="Code"/> and <see cref="Codes"/> properties based on the inner exception(s). Loads any exception data
        /// from the inner exception into this instance.
        /// </summary>
        public DocumentError(string message, Exception innerException) : base(message, innerException) { }
    }
}
