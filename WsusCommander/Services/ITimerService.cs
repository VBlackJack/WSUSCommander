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
/// Interface for the timer service used for auto-refresh functionality.
/// </summary>
public interface ITimerService
{
    /// <summary>
    /// Event raised when the timer interval elapses.
    /// </summary>
    event EventHandler? Tick;

    /// <summary>
    /// Gets or sets the interval between tick events in milliseconds.
    /// </summary>
    double Interval { get; set; }

    /// <summary>
    /// Gets a value indicating whether the timer is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();
}
