/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using WsusCommander.Models;
using ValidationException = WsusCommander.Models.ValidationException;
using ValidationError = WsusCommander.Models.ValidationError;

namespace WsusCommander.Services;

/// <summary>
/// Input validation service implementation.
/// </summary>
public sealed partial class ValidationService : IValidationService
{
    // Hostname regex: letters, numbers, hyphens, dots
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-\.]*[a-zA-Z0-9])?$", RegexOptions.Compiled)]
    private static partial Regex HostnameRegex();

    // Dangerous characters to sanitize
    private static readonly char[] DangerousChars = ['<', '>', '"', '\'', '&', '\0', '\r', '\n'];

    /// <inheritdoc/>
    public IReadOnlyList<ValidationError> Validate<T>(T obj) where T : class
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new ValidationContext(obj);
        var errors = new List<ValidationError>();

        Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

        foreach (var result in results)
        {
            errors.Add(new ValidationError
            {
                FieldName = result.MemberNames.FirstOrDefault() ?? "Unknown",
                Message = result.ErrorMessage ?? "Validation failed."
            });
        }

        return errors.AsReadOnly();
    }

    /// <inheritdoc/>
    public void ValidateAndThrow<T>(T obj) where T : class
    {
        var errors = Validate(obj);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    /// <inheritdoc/>
    public ValidationError? ValidateGuid(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ValidationError
            {
                FieldName = fieldName,
                Message = $"{fieldName} is required.",
                AttemptedValue = value
            };
        }

        if (!Guid.TryParse(value, out _))
        {
            return new ValidationError
            {
                FieldName = fieldName,
                Message = $"{fieldName} must be a valid GUID.",
                AttemptedValue = value
            };
        }

        return null;
    }

    /// <inheritdoc/>
    public ValidationError? ValidateServerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ValidationError
            {
                FieldName = "ServerName",
                Message = "Server name is required.",
                AttemptedValue = value
            };
        }

        if (value.Length > 255)
        {
            return new ValidationError
            {
                FieldName = "ServerName",
                Message = "Server name must not exceed 255 characters.",
                AttemptedValue = value
            };
        }

        // Check for valid hostname or IP address
        if (!HostnameRegex().IsMatch(value) && !System.Net.IPAddress.TryParse(value, out _))
        {
            return new ValidationError
            {
                FieldName = "ServerName",
                Message = "Server name must be a valid hostname or IP address.",
                AttemptedValue = value
            };
        }

        // Check for dangerous patterns
        if (ContainsDangerousPatterns(value))
        {
            return new ValidationError
            {
                FieldName = "ServerName",
                Message = "Server name contains invalid characters.",
                AttemptedValue = value
            };
        }

        return null;
    }

    /// <inheritdoc/>
    public ValidationError? ValidatePort(int value)
    {
        if (value < 1 || value > 65535)
        {
            return new ValidationError
            {
                FieldName = "Port",
                Message = "Port must be between 1 and 65535.",
                AttemptedValue = value
            };
        }

        return null;
    }

    /// <inheritdoc/>
    public string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove dangerous characters
        var sanitized = new string(input.Where(c => !DangerousChars.Contains(c)).ToArray());

        // Trim and limit length
        return sanitized.Trim().Length > 1000 ? sanitized[..1000] : sanitized.Trim();
    }

    private static bool ContainsDangerousPatterns(string value)
    {
        // Check for PowerShell/command injection patterns
        var dangerousPatterns = new[]
        {
            ";", "|", "&", "$", "`", "$(", "${",
            "\r", "\n", "\0"
        };

        return dangerousPatterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
