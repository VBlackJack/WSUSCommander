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
/// Provides navigation and window management for the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Opens the computer updates window for the specified computer.
    /// </summary>
    /// <param name="computer">The selected computer.</param>
    /// <param name="updates">The updates associated with the computer.</param>
    void NavigateToComputerUpdates(ComputerStatus computer, IReadOnlyList<ComputerUpdateStatus> updates);
}
