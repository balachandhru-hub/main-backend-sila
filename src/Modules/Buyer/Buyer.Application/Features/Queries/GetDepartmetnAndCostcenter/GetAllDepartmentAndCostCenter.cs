using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Department
{
   public class GetBuyerDepartmentQuery : IRequest<List<BuyerDepartmentDto>>
{
    public Guid? BuyerId { get; set; }

    public Guid OrganizationId { get; set; }

    public int Index { get; set; }

    public int Limit { get; set; }

    public string? SearchTerm { get; set; }
}

}