using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetInvoiceOcrConfiguration
{
    /// <summary>
    /// Returns the invoice OCR policy of the organization (the built-in defaults until it is saved).
    /// </summary>
    public class GetInvoiceOcrConfigurationQuery : IRequest<InvoiceOcrConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
