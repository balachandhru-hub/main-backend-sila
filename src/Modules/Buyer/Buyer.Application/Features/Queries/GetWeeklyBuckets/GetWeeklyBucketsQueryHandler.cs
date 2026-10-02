using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWeeklyBuckets
{
    public class GetWeeklyBucketsQueryHandler : IRequestHandler<GetWeeklyBucketsQuery, List<WeeklyBucketListItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetWeeklyBucketsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<WeeklyBucketListItemDto>> Handle(GetWeeklyBucketsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching weekly buckets. OrganizationId: {request.OrganizationId}, Index: {request.Index}, Limit: {request.Limit}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            int index = request.Index < 0 ? 0 : request.Index;
            int limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 100);
            List<WeeklyBucket> rows = await _repository.WeeklyBucket
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.WeekNumber)
                .ThenByDescending(x => x.DateCreated)
                .Skip(index)
                .Take(limit)
                .ToListAsync(cancellationToken);

            List<Guid> bucketIds = rows.Select(row => row.Id).ToList();
            List<Guid> propertyIds = rows.Select(row => row.PropertyId).Distinct().ToList();
            List<BuyerProperty> properties = await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && propertyIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            List<Guid> itemBucketIds = await _repository.WeeklyBucketItem
                .FindByCondition(x => bucketIds.Contains(x.WeeklyBucketId) && x.IsActive)
                .Select(x => x.WeeklyBucketId)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Weekly buckets fetched. Count: {rows.Count}, BuyerId: {buyer.Id}");
            return rows.Select(row => new WeeklyBucketListItemDto
            {
                Id = row.Id,
                BucketCode = row.BucketCode,
                WeekNumber = row.WeekNumber,
                Year = row.Year,
                PropertyName = properties.FirstOrDefault(x => x.Id == row.PropertyId)?.PropertyName,
                PlantCode = row.PlantCode,
                Status = row.Status,
                ItemCount = itemBucketIds.Count(id => id == row.Id),
                FrozenOn = row.FrozenOn
            }).ToList();
        }

        private BuyerBusinessProfile GetBuyer(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }
    }
}
