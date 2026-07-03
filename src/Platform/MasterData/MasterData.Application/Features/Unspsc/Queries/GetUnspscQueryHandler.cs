using MediatR;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public class GetUnspscQueryHandler
    : IRequestHandler<GetUnspscQuery, List<SegmentDto>>
{
    private readonly IRepositoryWrapper _repository;

    public GetUnspscQueryHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

     public async Task<List<SegmentDto>> Handle(
        GetUnspscQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.Unspsc.GetAsync(
            request.PageIndex,
            request.PageSize);
    }
}