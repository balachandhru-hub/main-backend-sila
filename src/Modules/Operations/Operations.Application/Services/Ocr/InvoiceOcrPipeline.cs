using System.Text;
using System.Text.RegularExpressions;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Ocr
{
    public interface IInvoiceOcrPipeline
    {
        /// <summary>
        /// Reads the text of a document: embedded PDF text first, then the external agent of the
        /// organization (when one is configured) with the built-in OCR as fallback. Never throws
        /// when OCR is not installed: it returns no text and <c>OcrUnavailable = true</c>.
        /// </summary>
        Task<OcrTextResult> ReadTextAsync(Document document, byte[] content, ExtractionAgentConfig? externalAgent, CancellationToken cancellationToken);

        ExtractedInvoice Parse(string text);

        AdvancedInvoiceExtractionResponseDto BuildAdvancedResponse(
            Document document,
            string? text,
            ExtractionTrigger trigger,
            InvoiceOcrConfiguration configuration,
            DateTime startedAt,
            bool ocrUnavailable);
    }

    /// <summary>
    /// The OCR / extraction engine. It works on bytes and text only and never touches the database.
    /// </summary>
    public class InvoiceOcrPipeline : IInvoiceOcrPipeline
    {
        private readonly IOcrProvider _builtInProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerManager _logger;

        public InvoiceOcrPipeline(IOcrProvider builtInProvider, IHttpClientFactory httpClientFactory, ILoggerManager logger)
        {
            _builtInProvider = builtInProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<OcrTextResult> ReadTextAsync(Document document, byte[] content, ExtractionAgentConfig? externalAgent, CancellationToken cancellationToken)
        {
            if (document.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                string? embedded = EmbeddedPdfText(content);
                if (!string.IsNullOrWhiteSpace(embedded))
                {
                    return new OcrTextResult
                    {
                        Text = embedded,
                        Confidence = 0.95m,
                        Provider = Common.OCR_PROVIDER_EMBEDDED_TEXT,
                        Method = ExtractionMethod.PDF_TEXT
                    };
                }
            }

            IOcrProvider provider = externalAgent == null ? _builtInProvider : new ExternalOcrProvider(externalAgent, _httpClientFactory);
            OcrTextResult result = new OcrTextResult
            {
                Provider = provider.Name,
                Method = externalAgent == null ? ExtractionMethod.OCR : ExtractionMethod.EXTERNAL_AGENT
            };
            (string? Text, decimal? Confidence) ocr;
            try
            {
                ocr = await provider.ExtractAsync(document, content, cancellationToken);
            }
            catch (Exception exception) when (externalAgent != null && exception is not OperationCanceledException)
            {
                _logger.LogError($"[OCR] External extraction failed, the built-in OCR is used. OcrRequestId: {document.OcrRequestId}, Provider: {provider.Name}, Error: {exception.Message}");
                provider = _builtInProvider;
                result.FallbackUsed = true;
                result.ErrorCategory = "EXTERNAL_PROVIDER_ERROR";
                result.Provider = provider.Name;
                result.Method = ExtractionMethod.OCR;
                ocr = await provider.ExtractAsync(document, content, cancellationToken);
            }

            result.Text = ocr.Text;
            result.Confidence = ocr.Confidence;
            result.OcrUnavailable = string.IsNullOrWhiteSpace(ocr.Text) && ReferenceEquals(provider, _builtInProvider) && _builtInProvider.Unavailable;
            return result;
        }

        public ExtractedInvoice Parse(string text)
        {
            return InvoiceTextParser.Parse(text);
        }

        public AdvancedInvoiceExtractionResponseDto BuildAdvancedResponse(
            Document document,
            string? text,
            ExtractionTrigger trigger,
            InvoiceOcrConfiguration configuration,
            DateTime startedAt,
            bool ocrUnavailable)
        {
            return AdvancedInvoiceResponseBuilder.Build(document, text, trigger, configuration, startedAt, ocrUnavailable);
        }

        // Text that is stored readable inside the PDF. Line boundaries are preserved.
        private static string? EmbeddedPdfText(byte[] content)
        {
            string text = Regex.Replace(Encoding.Latin1.GetString(content), @"[^\u0009\u000A\u000D -~]", " ");
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\r?\n[ \t]*", "\n").Trim();
            return text.Length < 20 || !text.Contains("Invoice", StringComparison.OrdinalIgnoreCase) ? null : text;
        }
    }
}
