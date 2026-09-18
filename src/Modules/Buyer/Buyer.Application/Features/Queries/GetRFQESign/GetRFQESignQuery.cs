using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetRFQESign
{
    public class GetRFQESignQuery : IRequest<List<RFQESignDto>>
    {
        public Guid RFQId { get; set; }
    }
}
