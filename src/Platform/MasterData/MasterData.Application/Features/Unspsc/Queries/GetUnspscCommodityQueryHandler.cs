using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Unspsc.Queries
{
    public class GetUnspscCommodityQueryHandler
        : IRequestHandler<GetUnspscCommodityQuery, List<UnspscCommodityDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetUnspscCommodityQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<UnspscCommodityDto>> Handle(
            GetUnspscCommodityQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.Unspsc.GetCommodityAsync(
                
                request.Class,
                request.PageIndex,
                request.PageSize);
        }
    }
}