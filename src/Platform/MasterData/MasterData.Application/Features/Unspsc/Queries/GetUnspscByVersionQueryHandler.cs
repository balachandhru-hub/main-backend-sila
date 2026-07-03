using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Unspsc.Queries;

public class GetUnspscByVersionQueryHandler
    : IRequestHandler<GetUnspscByVersionQuery, List<ClassDto>>
{
    private readonly IRepositoryWrapper _repository;

    public GetUnspscByVersionQueryHandler(
        IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<List<ClassDto>> Handle(
        GetUnspscByVersionQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.Unspsc.GetByVersionAsync(
            request.Segment,
            request.Family,
            request.PageIndex,
            request.PageSize);
    }
}