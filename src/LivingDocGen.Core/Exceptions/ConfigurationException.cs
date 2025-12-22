using System;

namespace LivingDocGen.Core.Exceptions;

/// <summary>
/// Exception thrown when configuration is invalid
/// </summary>
public class ConfigurationException : BDDException
{
    /// <summary>
    /// Gets or sets the configuration key that caused the error.
    /// </summary>
    public string ConfigurationKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with configuration details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="configurationKey">The configuration key that caused the error.</param>
    public ConfigurationException(string message, string configurationKey)
        : base(message)
    {
        ConfigurationKey = configurationKey;
    }
}
