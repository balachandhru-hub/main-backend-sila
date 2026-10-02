using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
public sealed record ResolvedExtractionProvider(IOcrProvider Provider, ExtractionAgentConfig? Configuration);

public interface IExtractionProviderResolver
{
    Task<ResolvedExtractionProvider> ResolveAsync(Guid? organizationId, DocumentType documentType, CancellationToken cancellationToken);
}

public sealed class BuiltInOcrProvider(ILogger<BuiltInOcrProvider>? logger = null) : IOcrProvider
{
    public string Name => "BUILT_IN_OCR";

    public async Task<(string? Text, decimal? Confidence)> ExtractAsync(
        Document document,
        Stream content,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var ascii = Encoding.Latin1.GetString(bytes);
        var readable = Regex.Replace(ascii, @"[^\u0009\u000A\u000D\u0020-\u007E]", " ");
        readable = Regex.Replace(readable, @"[ \t]+", " ");
        readable = Regex.Replace(readable, @"\r?\n[ \t]*", "\n").Trim();
        if (readable.Contains("Invoice", StringComparison.OrdinalIgnoreCase) &&
            (readable.Contains("Supplier", StringComparison.OrdinalIgnoreCase) || readable.Contains("TRN", StringComparison.OrdinalIgnoreCase)))
        {
            logger?.LogInformation("[BASIC-OCR] TEXT_EXTRACTION_METHOD=PDF_TEXT");
            return (readable, 0.82m);
        }

        logger?.LogInformation("[BASIC-OCR] TEXT_EXTRACTION_METHOD=BUILT_IN_OCR_TESSERACT");
            var ocrText = await ExtractWithTesseractAsync(document, bytes, cancellationToken, logger);
        return string.IsNullOrWhiteSpace(ocrText) ? (null, null) : (ocrText, 0.70m);
    }

    private static async Task<string?> ExtractWithTesseractAsync(
        Document document,
        byte[] bytes,
        CancellationToken cancellationToken,
        ILogger<BuiltInOcrProvider>? logger)
    {
        var extension = Path.GetExtension(document.OriginalFilename).ToLowerInvariant();
        var directory = Path.Combine(Path.GetTempPath(), "sila-me-basic-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, $"source{(extension is ".pdf" or ".png" or ".jpg" or ".jpeg" ? extension : ".bin")}");
            await File.WriteAllBytesAsync(source, bytes, cancellationToken);
            var images = new List<string>();
            if (extension == ".pdf" || document.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pythonText = await ExtractWithPythonAsync(source, cancellationToken, logger, document.OcrRequestId);
                if (!string.IsNullOrWhiteSpace(pythonText)) return pythonText;
                if (!HasCommand("pdftoppm") || !HasCommand("tesseract"))
                {
                    logger?.LogWarning("[OCR] Request={RequestId} Event=OCR_TOOLS_MISSING pdftoppm={Pdftoppm} tesseract={Tesseract}",
                        document.OcrRequestId, HasCommand("pdftoppm") ? "AVAILABLE" : "MISSING", HasCommand("tesseract") ? "AVAILABLE" : "MISSING");
                    return null;
                }
                var prefix = Path.Combine(directory, "page");
                var rendered = await RunProcessAsync("pdftoppm", ["-jpeg", "-r", "300", source, prefix], cancellationToken);
                logger?.LogInformation("[OCR] DocumentId={DocumentId} Event=PDF_RENDERED Dpi=300 ExitCode={ExitCode}", document.Id, rendered.ExitCode);
                if (rendered.ExitCode != 0) return null;
                images.AddRange(Directory.GetFiles(directory, "page*.jpg").OrderBy(item => item, StringComparer.OrdinalIgnoreCase));
            }
            else if (!HasCommand("tesseract"))
            {
                logger?.LogWarning("[OCR] Request={RequestId} Event=OCR_TOOLS_MISSING tesseract=MISSING", document.OcrRequestId);
                return null;
            }
            else
            {
                images.Add(source);
            }

            logger?.LogInformation("[ADV-OCR] Request={RequestId} Event=OCR_PAGES_DISCOVERED PageCount={PageCount}", document.OcrRequestId, images.Count);
            if (images.Count == 0) return null;
            var pages = new List<string>();
            for (var index = 0; index < images.Count; index++)
            {
                var image = images[index];
                var recognized = await RunProcessAsync("tesseract", [image, "stdout", "--psm", "6", "-l", "eng"], cancellationToken);
                if (recognized.ExitCode == 0 && !string.IsNullOrWhiteSpace(recognized.Stdout))
                    pages.Add(recognized.Stdout.Trim());
                logger?.LogInformation("[ADV-OCR] Request={RequestId} Event=OCR_PAGE_COMPLETED Page={Page} ExitCode={ExitCode} Characters={Characters}",
                    document.OcrRequestId, index + 1, recognized.ExitCode, recognized.Stdout.Length);
            }
            logger?.LogInformation("[ADV-OCR] Request={RequestId} Event=OCR_QUALITY PagesWithText={PagesWithText} Characters={Characters}",
                document.OcrRequestId, pages.Count, pages.Sum(item => item.Length));
            return pages.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, pages);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { /* best-effort temporary cleanup */ }
        }
    }

    private static async Task<string?> ExtractWithPythonAsync(
        string source,
        CancellationToken cancellationToken,
        ILogger<BuiltInOcrProvider>? logger,
        string? requestId)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "ocr_preprocess.py");
        if (!File.Exists(script)) return null;
        var result = await RunProcessAsync("python3", [script, source], cancellationToken);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Stdout)) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Stdout);
            var root = json.RootElement;
            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
            {
                logger?.LogWarning(
                    "[OCR] Request={RequestId} Engine=PYTHON Failed error={Error}",
                    requestId,
                    root.TryGetProperty("error", out var error) ? error.GetString() : "unknown");
                return null;
            }
            var text = root.TryGetProperty("text", out var textNode) ? textNode.GetString() : null;
            logger?.LogInformation(
                "[OCR] Request={RequestId} Engine=PYTHON PreprocessingProfile={Profile} DocumentType={DocumentType} Characters={Characters}",
                requestId,
                root.TryGetProperty("preprocessingProfile", out var profile) ? profile.GetString() : null,
                root.TryGetProperty("documentType", out var type) ? type.GetString() : null,
                text?.Length ?? 0);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool HasCommand(string name)
    {
        try
        {
            using var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            probe.StartInfo.ArgumentList.Add(name);
            if (!probe.Start()) return false;
            probe.WaitForExit(2000);
            return probe.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string Stdout)> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        try
        {
            if (!process.Start()) return (-1, string.Empty);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (-1, string.Empty);
        }
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            _ = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await stdout);
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }
}

