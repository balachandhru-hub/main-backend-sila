using MediatR;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Commands.RunDocumentAdvancedExtraction
{
    /// <summary>
    /// Runs the advanced (field by field) extraction of a stored document. Also sent by the invoice processing worker.
    /// </summary>
    public class RunDocumentAdvancedExtractionCommand : IRequest<AdvancedInvoiceExtractionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
        public ExtractionTrigger Trigger { get; set; } = ExtractionTrigger.MANUAL_REREAD;
    }
}
