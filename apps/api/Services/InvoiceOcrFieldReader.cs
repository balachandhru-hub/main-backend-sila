using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SilaMe.Api.Services;

/// <summary>
/// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
/// Candidate/scoring OCR field reader. Do not replace with a single giant regex.
/// </summary>
public static class InvoiceOcrFieldReader
{
    private static readonly string[] CompanyHints =
    [
        "llc", "l.l.c", "l l c", "trading", "company", "corp", "corporation",
        "industries", "enterprise", "fze", "fzc", "fz-llc", "establishment",
        "ltd", "limited", "gmbh", "sarl", "pvt", "private",
    ];

    private static readonly Regex BuyerLine = new(
        @"^(bill\s+to|ship\s+to|sold\s+to|customer|buyer|delivery\s+to|deliver\s+to)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RejectSupplier = new(
        @"\b(bill\s+to|ship\s+to|customer|buyer|deliver\s+to|tax\s+invoice|purchase\s+order|p\.?o\.?\s+(?:number|no\.?|date)|invoice\s+(?:date|number|no\.?|#)|credit\s+note|debit\s+note|grand\s+total|gross\s+amount|supplier\s+(?:trn|id|code)|vat\s+trn|trn|vat|company\s+code|currency|not\s+provided)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string NormalizeOcrText(string text)
    {
        var value = (text ?? string.Empty).Normalize(NormalizationForm.FormKC).Replace('\u00a0', ' ');
        value = Regex.Replace(value, @"[ \t]+", " ");
        value = Regex.Replace(value, @"[ \t]+\r?\n", "\n");
        value = Regex.Replace(value, @"\bP[O0]\b", "PO", RegexOptions.IgnoreCase);
        value = Regex.Replace(
            value,
            @"(?im)^(?<label>supplier(?:\s+name)?|vendor(?:\s+name)?|invoice\s+number|tax\s+invoice\s+number|invoice\s+no|inv\s*no|invoice\s+date|tax\s+invoice\s+date|document\s+date|trn|vat\s+trn|tax\s+(?:registration\s+)?number|gross\s+amount|grand\s+total|invoice\s+total|po\s*(?:number|no\.?|#)?|purchase\s+order(?:\s*(?:number|no\.?))?)\s*[.,;]+(?=\s|$)",
            "${label}:");
        return value.Trim();
    }

    public static string? ReadSupplierName(string text)
    {
        var lines = SplitLines(text);
        var labelled = ReadNameAfterSupplierLabel(lines)
            ?? FirstValueAfter(lines, [
            @"supplier\s+name",
            @"vendor\s+name",
            @"issued\s+by",
            @"sold\s+by",
            @"billed\s+from",
            @"from",
            @"supplier",
            @"vendor",
        ], value =>
        {
            var cleaned = StripTrailingFieldNoise(value);
            return !IsSupplierNameNoise(cleaned) && !RejectSupplier.IsMatch(cleaned) && !BuyerLine.IsMatch(cleaned) && !IsSupplierIdLabel(cleaned) && LooksLikeName(cleaned);
        });
        if (!string.IsNullOrWhiteSpace(labelled))
        {
            labelled = StripTrailingFieldNoise(labelled);
            return InvoiceOcrFieldReader.IsSupplierIdLabel(labelled) ? ReadSupplierId(labelled) : CleanName(labelled);
        }

        for (var index = 0; index < Math.Min(lines.Length, 12); index++)
        {
            var line = lines[index];
            if (BuyerLine.IsMatch(line) || RejectSupplier.IsMatch(line) || IsSupplierIdLabel(line) || Regex.IsMatch(line, @"\d{8,}"))
                continue;
            var followsSupplierLabel = Enumerable.Range(Math.Max(0, index - 3), Math.Min(3, index))
                .Any(previous => Regex.IsMatch(lines[previous], @"^(supplier|vendor)(?:\s+name)?\s*[:#=.\-–—,;]*$", RegexOptions.IgnoreCase));
            if (followsSupplierLabel && LooksLikeName(line))
                return CleanName(line);
            if (CompanyHints.Any(hint => line.Contains(hint, StringComparison.OrdinalIgnoreCase)) && LooksLikeName(line))
                return CleanName(line);
        }

        return ReadSupplierId(text);
    }

    public static string? ReadSupplierId(string text)
    {
        var match = Regex.Match(
            text ?? string.Empty,
            @"(?im)\b(?:supplier|vendor)\s*(?:id|code)\s*[:#=.–—-]?\s*([A-Z0-9-]{4,})\b");
        if (match.Success) return match.Groups[1].Value.Trim();
        match = Regex.Match(text ?? string.Empty, @"(?im)^(?:id|code)\s*[:#=.–—-]?\s*([A-Z0-9-]{4,})\s*$");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static string? NormalizeSupplierCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Replace('\u00a0', ' ').Trim();
        var labelled = Regex.Match(
            trimmed,
            @"(?:supplier|vendor)?\s*(?:id|code)\s*[:#=.–—-]+\s*([A-Z0-9-]{4,})",
            RegexOptions.IgnoreCase);
        if (!labelled.Success)
            labelled = Regex.Match(trimmed, @"^(?:id|code)\s+([A-Z0-9-]{4,})$", RegexOptions.IgnoreCase);
        if (labelled.Success)
            return labelled.Groups[1].Value.Trim();
        return IsSupplierIdLabel(trimmed) ? ReadSupplierId(trimmed) ?? trimmed : trimmed;
    }

    public static bool IsSupplierIdLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, @"^(?:supplier\s*)?(?:id|code)\s*[:#=-]?\s*[A-Z0-9-]{4,}$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(trimmed, @"^(?:supplier\s*)?(?:id|code)$", RegexOptions.IgnoreCase);
    }

