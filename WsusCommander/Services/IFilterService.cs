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
/// Filter criteria for updates.
/// </summary>
public sealed class UpdateFilterCriteria
{
    /// <summary>
    /// Gets or sets the search text (matches title, KB, description).
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the classification filter.
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Gets or sets whether to show only approved updates.
    /// </summary>
    public bool? IsApproved { get; set; }

    /// <summary>
    /// Gets or sets whether to show only declined updates.
    /// </summary>
    public bool? IsDeclined { get; set; }

    /// <summary>
    /// Gets or sets the minimum creation date.
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum creation date.
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Gets or sets the product filter.
    /// </summary>
    public string? Product { get; set; }

    /// <summary>
    /// Gets or sets whether to hide superseded updates.
    /// </summary>
    public bool HideSuperseded { get; set; }
}

/// <summary>
/// Filter criteria for computers.
/// </summary>
public sealed class ComputerFilterCriteria
{
    /// <summary>
    /// Gets or sets the search text (matches name, IP).
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the group filter.
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Gets or sets whether to show only computers with needed updates.
    /// </summary>
    public bool HasNeededUpdates { get; set; }

    /// <summary>
    /// Gets or sets whether to show only computers with failed updates.
    /// </summary>
    public bool HasFailedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the minimum last reported date.
    /// </summary>
    public DateTime? ReportedAfter { get; set; }

    /// <summary>
    /// Gets or sets whether to show only stale computers (not reported recently).
    /// </summary>
    public bool ShowStaleOnly { get; set; }

    /// <summary>
    /// Gets or sets the stale threshold in days.
    /// </summary>
    public int StaleDays { get; set; } = 30;
}

/// <summary>
/// Sort direction.
/// </summary>
public enum SortDirection
{
    /// <summary>Ascending order.</summary>
    Ascending,

    /// <summary>Descending order.</summary>
    Descending
}

/// <summary>
/// Sort specification.
/// </summary>
public sealed class SortCriteria
{
    /// <summary>
    /// Gets or sets the property name to sort by.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}

/// <summary>
/// Interface for filtering and search service.
/// </summary>
public interface IFilterService
{
    /// <summary>
    /// Filters updates based on criteria.
    /// </summary>
    /// <param name="updates">Updates to filter.</param>
    /// <param name="criteria">Filter criteria.</param>
    /// <returns>Filtered updates.</returns>
    IEnumerable<WsusUpdate> FilterUpdates(IEnumerable<WsusUpdate> updates, UpdateFilterCriteria criteria);

    /// <summary>
    /// Filters computers based on criteria.
    /// </summary>
    /// <param name="computers">Computers to filter.</param>
    /// <param name="criteria">Filter criteria.</param>
    /// <returns>Filtered computers.</returns>
    IEnumerable<ComputerStatus> FilterComputers(IEnumerable<ComputerStatus> computers, ComputerFilterCriteria criteria);

    /// <summary>
    /// Sorts a collection by the specified criteria.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="items">Items to sort.</param>
    /// <param name="sortCriteria">Sort criteria.</param>
    /// <returns>Sorted items.</returns>
    IEnumerable<T> Sort<T>(IEnumerable<T> items, SortCriteria sortCriteria);

    /// <summary>
    /// Performs a fuzzy search on updates.
    /// </summary>
    /// <param name="updates">Updates to search.</param>
    /// <param name="searchText">Search text.</param>
    /// <returns>Matching updates ordered by relevance.</returns>
    IEnumerable<WsusUpdate> FuzzySearch(IEnumerable<WsusUpdate> updates, string searchText);

    /// <summary>
    /// Gets distinct classifications from updates.
    /// </summary>
    /// <param name="updates">Updates to extract classifications from.</param>
    /// <returns>Distinct classification names.</returns>
    IEnumerable<string> GetDistinctClassifications(IEnumerable<WsusUpdate> updates);

    /// <summary>
    /// Gets distinct products from updates.
    /// </summary>
    /// <param name="updates">Updates to extract products from.</param>
    /// <returns>Distinct product names.</returns>
    IEnumerable<string> GetDistinctProducts(IEnumerable<WsusUpdate> updates);
}
