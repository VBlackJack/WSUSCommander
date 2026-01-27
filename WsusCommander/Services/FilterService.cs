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

using System.Reflection;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Filtering and search service implementation.
/// </summary>
public sealed class FilterService : IFilterService
{
    /// <inheritdoc/>
    public IEnumerable<WsusUpdate> FilterUpdates(IEnumerable<WsusUpdate> updates, UpdateFilterCriteria criteria)
    {
        var query = updates.AsEnumerable();

        // Text search
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var searchLower = criteria.SearchText.ToLowerInvariant();
            query = query.Where(u =>
                (u.Title?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.KbArticle?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Description?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Classification filter
        if (!string.IsNullOrWhiteSpace(criteria.Classification))
        {
            query = query.Where(u =>
                u.Classification?.Equals(criteria.Classification, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // Approval status
        if (criteria.IsApproved.HasValue)
        {
            query = query.Where(u => u.IsApproved == criteria.IsApproved.Value);
        }

        // Declined status
        if (criteria.IsDeclined.HasValue)
        {
            query = query.Where(u => u.IsDeclined == criteria.IsDeclined.Value);
        }

        // Superseded status
        if (criteria.IsSuperseded.HasValue)
        {
            query = query.Where(u => u.IsSuperseded == criteria.IsSuperseded.Value);
        }

        // Date range
        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(u => u.CreationDate >= criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            query = query.Where(u => u.CreationDate <= criteria.CreatedBefore.Value);
        }

        // Product filter
        if (!string.IsNullOrWhiteSpace(criteria.Product))
        {
            query = query.Where(u =>
                u.ProductTitles?.Any(p => p.Contains(criteria.Product, StringComparison.OrdinalIgnoreCase)) ?? false);
        }

        // Hide superseded
        if (criteria.HideSuperseded)
        {
            query = query.Where(u => !u.IsSuperseded);
        }

        return query;
    }

    /// <inheritdoc/>
    public IEnumerable<ComputerStatus> FilterComputers(IEnumerable<ComputerStatus> computers, ComputerFilterCriteria criteria)
    {
        var query = computers.AsEnumerable();

        // Text search
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var searchLower = criteria.SearchText.ToLowerInvariant();
            query = query.Where(c =>
                (c.Name?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.IpAddress?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Group filter
        if (criteria.GroupId.HasValue)
        {
            query = query.Where(c => c.GroupIds?.Contains(criteria.GroupId.Value) ?? false);
        }

        // Has needed updates
        if (criteria.HasNeededUpdates)
        {
            query = query.Where(c => c.NeededCount > 0);
        }

        // Has failed updates
        if (criteria.HasFailedUpdates)
        {
            query = query.Where(c => c.FailedCount > 0);
        }

        // Reported after
        if (criteria.ReportedAfter.HasValue)
        {
            query = query.Where(c => c.LastReportedTime >= criteria.ReportedAfter.Value);
        }

        // Stale computers
        if (criteria.ShowStaleOnly)
        {
            var staleThreshold = DateTime.UtcNow.AddDays(-criteria.StaleDays);
            query = query.Where(c => c.LastReportedTime < staleThreshold || c.LastReportedTime == null);
        }

        return query;
    }

    /// <inheritdoc/>
    public IEnumerable<T> Sort<T>(IEnumerable<T> items, SortCriteria sortCriteria)
    {
        var property = typeof(T).GetProperty(
            sortCriteria.PropertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null)
        {
            return items;
        }

        return sortCriteria.Direction == SortDirection.Ascending
            ? items.OrderBy(x => property.GetValue(x))
            : items.OrderByDescending(x => property.GetValue(x));
    }

    /// <inheritdoc/>
    public IEnumerable<WsusUpdate> FuzzySearch(IEnumerable<WsusUpdate> updates, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return updates;
        }

        var searchTerms = searchText.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return updates
            .Select(u => new
            {
                Update = u,
                Score = CalculateRelevanceScore(u, searchTerms)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Update);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetDistinctClassifications(IEnumerable<WsusUpdate> updates)
    {
        return updates
            .Where(u => !string.IsNullOrWhiteSpace(u.Classification))
            .Select(u => u.Classification!)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetDistinctProducts(IEnumerable<WsusUpdate> updates)
    {
        return updates
            .Where(u => u.ProductTitles != null)
            .SelectMany(u => u.ProductTitles!)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .OrderBy(p => p);
    }

    private static int CalculateRelevanceScore(WsusUpdate update, string[] searchTerms)
    {
        var score = 0;

        foreach (var term in searchTerms)
        {
            // KB article exact match (highest priority)
            if (update.KbArticle?.Equals(term, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                score += 100;
            }
            else if (update.KbArticle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                score += 50;
            }

            // Title match
            if (update.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                score += 30;
                // Boost for title starting with term
                if (update.Title.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                }
            }

            // Description match
            if (update.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                score += 10;
            }

            // Product match
            if (update.ProductTitles?.Any(p => p.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false)
            {
                score += 15;
            }

            // Classification match
            if (update.Classification?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                score += 20;
            }
        }

        return score;
    }
}
