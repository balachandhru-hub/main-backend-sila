using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Application.Features.Metadata.Queries
{
    public class GetMetadataByKeysQueryHandler
        : IRequestHandler<GetMetadataByKeysQuery, List<MetadataDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetMetadataByKeysQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<MetadataDto>> Handle(
            GetMetadataByKeysQuery request,
            CancellationToken cancellationToken)
        {
            var metadata = _repository.Metadata
                .FindByCondition(x =>
                    x.Type == request.Type &&
                    request.Keys.Contains(x.Key))
                .ToList();

            return metadata.Select(x => new MetadataDto
            {
                Id = x.Id,
                Key = x.Key,
                Type = x.Type,
                Description = x.Description
            }).ToList();
        }
    }
}