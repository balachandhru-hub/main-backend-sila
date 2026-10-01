using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.RetryGoodsReceipt
{
    /// <summary>
    /// Posts a failed goods receipt to the ERP again and books it when the ERP accepts it.
    /// </summary>
    public class RetryGoodsReceiptCommand : IRequest<GoodsReceiptResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid GoodsReceiptId { get; set; }
    }
}
