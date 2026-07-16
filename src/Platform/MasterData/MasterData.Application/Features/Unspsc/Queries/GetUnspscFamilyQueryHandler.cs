using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Unspsc.Queries;

public class GetUnspscFamilyQueryHandler
    : IRequestHandler<GetUnspscFamilyQuery, List<FamilyDto>>
{
    private readonly IRepositoryWrapper _repository;

    public GetUnspscFamilyQueryHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<List<FamilyDto>> Handle(
        GetUnspscFamilyQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.Unspsc.GetFamilyAsync(
            request.Segment,
            request.PageIndex,
            request.PageSize);
    }
}