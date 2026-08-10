using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries
{
    public record GetUnspscCommodityQuery(
       
        long Class,
        int PageIndex,
        int PageSize
    ) : IRequest<List<UnspscCommodityDto>>;
}