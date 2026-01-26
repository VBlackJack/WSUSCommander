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

using System.Timers;
using Timer = System.Timers.Timer;

namespace WsusCommander.Services;

/// <summary>
/// Service responsible for timer-based auto-refresh functionality.
/// </summary>
public sealed class TimerService : ITimerService, IDisposable
{
    private readonly Timer _timer;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler? Tick;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerService"/> class.
    /// </summary>
    public TimerService()
    {
        _timer = new Timer();
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    /// <inheritdoc/>
    public double Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    /// <inheritdoc/>
    public bool IsRunning => _timer.Enabled;

    /// <inheritdoc/>
    public void Start()
    {
        if (!_disposed)
        {
            _timer.Start();
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_disposed)
        {
            _timer.Stop();
        }
    }

    /// <summary>
    /// Handles the timer elapsed event.
    /// </summary>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Disposes resources used by the timer service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
        _disposed = true;
    }
}
