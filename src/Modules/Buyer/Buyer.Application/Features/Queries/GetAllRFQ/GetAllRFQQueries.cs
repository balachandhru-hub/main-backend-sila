using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQByIdQuery : IRequest<GetRFQByIdDto>
    {
        public Guid RFQId { get; set; }
    }
}