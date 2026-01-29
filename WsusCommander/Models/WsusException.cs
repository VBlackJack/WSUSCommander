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

using WsusCommander.Constants;

namespace WsusCommander.Models;

/// <summary>
/// Defines the error codes for WSUS operations.
/// </summary>
public enum WsusErrorCode
{
    /// <summary>Unknown error.</summary>
    Unknown = 0,

    /// <summary>Failed to connect to WSUS server.</summary>
    ConnectionFailed = 100,

    /// <summary>WSUS server is unavailable.</summary>
    ServerUnavailable = 101,

    /// <summary>Connection timed out.</summary>
    ConnectionTimeout = 102,

    /// <summary>SSL/TLS certificate validation failed.</summary>
    CertificateError = 103,

    /// <summary>User is not authorized to perform the operation.</summary>
    Unauthorized = 200,

    /// <summary>User authentication failed.</summary>
    AuthenticationFailed = 201,

    /// <summary>Update not found.</summary>
    UpdateNotFound = 300,

    /// <summary>Invalid update identifier.</summary>
    InvalidUpdateId = 301,

    /// <summary>Update is already approved.</summary>
    UpdateAlreadyApproved = 302,

    /// <summary>Update is already declined.</summary>
    UpdateAlreadyDeclined = 303,

    /// <summary>Computer group not found.</summary>
    GroupNotFound = 400,

    /// <summary>Invalid group identifier.</summary>
    InvalidGroupId = 401,

    /// <summary>Group operation failed.</summary>
    GroupOperationFailed = 402,

    /// <summary>Synchronization is already in progress.</summary>
    SyncInProgress = 500,

    /// <summary>Synchronization failed.</summary>
    SyncFailed = 501,

    /// <summary>PowerShell script not found.</summary>
    ScriptNotFound = 600,

    /// <summary>PowerShell execution error.</summary>
    PowerShellError = 601,

    /// <summary>Script timeout.</summary>
    ScriptTimeout = 602,

    /// <summary>Input validation failed.</summary>
    ValidationError = 700,

    /// <summary>Invalid configuration.</summary>
    ConfigurationError = 701,

    /// <summary>Invalid input provided.</summary>
    InvalidInput = 702,

    /// <summary>Operation failed.</summary>
    OperationFailed = 703,

    /// <summary>Script execution failed.</summary>
    ScriptExecutionFailed = 604,

    /// <summary>Operation was cancelled.</summary>
    OperationCancelled = 800,

    /// <summary>Operation timed out.</summary>
    OperationTimeout = 801,

    /// <summary>Maximum retry attempts exceeded.</summary>
    RetryLimitExceeded = 802
}

/// <summary>
/// Exception class for WSUS-related errors with structured error information.
/// </summary>
public class WsusException : Exception
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public WsusErrorCode ErrorCode { get; }

    /// <summary>
    /// Gets a value indicating whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Gets the suggested delay before retrying the operation.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Gets additional context data for the error.
    /// </summary>
    public Dictionary<string, object> Context { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="WsusException"/> class.
    /// </summary>
    public WsusException(WsusErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        IsRetryable = DetermineRetryable(errorCode);
        RetryAfter = DetermineRetryDelay(errorCode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WsusException"/> class with inner exception.
    /// </summary>
    public WsusException(WsusErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        IsRetryable = DetermineRetryable(errorCode);
        RetryAfter = DetermineRetryDelay(errorCode);
    }

    /// <summary>
    /// Adds context data to the exception.
    /// </summary>
    public WsusException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Determines if the error code indicates a retryable error.
    /// </summary>
    private static bool DetermineRetryable(WsusErrorCode errorCode)
    {
        return errorCode switch
        {
            WsusErrorCode.ConnectionTimeout => true,
            WsusErrorCode.ServerUnavailable => true,
            WsusErrorCode.SyncInProgress => true,
            WsusErrorCode.ScriptTimeout => true,
            WsusErrorCode.OperationTimeout => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines the suggested retry delay for the error code.
    /// </summary>
    private static TimeSpan? DetermineRetryDelay(WsusErrorCode errorCode)
    {
        return errorCode switch
        {
            WsusErrorCode.ConnectionTimeout => TimeSpan.FromSeconds(AppConstants.RetryDelays.ConnectionTimeoutSeconds),
            WsusErrorCode.ServerUnavailable => TimeSpan.FromSeconds(AppConstants.RetryDelays.ServerUnavailableSeconds),
            WsusErrorCode.SyncInProgress => TimeSpan.FromMinutes(AppConstants.RetryDelays.SyncInProgressMinutes),
            WsusErrorCode.ScriptTimeout => TimeSpan.FromSeconds(AppConstants.RetryDelays.ScriptTimeoutSeconds),
            WsusErrorCode.OperationTimeout => TimeSpan.FromSeconds(AppConstants.RetryDelays.OperationTimeoutSeconds),
            _ => null
        };
    }
}

/// <summary>
/// Exception for validation errors.
/// </summary>
public class ValidationException : WsusException
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base(WsusErrorCode.ValidationError, "Validation failed.")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}

/// <summary>
/// Represents a validation error for a specific field.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the name of the field that failed validation.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the attempted value that failed validation.
    /// </summary>
    public object? AttemptedValue { get; init; }
}
