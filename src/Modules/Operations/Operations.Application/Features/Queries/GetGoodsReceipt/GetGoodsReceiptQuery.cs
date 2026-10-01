using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetGoodsReceipt
{
    /// <summary>
    /// Returns one goods receipt of the organization with its lines.
    /// </summary>
    public class GetGoodsReceiptQuery : IRequest<GoodsReceiptResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid GoodsReceiptId { get; set; }
    }
}
