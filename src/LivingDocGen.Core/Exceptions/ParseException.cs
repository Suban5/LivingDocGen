using System;
using System.Text;

namespace LivingDocGen.Core.Exceptions;

/// <summary>
/// Exception thrown when parsing fails
/// </summary>
public class ParseException : BDDException
{
    /// <summary>
    /// Gets or sets the file path where the parse error occurred.
    /// </summary>
    public string FilePath { get; set; }
    
    /// <summary>
    /// Gets or sets the line number where the parse error occurred.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class.
    /// </summary>
    public ParseException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ParseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ParseException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class with error details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="filePath">The file path where the error occurred.</param>
    /// <param name="lineNumber">The line number where the error occurred.</param>
    public ParseException(string message, string filePath, int? lineNumber = null)
        : base(message)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the exception including file path and line number if available.
    /// </summary>
    /// <returns>A string that represents the exception.</returns>
    public override string ToString()
    {
        var baseMessage = base.ToString();
        if (!string.IsNullOrEmpty(FilePath))
        {
            baseMessage += $"\nFile: {FilePath}";
            if (LineNumber.HasValue)
            {
                baseMessage += $" (Line {LineNumber})";
            }
        }
        return baseMessage;
    }
}
