using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.RetryDocumentTransfer
{
    /// <summary>
    /// Queues a failed document transfer again. TransferId is the transfer job id or, for convenience of the list screen, the destination id.
    /// </summary>
    public class RetryDocumentTransferCommand : IRequest<DocumentTransferRetryResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid TransferId { get; set; }
    }
}
