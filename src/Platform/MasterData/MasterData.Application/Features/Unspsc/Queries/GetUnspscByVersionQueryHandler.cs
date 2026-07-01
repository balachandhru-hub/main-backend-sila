using MediatR;
using MasterData.Application.Contracts.IRepository;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public class GetUnspscByVersionQueryHandler
    : IRequestHandler<GetUnspscByVersionQuery, List<UnspscDto>>
{
    private readonly IRepositoryWrapper _repository;

    public GetUnspscByVersionQueryHandler(
        IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<List<UnspscDto>> Handle(
        GetUnspscByVersionQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.Unspsc.GetByVersionAsync(
            request.Version,
            request.PageIndex,
            request.PageSize);
    }
}