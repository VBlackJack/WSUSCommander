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
using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Services;

/// <summary>
/// Email notification service implementation.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="configurationService">Configuration service.</param>
    /// <param name="loggingService">Logging service.</param>
    public EmailService(IConfigurationService configurationService, ILoggingService loggingService)
    {
        _configurationService = configurationService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task EvaluateAndSendAlertsAsync(EmailAlertContext context, CancellationToken cancellationToken = default)
    {
        var config = _configurationService.Email;
        if (!config.Enabled)
        {
            return;
        }

        if (context.CriticalUpdatesPendingDays >= config.CriticalUpdateThresholdDays)
        {
            var subject = string.Format(Resources.EmailSubjectCriticalUpdates, context.CriticalUpdatesPendingDays);
            var body = string.Format(Resources.EmailBodyCriticalUpdates, context.CriticalUpdatesPendingDays);
            await SendAlertAsync(subject, body, cancellationToken);
        }

        if (context.HasSyncFailure)
        {
            var subject = Resources.EmailSubjectSyncFailure;
            var body = string.Format(Resources.EmailBodySyncFailure, context.Details ?? Resources.HealthStatusUnknown);
            await SendAlertAsync(subject, body, cancellationToken);
        }

        if (context.CompliancePercent < config.ComplianceThresholdPercent)
        {
            var subject = string.Format(Resources.EmailSubjectComplianceLow, context.CompliancePercent);
            var body = string.Format(Resources.EmailBodyComplianceLow, context.CompliancePercent);
            await SendAlertAsync(subject, body, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task SendAlertAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        var config = _configurationService.Email;
        if (!config.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(config.SmtpServer) || string.IsNullOrWhiteSpace(config.FromAddress))
        {
            await _loggingService.LogWarningAsync(Resources.EmailMissingConfiguration);
            return;
        }

        if (config.ToAddresses.Count == 0)
        {
            await _loggingService.LogWarningAsync(Resources.EmailMissingRecipients);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(config.FromAddress),
            Subject = subject,
            Body = body
        };

        foreach (var recipient in config.ToAddresses)
        {
            message.To.Add(recipient);
        }

        using var client = new SmtpClient(config.SmtpServer, config.SmtpPort)
        {
            EnableSsl = config.UseSsl
        };

        await client.SendMailAsync(message, cancellationToken);
        await _loggingService.LogInfoAsync(string.Format(Resources.LogEmailSent, subject));
    }
}
