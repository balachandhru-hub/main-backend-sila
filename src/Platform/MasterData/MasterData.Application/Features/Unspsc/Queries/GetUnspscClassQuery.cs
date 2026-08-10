using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries
{
    public record GetUnspscClassQuery(
      
        long Family,
        int PageIndex,
        int PageSize
    ) : IRequest<List<UnspscClassDto>>;
}