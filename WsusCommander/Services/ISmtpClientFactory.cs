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

using System.Net.Mail;

namespace WsusCommander.Services;

/// <summary>
/// Factory for creating SMTP clients.
/// </summary>
public interface ISmtpClientFactory
{
    /// <summary>
    /// Creates an SMTP client configured for the server.
    /// </summary>
    /// <param name="server">SMTP server.</param>
    /// <param name="port">SMTP port.</param>
    /// <param name="useSsl">Whether to use SSL.</param>
    /// <returns>Configured SMTP client.</returns>
    SmtpClient Create(string server, int port, bool useSsl);
}
