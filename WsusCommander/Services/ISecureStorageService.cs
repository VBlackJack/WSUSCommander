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

namespace WsusCommander.Services;

/// <summary>
/// Interface for secure storage of sensitive data using DPAPI.
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Stores a value securely.
    /// </summary>
    /// <param name="key">Storage key.</param>
    /// <param name="value">Value to store.</param>
    Task StoreAsync(string key, string value);

    /// <summary>
    /// Retrieves a securely stored value.
    /// </summary>
    /// <param name="key">Storage key.</param>
    /// <returns>The stored value or null if not found.</returns>
    Task<string?> RetrieveAsync(string key);

    /// <summary>
    /// Removes a stored value.
    /// </summary>
    /// <param name="key">Storage key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Checks if a key exists in secure storage.
    /// </summary>
    /// <param name="key">Storage key.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Encrypts data using DPAPI.
    /// </summary>
    /// <param name="data">Data to encrypt.</param>
    /// <returns>Encrypted data as Base64 string.</returns>
    string Encrypt(string data);

    /// <summary>
    /// Decrypts data using DPAPI.
    /// </summary>
    /// <param name="encryptedData">Encrypted Base64 data.</param>
    /// <returns>Decrypted data.</returns>
    string Decrypt(string encryptedData);
}
