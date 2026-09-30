using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQListQuery : IRequest<List<RFQListDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
}
