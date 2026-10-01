using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UploadInvoice
{
    /// <summary>
    /// Saves an uploaded invoice document with its invoice and starts the extraction.
    /// </summary>
    public class UploadInvoiceCommand : IRequest<DocumentResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public UploadInvoiceRequestDto Request { get; set; } = new();
        public string? IdempotencyKey { get; set; }
    }
}
