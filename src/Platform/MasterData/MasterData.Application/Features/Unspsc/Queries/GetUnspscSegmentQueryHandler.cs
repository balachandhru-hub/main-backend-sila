using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Unspsc.Queries;

public class GetUnspscSegmentQueryHandler
    : IRequestHandler<GetUnspscsegmentQuery, List<GetSegmentDto>>
{
    private readonly IRepositoryWrapper _repository;

    public GetUnspscSegmentQueryHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<List<GetSegmentDto>> Handle(
        GetUnspscsegmentQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.Unspsc.GetSegmentAsync(
    request.PageIndex,
    request.PageSize,
    request.SearchTerm);
    }
}