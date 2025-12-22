using System;

namespace LivingDocGen.Core.Exceptions;

/// <summary>
/// Base exception for all BDD framework exceptions
/// </summary>
public class BDDException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BDDException"/> class.
    /// </summary>
    public BDDException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BDDException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BDDException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BDDException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BDDException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
