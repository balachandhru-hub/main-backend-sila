using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Unspsc.Queries
{
    public class GetUnspscClassQueryHandler
        : IRequestHandler<GetUnspscClassQuery, List<UnspscClassDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetUnspscClassQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<UnspscClassDto>> Handle(
            GetUnspscClassQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.Unspsc.GetClassAsync(
              
                request.Family,
                request.PageIndex,
                request.PageSize);
        }
    }
}