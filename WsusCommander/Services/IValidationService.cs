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

using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Interface for input validation service.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates an object using DataAnnotations.
    /// </summary>
    /// <typeparam name="T">Type of object to validate.</typeparam>
    /// <param name="obj">Object to validate.</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    IReadOnlyList<ValidationError> Validate<T>(T obj) where T : class;

    /// <summary>
    /// Validates an object and throws if invalid.
    /// </summary>
    /// <typeparam name="T">Type of object to validate.</typeparam>
    /// <param name="obj">Object to validate.</param>
    /// <exception cref="ValidationException">Thrown if validation fails.</exception>
    void ValidateAndThrow<T>(T obj) where T : class;

    /// <summary>
    /// Validates a GUID string.
    /// </summary>
    /// <param name="value">String value to validate.</param>
    /// <param name="fieldName">Name of the field for error messages.</param>
    /// <returns>Validation error or null if valid.</returns>
    ValidationError? ValidateGuid(string value, string fieldName);

    /// <summary>
    /// Validates a server name/hostname.
    /// </summary>
    /// <param name="value">Server name to validate.</param>
    /// <returns>Validation error or null if valid.</returns>
    ValidationError? ValidateServerName(string value);

    /// <summary>
    /// Validates a port number.
    /// </summary>
    /// <param name="value">Port number to validate.</param>
    /// <returns>Validation error or null if valid.</returns>
    ValidationError? ValidatePort(int value);

    /// <summary>
    /// Sanitizes a string for safe use in operations.
    /// </summary>
    /// <param name="input">Input string to sanitize.</param>
    /// <returns>Sanitized string.</returns>
    string Sanitize(string input);
}
