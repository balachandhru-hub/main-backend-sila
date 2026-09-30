using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.GetAllContractTemplates
{
    public class GetAllContractTemplatesQueryHandler
        : IRequestHandler<GetAllContractTemplatesQuery, List<ContractTemplateResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetAllContractTemplatesQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<ContractTemplateResponseDto>> Handle(
            GetAllContractTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            var templates = await _repository.ContractTemplate
                .FindByCondition(x => x.IsActive && x.BuyerId == request.BuyerId)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var assetIds = templates.Select(x => x.AssetId).ToList();

            var assetFileNamesById = await _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileName, cancellationToken);

            return templates.Select(x => new ContractTemplateResponseDto
            {
                Id = x.Id,
                SegmentId = x.SegmentId,
                SegmentTitle = x.SegmentTitle,
                TemplateName = x.TemplateName,
                BuyerId = x.BuyerId,
                AssetId = x.AssetId,
                FileName = assetFileNamesById.TryGetValue(x.AssetId, out var fileName) ? fileName : null,
                DateCreated = x.DateCreated
            }).ToList();
        }
    }
}
