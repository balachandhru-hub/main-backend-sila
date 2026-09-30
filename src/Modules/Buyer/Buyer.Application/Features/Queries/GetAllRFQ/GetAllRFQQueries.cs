using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQByIdQuery : IRequest<GetRFQByIdDto>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}