using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.CheckPredefinedMaterialSimilarity
{
    public class CheckPredefinedMaterialSimilarityQueryHandler
        : IRequestHandler<CheckPredefinedMaterialSimilarityQuery, List<SimilarPredefinedMaterialDto>>
    {
        private const int MaxResults = 10;

        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public CheckPredefinedMaterialSimilarityQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<SimilarPredefinedMaterialDto>> Handle(
            CheckPredefinedMaterialSimilarityQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Description) ||
                string.IsNullOrWhiteSpace(request.MaterialGroup))
            {
                _logger.LogError(
                    "Description and Material Group are required for similarity check.");
                throw new BadRequestCustomException(
                    "Description and Material Group are required.",
                    "Provide both Description and MaterialGroup to check for similar items.");
            }

            _logger.LogInfo(
                $"Checking Item Master similarity. Description: {request.Description}, " +
                $"MaterialGroup: {request.MaterialGroup}, BuyerId: {request.BuyerId}");

            var description = request.Description.Trim().ToLower();
            var materialGroup = request.MaterialGroup.Trim().ToLower();

            var query = _repositoryWrapper.PredefinedMaterial
                .FindByCondition(x => x.IsActive);

            if (request.BuyerId.HasValue)
            {
                query = query.Where(x => x.BuyerId == request.BuyerId.Value);
            }

            // "Contains" both ways, so "chicken" matches "Chicken Breast
            // Boneless" and "Chicken Breast Boneless" also matches "chicken".
            query = query.Where(x =>
                ((x.Description ?? "").ToLower().Contains(description) ||
                    description.Contains((x.Description ?? "").ToLower())) &&
                (x.MaterialGroup.ToLower().Contains(materialGroup) ||
                    materialGroup.Contains(x.MaterialGroup.ToLower())));

            var result = await query
                .OrderByDescending(x => x.DateUpdated)
                .Take(MaxResults)
                .Select(x => new SimilarPredefinedMaterialDto
                {
                    Id = x.Id,
                    MaterialCode = x.MaterialCode,
                    Description = x.Description
                })
                .ToListAsync(cancellationToken);

            _logger.LogInfo(
                $"Item Master similarity check complete. Similar items found: {result.Count}");

            return result;
        }
    }
}
