using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.PostGoodsReceipt
{
    /// <summary>
    /// Posts a goods receipt: books it in the ERP (when one is configured), then updates the purchase order and the stock.
    /// </summary>
    public class PostGoodsReceiptCommand : IRequest<GoodsReceiptResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public PostGrnRequestDto Request { get; set; } = new();
        public string? IdempotencyKey { get; set; }
    }
}
