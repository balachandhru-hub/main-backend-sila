using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetGoodsReceiptByNumber
{
    /// <summary>
    /// Returns one goods receipt of the organization by its GRN number.
    /// </summary>
    public class GetGoodsReceiptByNumberQuery : IRequest<GoodsReceiptResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string GrnNumber { get; set; } = string.Empty;
    }
}
