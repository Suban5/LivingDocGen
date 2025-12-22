namespace LivingDocGen.Core.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : BDDException
{
    /// <summary>
    /// Gets or sets the list of validation error messages.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with validation errors.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errors">The list of validation error messages.</param>
    public ValidationException(string message, List<string> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    /// <summary>
    /// Returns a string representation of the exception including all validation errors.
    /// </summary>
    /// <returns>A string that represents the exception.</returns>
    public override string ToString()
    {
        var baseMessage = base.ToString();
        if (ValidationErrors.Any())
        {
            baseMessage += "\nValidation Errors:\n";
            baseMessage += string.Join("\n", ValidationErrors.Select(e => $"  - {e}"));
        }
        return baseMessage;
    }
}
