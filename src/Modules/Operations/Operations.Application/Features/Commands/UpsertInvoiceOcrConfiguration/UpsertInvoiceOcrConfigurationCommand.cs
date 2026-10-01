using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpsertInvoiceOcrConfiguration
{
    /// <summary>
    /// Saves the invoice OCR policy of the organization.
    /// </summary>
    public class UpsertInvoiceOcrConfigurationCommand : IRequest<InvoiceOcrConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public UpsertInvoiceOcrConfigurationRequestDto Request { get; set; } = new();
    }
}
