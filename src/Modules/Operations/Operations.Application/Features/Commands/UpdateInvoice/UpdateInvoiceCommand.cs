using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpdateInvoice
{
    /// <summary>
    /// Saves the reviewed header of an invoice. Every saved field counts as manually edited and survives a re-read.
    /// </summary>
    public class UpdateInvoiceCommand : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
        public UpdateInvoiceRequestDto Request { get; set; } = new();
    }
}
