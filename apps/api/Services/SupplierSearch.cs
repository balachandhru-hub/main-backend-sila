using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class SupplierSearch
{
    public static string Normalize(string? value)
    {
        var folded = Regex.Replace((value ?? string.Empty).Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ");
        folded = Regex.Replace(folded, @"\bL\s+L\s+C\b", "LLC");
        return Regex.Replace(folded, @"\s+", " ").Trim();
    }

    public static IQueryable<Supplier> Filter(IQueryable<Supplier> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var normalized = Normalize(query);
        if (normalized.Length == 0)
            return source;

        var like = $"%{EscapeILike(normalized).Replace(" ", "%", StringComparison.Ordinal)}%";
        var labelled = Regex.Match(query ?? string.Empty, @"(?:supplier|vendor)?\s*(?:id|code)\s*[:#=.–—-]?\s*([A-Z0-9-]{4,})", RegexOptions.IgnoreCase);
        var supplierCode = labelled.Success ? labelled.Groups[1].Value.Trim() : string.Empty;
        return source.Where(item =>
            (!string.IsNullOrWhiteSpace(supplierCode) && item.SupplierCode == supplierCode) ||
            EF.Functions.ILike(item.SupplierCode, like) ||
            EF.Functions.ILike(item.Name, like) ||
            EF.Functions.ILike(item.NormalizedName, like) ||
            (item.LegalName != null && EF.Functions.ILike(item.LegalName, like)) ||
            (item.SearchName != null && EF.Functions.ILike(item.SearchName, like)) ||
            (item.TaxNumber != null && EF.Functions.ILike(item.TaxNumber, like)) ||
            (item.Trn != null && EF.Functions.ILike(item.Trn, like)) ||
            item.Aliases.Any(alias =>
                alias.IsConfirmed &&
                (EF.Functions.ILike(alias.Alias, like) || EF.Functions.ILike(alias.NormalizedAlias, like))));
    }

    public static IReadOnlyList<Supplier> Rank(IEnumerable<Supplier> suppliers, string? query, int take = 25)
    {
        var normalized = Normalize(query);
        return suppliers
            .Select(item => (Supplier: item, Score: Score(item, normalized, query)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Supplier.Name)
            .Take(take)
            .Select(item => item.Supplier)
            .ToList();
    }

    public static int Score(Supplier item, string normalizedQuery, string? rawQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return 0;

        var digits = Regex.Replace(rawQuery ?? string.Empty, @"\D", string.Empty);
        if (!string.IsNullOrWhiteSpace(digits) &&
            (item.TaxNumber == digits || item.Trn == digits || item.TaxNumber == rawQuery?.Trim() || item.Trn == rawQuery?.Trim()))
            return 1000;
        var labelledCode = Regex.Match(rawQuery ?? string.Empty, @"(?:supplier\s*)?(?:id|code)\s*[:#=-]?\s*([A-Z0-9-]+)", RegexOptions.IgnoreCase);
        var supplierCodeCandidate = labelledCode.Success ? labelledCode.Groups[1].Value : digits;
        if (!string.IsNullOrWhiteSpace(supplierCodeCandidate) &&
            string.Equals(item.SupplierCode, supplierCodeCandidate, StringComparison.OrdinalIgnoreCase))
            return 950;
        if (string.Equals(item.SupplierCode, rawQuery?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.SupplierCode, normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 950;
        if (item.NormalizedName == normalizedQuery)
            return 900;
        if (item.Aliases.Any(alias => alias.IsConfirmed && alias.NormalizedAlias == normalizedQuery))
            return 850;
        if (Normalize(item.LegalName) == normalizedQuery)
            return 800;
        if (item.NormalizedName.StartsWith(normalizedQuery, StringComparison.Ordinal))
            return 700;
        if (item.NormalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
            return 600;
        var tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 0 && tokens.All(token => item.NormalizedName.Contains(token, StringComparison.Ordinal)))
            return 500;
        return 100;
    }

    private static string EscapeILike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
