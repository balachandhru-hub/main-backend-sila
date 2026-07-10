using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllBuyers
{
    public class GetAllBuyersQuery : IRequest<List<OrganizationDto>>
    {
        public int Index { get; set; } 

        public int Limit { get; set; } 
    }
}