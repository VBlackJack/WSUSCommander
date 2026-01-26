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

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WsusCommander.Services;

/// <summary>
/// Secure storage service using DPAPI for encryption.
/// </summary>
public sealed class SecureStorageService : ISecureStorageService
{
    private readonly string _storagePath;
    private readonly ILoggingService _loggingService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureStorageService"/> class.
    /// </summary>
    public SecureStorageService(IConfigurationService configService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _storagePath = Path.Combine(configService.AppSettings.DataPath, "secure.dat");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc/>
    public async Task StoreAsync(string key, string value)
    {
        await _lock.WaitAsync();
        try
        {
            var storage = await LoadStorageAsync();
            storage[key] = Encrypt(value);
            await SaveStorageAsync(storage);
            await _loggingService.LogDebugAsync($"Secure storage: stored key '{key}'");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RetrieveAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var storage = await LoadStorageAsync();
            if (storage.TryGetValue(key, out var encryptedValue))
            {
                return Decrypt(encryptedValue);
            }
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var storage = await LoadStorageAsync();
            if (storage.Remove(key))
            {
                await SaveStorageAsync(storage);
                await _loggingService.LogDebugAsync($"Secure storage: removed key '{key}'");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var storage = await LoadStorageAsync();
            return storage.ContainsKey(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public string Encrypt(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedBytes = ProtectedData.Protect(
            dataBytes,
            null,
            DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(encryptedBytes);
    }

    /// <inheritdoc/>
    public string Decrypt(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
        {
            return string.Empty;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException ex)
        {
            _loggingService.LogErrorAsync("Failed to decrypt secure data", ex);
            throw;
        }
    }

    private async Task<Dictionary<string, string>> LoadStorageAsync()
    {
        if (!File.Exists(_storagePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var content = await File.ReadAllTextAsync(_storagePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(content)
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load secure storage: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    private async Task SaveStorageAsync(Dictionary<string, string> storage)
    {
        var content = JsonSerializer.Serialize(storage, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        await File.WriteAllTextAsync(_storagePath, content);
    }
}