    public static DateOnly? ReadInvoiceDate(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [
            @"tax\s+invoice\s+date",
            @"invoice\s+date",
            @"document\s+date",
            @"inv(?:oice)?\s+dt",
            @"dated",
        ], value => ParseDate(value) is not null);
        var parsed = ParseDate(labelled);
        if (parsed is not null)
            return parsed;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"\bdue\s+date\b", RegexOptions.IgnoreCase))
                continue;
            if (!Regex.IsMatch(line, @"\b(invoice\s+date|tax\s+invoice\s+date|document\s+date|dated)\b", RegexOptions.IgnoreCase)
                && !Regex.IsMatch(line, @"(?i)^date\s*[:.#]"))
                continue;
            parsed = ParseDate(line);
            if (parsed is not null)
                return parsed;
        }

        return null;
    }

    public static string? ReadPurchaseOrderNumber(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [
            @"purchase\s+order\s*(?:number|no\.?|#)?",
            @"customer\s+p\.?o\.?",
            @"your\s+p\.?o\.?",
            @"buyer\s+p\.?o\.?",
            @"order\s+ref(?:erence)?",
            @"p\.?o\.?\s*(?:number|no\.?|#)",
            @"po\s*(?:number|no\.?|#)",
        ], value => LooksLikePo(value));
        if (!string.IsNullOrWhiteSpace(labelled))
            return CleanPo(labelled);

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"\b(?:PO|P\.O\.)[\s#:.\-]*([A-Z0-9][A-Z0-9./\-]{3,24})\b", RegexOptions.IgnoreCase);
            if (match.Success && LooksLikePo(match.Groups[1].Value))
                return CleanPo(match.Groups[1].Value);
        }

        return null;
    }

    public static string? ReadSupplierTrn(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [
            @"tax\s+registration\s+number",
            @"vat\s+registration\s+number",
            @"vat\s+trn",
            @"vat\s+reg(?:istration)?",
            @"tax\s+number",
            @"tax\s+no\.?",
            @"trn",
        ], value => LooksLikeTrn(value));
        if (!string.IsNullOrWhiteSpace(labelled))
            return CleanTrn(labelled);

        for (var index = 0; index < lines.Length; index++)
        {
            if (!Regex.IsMatch(lines[index], @"\b(trn|vat\s+reg|tax\s+reg|tax\s+number|tax\s+no)\b", RegexOptions.IgnoreCase))
                continue;
            var nearby = $"{lines[index]} {(index + 1 < lines.Length ? lines[index + 1] : string.Empty)}";
            var digits = CleanTrn(nearby);
            if (LooksLikeTrn(digits))
                return digits;
        }

        return null;
    }

    public static string? ReadInvoiceNumber(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [
            @"supplier\s+invoice\s*(?:number|no\.?|#)",
            @"tax\s+invoice\s*(?:number|no\.?|#)",
            @"invoice\s*(?:number|no\.?|ref(?:erence)?|#)",
            @"invoice\s*#",
            @"inv(?:oice)?\s*(?:number|no\.?|#)",
            @"document\s*(?:number|no\.?|#)",
            @"invoice\s+id",
        ], value => LooksLikeInvoiceNumber(value));
        if (!string.IsNullOrWhiteSpace(labelled))
            return CleanInvoiceNumber(labelled);

        foreach (var line in lines)
        {
            var match = Regex.Match(
                line,
                @"(?i)\b(?:supplier\s+invoice|tax\s+invoice|invoice|inv)\s*(?:number|no\.?|#)\s*[:#=.\-–—]?\s*([A-Z0-9][A-Z0-9._/\-]{2,47})\b");
            if (match.Success && LooksLikeInvoiceNumber(match.Groups[1].Value))
                return CleanInvoiceNumber(match.Groups[1].Value);
        }

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"\b((?:INV|TAX)[\s.\-]*[A-Z0-9][A-Z0-9._/\-]{2,40})\b", RegexOptions.IgnoreCase);
            if (match.Success && LooksLikeInvoiceNumber(match.Groups[1].Value))
                return CleanInvoiceNumber(match.Groups[1].Value);
        }

        return ReadInvoiceNumberFromHeading(lines);
    }

    public static string? ReadCurrency(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [@"currency"], value =>
            Regex.IsMatch(StripTrailingFieldNoise(value), @"^(AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD|INR)\b", RegexOptions.IgnoreCase));
        if (!string.IsNullOrWhiteSpace(labelled))
        {
            var match = Regex.Match(labelled, @"\b(AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD|INR)\b", RegexOptions.IgnoreCase);
            if (match.Success) return match.Value.ToUpperInvariant();
        }

        var anywhere = Regex.Match(text ?? string.Empty, @"\b(AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD|INR)\b", RegexOptions.IgnoreCase);
        return anywhere.Success ? anywhere.Value.ToUpperInvariant() : null;
    }

    public static decimal? ReadGrossAmount(string text)
    {
        var lines = SplitLines(text);
        var labelled = FirstValueAfter(lines, [
            @"grand\s+total",
            @"gross\s+(?:amount|total)",
            @"invoice\s+(?:total|amount)",
            @"total\s+incl(?:uding)?\s+vat",
            @"total\s+including\s+vat",
            @"total\s+amount",
            @"total\s+payable",
            @"net\s+payable",
            @"amount\s+due",
            @"total\s+due",
            @"total",
        ], value => ParseAmount(value) is not null && !IsWeakAmountLine(value));
        var parsed = ParseAmount(labelled);
        if (parsed is not null)
            return parsed;

        decimal? best = null;
        foreach (var line in lines)
        {
            if (IsWeakAmountLine(line) || !IsStrongAmountLine(line))
                continue;
            var amount = ParseAmount(line);
            if (amount is null)
                continue;
            best = amount;
        }

        return best;
    }

    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var raw = value.Trim().TrimEnd(',', '.', ';');
        var iso = Regex.Match(raw, @"\b(20\d{2})[./-](\d{1,2})[./-](\d{1,2})\b");
        if (iso.Success && TryDate(iso.Groups[1].Value, iso.Groups[2].Value, iso.Groups[3].Value, yearFirst: true, out var date))
            return date;

        var numeric = Regex.Match(raw, @"\b(\d{1,2})[./-](\d{1,2})[./-](\d{2,4})\b");
        if (numeric.Success && TryDate(numeric.Groups[3].Value, numeric.Groups[2].Value, numeric.Groups[1].Value, yearFirst: false, out date))
            return date;

        var named = Regex.Match(
            raw,
            @"\b(\d{1,2})[\s\-]+(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)[,\s\-]+(\d{4})\b",
            RegexOptions.IgnoreCase);
        if (named.Success)
        {
            var months = new[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
            var month = Array.FindIndex(months, item => named.Groups[2].Value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) + 1;
            if (month > 0 && TryDate(named.Groups[3].Value, month.ToString(CultureInfo.InvariantCulture), named.Groups[1].Value, yearFirst: true, out date))
                return date;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant))
            return invariant;
        return DateOnly.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out var local) ? local : null;
    }

    private static string[] SplitLines(string text) =>
        NormalizeOcrText(text)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? FirstValueAfter(string[] lines, string[] labels, Func<string, bool> accept)
    {
        foreach (var label in labels)
        {
            var pattern = $@"^(?i)(?<label>{label})\b(?:\s*[:#=.\-–—,;]+\s*|\s+)?(?<value>.*)$";
            for (var index = 0; index < lines.Length; index++)
            {
                var match = Regex.Match(lines[index], pattern);
                if (!match.Success)
                    continue;
                var same = CleanCaptured(match.Groups["value"].Value);
                var next = index + 1 < lines.Length ? CleanCaptured(lines[index + 1]) : string.Empty;
                foreach (var candidate in new[] { same, next })
                {
                    if (string.IsNullOrWhiteSpace(candidate) || !accept(candidate))
                        continue;
                    if (Regex.IsMatch(candidate, @"^(name|address|email|phone|date|number|no\.?|#|trn|vat|supplier(?:\s+name)?|invoice(?:\s+(?:date|number|no\.?))?|tax\s+invoice|gross\s+amount|grand\s+total)$", RegexOptions.IgnoreCase))
                        continue;
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? ReadInvoiceNumberFromHeading(string[] lines)
    {
        for (var index = 0; index < Math.Min(lines.Length, 16); index++)
        {
            var line = lines[index];
            if (Regex.IsMatch(line, @"^(?:tax\s+)?invoice\s*$", RegexOptions.IgnoreCase) && index + 1 < lines.Length)
            {
                var next = lines[index + 1];
                if (LooksLikeHeadingInvoiceNumber(next))
                    return CleanInvoiceNumber(next);
            }
            if (LooksLikeHeadingInvoiceNumber(line)
                && !Regex.IsMatch(line, @"\b(invoice\s+date|po\s+number|purchase\s+order|supplier|bill\s+to|currency|quantity|description)\b", RegexOptions.IgnoreCase))
                return CleanInvoiceNumber(line);
        }

        for (var index = 0; index < lines.Length; index++)
        {
            if (!Regex.IsMatch(lines[index], @"\binvoice\s+date\b", RegexOptions.IgnoreCase))
                continue;
            for (var back = 1; back <= 5 && index - back >= 0; back++)
            {
                var previous = lines[index - back];
                if (LooksLikeHeadingInvoiceNumber(previous))
                    return CleanInvoiceNumber(previous);
            }
        }

        return null;
    }

    private static bool LooksLikeHeadingInvoiceNumber(string value)
    {
        if (!LooksLikeInvoiceNumber(value))
            return false;
        var cleaned = CleanInvoiceNumber(value);
        if (Regex.IsMatch(cleaned, @"^45\d{8}$"))
            return false;
        return Regex.IsMatch(cleaned, @"[A-Z]", RegexOptions.IgnoreCase) && Regex.IsMatch(cleaned, @"\d");
    }

    private static string StripTrailingFieldNoise(string value)
    {
        var cleaned = Regex.Replace(value ?? string.Empty, @"\s+(?:supplier|vendor)\s+(?:id|code)\s*[:#=.\-–—].*$", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+(?:TRN|VAT|PO\b|Invoice|Date|Total).*$", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static string CleanCaptured(string value)
    {
        var cleaned = Regex.Replace(value ?? string.Empty, @"^[\s:#=\-–—,;.]+", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim().TrimEnd(',', ';', '|');
        cleaned = StripTrailingFieldNoise(cleaned);
        return cleaned.Trim();
    }

    private static string CleanName(string value) =>
        Regex.Replace(value, @"\s{2,}", " ").Trim().TrimEnd(',', '.', '|');

    private static string? ReadNameAfterSupplierLabel(string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            var match = Regex.Match(lines[index], @"^(?i)(?:supplier|vendor)(?:\s+name)?\s*[:#=.\-–—,;]*\s*(.*)$");
            if (!match.Success)
                continue;
            for (var offset = 0; offset <= 4; offset++)
            {
                var candidate = offset == 0
                    ? CleanCaptured(match.Groups[1].Value)
                    : index + offset < lines.Length ? CleanCaptured(lines[index + offset]) : string.Empty;
                if (string.IsNullOrWhiteSpace(candidate) || IsSupplierNameNoise(candidate) || BuyerLine.IsMatch(candidate)
                    || IsSupplierIdLabel(candidate) || RejectSupplier.IsMatch(candidate) || !LooksLikeName(candidate))
                    continue;
                return CleanName(candidate);
            }
        }
        return null;
    }

    public static bool IsSupplierNameNoise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, @"^(?:tax\s+)?invoice(?:\s+(?:date|number|no\.?|#|id))?$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(trimmed, @"^(date|invoice\s+date|invoice\s+number|po\s+(?:number|no\.?|date)|purchase\s+order)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(trimmed, @"^(currency|description|quantity|unit\s+price|amount|subtotal|total|tax|vat|trn|bill\s+to|ship\s+to|name)$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(trimmed, @"^\d{1,2}[./-]\d{1,2}[./-]\d{2,4}$")
            || Regex.IsMatch(trimmed, @"^grand(\s+total)?\b", RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeName(string value)
    {
        if (value.Length < 3 || value.Length > 120)
            return false;
        if (Regex.IsMatch(value, @"^\d+$"))
            return false;
        if (IsSupplierNameNoise(value) || Regex.IsMatch(value, @"^(trn|vat|po|inv|tax|name)$", RegexOptions.IgnoreCase))
            return false;
        return value.Count(char.IsLetter) >= 3;
    }

    private static bool LooksLikePo(string value)
    {
        var cleaned = CleanPo(value);
        if (cleaned.Length < 4 || cleaned.Length > 32)
            return false;
        if (Regex.IsMatch(cleaned, @"^(INV|TAX|TRN|VAT)", RegexOptions.IgnoreCase))
            return false;
        return Regex.IsMatch(cleaned, @"^[A-Z0-9][A-Z0-9./\-]{3,}$", RegexOptions.IgnoreCase);
    }

    private static string CleanPo(string value)
    {
        var cleaned = CleanCaptured(value).Replace(" ", string.Empty).TrimEnd('.', ',');
        cleaned = Regex.Replace(cleaned, @"^(?:NO\.?|#|:)+", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.ToUpperInvariant();
    }

    private static bool LooksLikeTrn(string value)
    {
        var digits = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        return digits.Length == 15;
    }

    private static string CleanTrn(string value)
    {
        var grouped = Regex.Match(value ?? string.Empty, @"\b\d{3}[\s\-]?\d{7,10}[\s\-]?\d{3}\b");
        if (grouped.Success)
        {
            var digits = Regex.Replace(grouped.Value, @"\D", string.Empty);
            if (digits.Length == 15)
                return digits;
        }

        var compact = Regex.Match(value ?? string.Empty, @"\b\d{15}\b");
        return compact.Success ? compact.Value : Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
    }

    private static bool LooksLikeInvoiceNumber(string value)
    {
        var cleaned = CleanInvoiceNumber(value);
        if (cleaned.Length is < 3 or > 48)
            return false;
        if (LooksLikeTrn(cleaned) || ParseDate(cleaned) is not null)
            return false;
        if (Regex.IsMatch(cleaned, @"^(INV|TAX|INVOICE|GROSS|AMOUNT|TOTAL|AED|USD|VAT|TRN|PO|GRN)$", RegexOptions.IgnoreCase))
            return false;
        if (Regex.IsMatch(cleaned, @"^(PO|GRN)[\-./]", RegexOptions.IgnoreCase))
            return false;
        return Regex.IsMatch(cleaned, @"[0-9]") && Regex.IsMatch(cleaned, @"^[A-Z0-9][A-Z0-9._/\-]{2,}$", RegexOptions.IgnoreCase);
    }

    private static string CleanInvoiceNumber(string value)
    {
        var cleaned = CleanCaptured(value).Replace(" ", string.Empty).TrimEnd(',', ';', '.');
        cleaned = Regex.Replace(cleaned, @"^(?:NO\.?|#|:)+", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(INV|TAX|INVOICE)[.\s]+", "$1-", RegexOptions.IgnoreCase);
        return cleaned.ToUpperInvariant();
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        decimal? last = null;
        foreach (Match match in Regex.Matches(value, @"\d{1,3}(?:,\d{3})+(?:\.\d{1,2})?|\d+\.\d{2}|\d+"))
        {
            if (decimal.TryParse(match.Value.Replace(",", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                last = amount;
        }

        return last;
    }

    private static bool IsWeakAmountLine(string value) =>
        Regex.IsMatch(value, @"\b(subtotal|unit\s+price|qty|quantity|discount|vat\s+amount|tax\s+amount|net\s+amount|net\s+total)\b", RegexOptions.IgnoreCase);

    private static bool IsStrongAmountLine(string value) =>
        Regex.IsMatch(value, @"\b(grand\s+total|gross\s+amount|gross\s+total|invoice\s+total|invoice\s+amount|total\s+incl(?:uding)?\s+vat|total\s+amount|amount\s+due|total\s+payable|net\s+payable|total\s+due)\b", RegexOptions.IgnoreCase);

    private static bool TryDate(string yearText, string monthText, string dayText, bool yearFirst, out DateOnly date)
    {
        date = default;
        if (!int.TryParse(yearText, out var year) || !int.TryParse(monthText, out var month) || !int.TryParse(dayText, out var day))
            return false;
        if (year < 100)
            year += 2000;
        if (year is < 2000 or > 2099)
            return false;
        if (!yearFirst && month > 12 && day <= 12)
            (month, day) = (day, month);
        if (month is < 1 or > 12 || day is < 1 or > 31)
            return false;
        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
