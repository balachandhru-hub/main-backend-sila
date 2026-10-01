using Operations.Domain.Entities;

namespace Operations.Application.Services.Ocr
{
    public interface IOcrProvider
    {
        string Name { get; }

        /// <summary>True after the provider could not run at all (OCR binaries missing).</summary>
        bool Unavailable { get; }

        Task<(string? Text, decimal? Confidence)> ExtractAsync(Document document, byte[] content, CancellationToken cancellationToken);
    }
}
