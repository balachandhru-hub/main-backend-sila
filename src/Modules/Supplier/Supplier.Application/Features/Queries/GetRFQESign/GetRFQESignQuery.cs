using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetRFQESign
{
    public class GetRFQESignQuery : IRequest<List<RFQESignDto>>
    {
        public Guid RFQId { get; set; }
    }
}
