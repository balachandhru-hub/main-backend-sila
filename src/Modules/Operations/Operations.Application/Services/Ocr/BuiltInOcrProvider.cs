using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Operations.Domain.Common;
using Operations.Domain.Entities;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Ocr
{
    /// <summary>
    /// Built-in OCR: readable text inside the file first, then <c>pdftoppm</c> + <c>tesseract</c>.
    /// Fails soft: when the binaries are missing nothing is thrown, the call returns no text and
    /// <see cref="Unavailable"/> turns true so the caller can ask for manual entry.
    /// Binary names can be overridden with <c>Ocr:PdfToPpmPath</c> / <c>Ocr:TesseractPath</c>.
    /// </summary>
    public class BuiltInOcrProvider : IOcrProvider
    {
        private readonly ILoggerManager _logger;
        private readonly string _pdfRenderer;
        private readonly string _tesseract;
        private readonly string _language;
        private volatile bool _toolsMissing;

        public BuiltInOcrProvider(IConfiguration configuration, ILoggerManager logger)
        {
            _logger = logger;
            _pdfRenderer = Setting(configuration, Common.OCR_PDF_RENDERER, "pdftoppm");
            _tesseract = Setting(configuration, Common.OCR_TESSERACT, "tesseract");
            _language = Setting(configuration, Common.OCR_LANGUAGE, "eng");
        }

        public string Name => Common.OCR_PROVIDER_BUILT_IN;

        public bool Unavailable => _toolsMissing;

        public async Task<(string? Text, decimal? Confidence)> ExtractAsync(Document document, byte[] content, CancellationToken cancellationToken)
        {
            // Line boundaries are kept: collapsing all whitespace makes a labelled field swallow the next label.
            string readable = Regex.Replace(Encoding.Latin1.GetString(content), @"[^\u0009\u000A\u000D -~]", " ");
            readable = Regex.Replace(readable, @"[ \t]+", " ");
            readable = Regex.Replace(readable, @"\r?\n[ \t]*", "\n").Trim();
            if (readable.Contains("Invoice", StringComparison.OrdinalIgnoreCase)
                && (readable.Contains("Supplier", StringComparison.OrdinalIgnoreCase) || readable.Contains("TRN", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInfo($"[OCR] Readable text found in the file. OcrRequestId: {document.OcrRequestId}");
                return (readable, 0.82m);
            }

            string? ocrText = await ExtractWithTesseractAsync(document, content, cancellationToken);
            return string.IsNullOrWhiteSpace(ocrText) ? (null, null) : (ocrText, 0.70m);
        }

        private async Task<string?> ExtractWithTesseractAsync(Document document, byte[] content, CancellationToken cancellationToken)
        {
            string extension = Path.GetExtension(document.OriginalFilename).ToLowerInvariant();
            string directory = Path.Combine(Path.GetTempPath(), "operations-ocr", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                string source = Path.Combine(directory, $"source{(extension is ".pdf" or ".png" or ".jpg" or ".jpeg" ? extension : ".bin")}");
                await File.WriteAllBytesAsync(source, content, cancellationToken);
                List<string> images = new List<string>();
                if (extension == ".pdf" || document.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    string prefix = Path.Combine(directory, "page");
                    (int ExitCode, string Stdout) rendered = await RunProcessAsync(_pdfRenderer, new[] { "-jpeg", "-r", "200", source, prefix }, cancellationToken);
                    if (rendered.ExitCode != 0)
                    {
                        _logger.LogError($"[OCR] PDF rendering failed. OcrRequestId: {document.OcrRequestId}, ExitCode: {rendered.ExitCode}");
                        return null;
                    }

                    images.AddRange(Directory.GetFiles(directory, "page-*.jpg").OrderBy(item => item, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    images.Add(source);
                }

                List<string> pages = new List<string>();
                foreach (string image in images)
                {
                    (int ExitCode, string Stdout) recognized = await RunProcessAsync(_tesseract, new[] { image, "stdout", "--psm", "6", "-l", _language }, cancellationToken);
                    if (recognized.ExitCode == 0 && !string.IsNullOrWhiteSpace(recognized.Stdout))
                    {
                        pages.Add(recognized.Stdout.Trim());
                    }
                }

                _toolsMissing = false;
                _logger.LogInfo($"[OCR] Tesseract finished. OcrRequestId: {document.OcrRequestId}, Pages: {images.Count}, PagesWithText: {pages.Count}");
                return pages.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, pages);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The executable could not be started: pdftoppm / tesseract is not installed.
                _toolsMissing = true;
                _logger.LogError($"[OCR] OCR tools are not installed. OcrRequestId: {document.OcrRequestId}, Renderer: {_pdfRenderer}, Ocr: {_tesseract}");
                return null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogError($"[OCR] OCR failed. OcrRequestId: {document.OcrRequestId}, Error: {exception.Message}");
                return null;
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch (Exception)
                {
                    // Best-effort cleanup of the temporary OCR folder.
                }
            }
        }

        private static async Task<(int ExitCode, string Stdout)> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            using Process process = new Process
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
            foreach (string argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return (-1, string.Empty);
            }

            try
            {
                Task<string> stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
                _ = process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                return (process.ExitCode, await stdout);
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw;
            }
        }

        private static string Setting(IConfiguration configuration, string key, string fallback)
        {
            string? value = configuration[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
