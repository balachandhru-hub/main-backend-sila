using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetRFQAward
{
    public class GetRFQAwardQuery : IRequest<RFQAwardResponseDto>
    {
        public Guid RFQId { get; set; }
    }
}
