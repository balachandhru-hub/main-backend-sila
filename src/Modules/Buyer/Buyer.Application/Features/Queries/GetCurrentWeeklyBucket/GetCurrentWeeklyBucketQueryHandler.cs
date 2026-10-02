using Buyer.Application.Features.Queries.GetWeeklyBucket;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetCurrentWeeklyBucket
{
    public class GetCurrentWeeklyBucketQueryHandler : IRequestHandler<GetCurrentWeeklyBucketQuery, WeeklyBucketDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;

        public GetCurrentWeeklyBucketQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
            _mediator = mediator;
        }

        public async Task<WeeklyBucketDetailDto> Handle(GetCurrentWeeklyBucketQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching the active weekly bucket. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}, OutletId: {request.OutletId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            BuyerOutlet outlet = await WeeklyBucketRules.ResolveOutletAsync(
                _repository, _logger, buyer.Id, request.UserId, request.OutletId, cancellationToken);
            BuyerProperty property = await WeeklyBucketRules.GetPropertyAsync(
                _repository, _logger, outlet.PropertyId!.Value, buyer.Id, cancellationToken);
            (WeeklyBucket? bucket, int year, int weekNumber) = await WeeklyBucketRules.FindActiveBucketAsync(
                _repository, _configuration, buyer.Id, property.Id, cancellationToken);

            if (bucket != null)
            {
                _logger.LogInfo($"Active weekly bucket found. WeeklyBucketId: {bucket.Id}, BucketCode: {bucket.BucketCode}");
                return await _mediator.Send(
                    new GetWeeklyBucketQuery
                    {
                        OrganizationId = request.OrganizationId,
                        WeeklyBucketId = bucket.Id
                    },
                    cancellationToken);
            }

            // Nobody requested a product for this week yet. The bucket is created by the first request, not by reading.
            string bucketCode = WeeklyBucketRules.BuildBucketCode(weekNumber, property.PlantCode);
            _logger.LogInfo($"Active weekly bucket is not created yet. BucketCode: {bucketCode}, PropertyId: {property.Id}");
            return new WeeklyBucketDetailDto
            {
                Id = null,
                BucketCode = bucketCode,
                WeekNumber = weekNumber,
                Year = year,
                PropertyId = property.Id,
                PropertyName = property.PropertyName,
                PlantCode = property.PlantCode,
                CompanyCode = property.CompanyCode,
                Status = Common.WEEKLY_BUCKET_OPEN,
                IsFrozen = false
            };
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
