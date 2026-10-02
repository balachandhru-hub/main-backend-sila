using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetLiveStock
{
    /// <summary>
    /// Reads the stock in hand of an organization from its stock API, at the moment it is asked.
    /// </summary>
    public class GetLiveStockQuery : IRequest<LiveStockResponseDto>
    {
        public Guid OrganizationId { get; set; }
    }
}