public sealed class ConfiguredExternalOcrProvider(
    ExtractionAgentConfig configuration,
    IHttpClientFactory httpClientFactory) : IOcrProvider
{
    public string Name => configuration.ProviderType;

    public async Task<(string? Text, decimal? Confidence)> ExtractAsync(
        Document document,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.EndpointUrl))
            throw new OperationalException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "The external extraction endpoint is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.EndpointUrl);
        request.Headers.Add("X-SILA-Document-Type", document.DocumentType.ToString());
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(document.ContentType);
        var client = httpClientFactory.CreateClient("external-extraction");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OperationalException("EXTERNAL_PROVIDER_FAILED", $"External extraction returned HTTP {(int)response.StatusCode}.");

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (payload.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            using var json = JsonDocument.Parse(payload);
            var text = json.RootElement.TryGetProperty("text", out var textElement) ? textElement.GetString() : payload;
            var confidence = json.RootElement.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetDecimal(out var parsed)
                ? parsed
                : 0.80m;
            return (text, confidence);
        }
        return (payload, 0.80m);
    }
}

public sealed class ExtractionProviderResolver(
    SilaMeDbContext db,
    BuiltInOcrProvider builtInOcrProvider,
    IHttpClientFactory httpClientFactory) : IExtractionProviderResolver
{
    public async Task<ResolvedExtractionProvider> ResolveAsync(Guid? organizationId, DocumentType documentType, CancellationToken cancellationToken)
    {
        var configuration = await db.ExtractionAgentConfigs.AsNoTracking()
            .Where(item => item.IsActive &&
                item.DocumentType == documentType.ToString() &&
                (item.OrganizationId == null || item.OrganizationId == organizationId))
            .OrderBy(item => item.Priority)
            .ThenByDescending(item => item.OrganizationId != null)
            .FirstOrDefaultAsync(cancellationToken);

        return configuration is null
            ? new ResolvedExtractionProvider(builtInOcrProvider, null)
            : new ResolvedExtractionProvider(new ConfiguredExternalOcrProvider(configuration, httpClientFactory), configuration);
    }
}

public sealed class BasicInvoiceExtractionService(IInvoiceExtractionService invoiceExtractionService)
{
    public BasicInvoiceExtraction Extract(string text)
    {
        var invoice = invoiceExtractionService.Extract(text);
        return new BasicInvoiceExtraction(
            invoice.SupplierName,
            invoice.SupplierTaxNumber,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.PoNumber,
            invoice.GrossAmount,
            invoice.Currency);
    }
}

public sealed record BasicInvoiceExtraction(
    string? SupplierName,
    string? SupplierTrn,
    string? SupplierInvoiceNumber,
    DateOnly? InvoiceDate,
    string? PurchaseOrderNumber,
    decimal? InvoiceGross,
    string? Currency);