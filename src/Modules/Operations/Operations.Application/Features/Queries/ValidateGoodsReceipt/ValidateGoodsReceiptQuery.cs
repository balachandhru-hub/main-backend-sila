using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.ValidateGoodsReceipt
{
    /// <summary>
    /// Checks a goods receipt before it is posted and returns the reasons it cannot be posted.
    /// </summary>
    public class ValidateGoodsReceiptQuery : IRequest<GrnValidationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public ValidateGrnRequestDto Request { get; set; } = new();
    }
}
