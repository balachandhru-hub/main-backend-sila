using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetGoodsReceipts
{
    /// <summary>
    /// Lists the latest goods receipts of the organization.
    /// </summary>
    public class GetGoodsReceiptsQuery : IRequest<List<GoodsReceiptResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
