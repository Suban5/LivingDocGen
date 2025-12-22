# LivingDocGen.Core

**LivingDocGen.Core** is the foundational library for the LivingDocGen ecosystem. It provides the shared infrastructure, base exceptions, and validation logic used across all other components (CLI, Parser, Generator, etc.).

## 🎯 Purpose

This project serves as the common dependency for the solution, ensuring consistent error handling and validation rules throughout the application. It is designed to be lightweight and has minimal external dependencies.

## 📦 Components

### 1. Exceptions
Provides a hierarchy of strongly-typed exceptions to handle specific failure scenarios in the BDD documentation process.

*   **`BDDException`**: The base class for all application-specific exceptions.
*   **`ConfigurationException`**: Thrown when configuration files are missing, invalid, or malformed.
*   **`ParseException`**: Thrown when parsing feature files or test results fails.
*   **`ValidationException`**: Thrown when input validation (files, arguments) fails.

### 2. Validators
Static helper classes for robust input validation.

*   **`FileValidator`**:
    *   `ValidateFileExists(string path, string? extension)`: Ensures a file exists and optionally checks its extension.
    *   `ValidateDirectoryExists(string path)`: Ensures a directory exists.

## 🛠 Dependencies

*   `Microsoft.Extensions.Logging.Abstractions`: For common logging interfaces.
*   **.NET 8.0**: The target framework.

## 💻 Usage

This library is primarily intended for internal use within the LivingDocGen solution.

```csharp
using LivingDocGen.Core.Validators;
using LivingDocGen.Core.Exceptions;

try 
{
    // Validate input
    FileValidator.ValidateFileExists("features/login.feature", ".feature");
}
catch (ValidationException ex)
{
    // Handle validation error consistently
    logger.LogError(ex.Message);
}
```

## 📝 Todo List

- [ ] Add more comprehensive file path validators (e.g., validate writable directories).
- [ ] Implement retry logic for transient file system errors.
- [ ] Add configuration validation helpers.
