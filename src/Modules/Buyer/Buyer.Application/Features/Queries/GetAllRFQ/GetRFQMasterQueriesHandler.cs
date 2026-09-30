using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQListQueryHandler
        : IRequestHandler<GetRFQListQuery, List<RFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetRFQListQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<RFQListDto>> Handle(
     GetRFQListQuery request,
     CancellationToken cancellationToken)
        {
            _logger.LogInfo("Get All RFQ Master Data");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (buyer == null)
            {
                _logger.LogError("Buyer not found.");
                throw new NotFoundCustomException("Buyer not found.", "Buyer does not exist.");
            }

            var query = _repository.RFQ
                .FindByCondition(x => x.BuyerId == buyer.Id);

            if (request.RoleId != Common.BUYER_ADMIN_ROLE_ID)
            {
                query = query.Where(x => x.CreatedBy == request.UserId);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x =>
                    x.RFQNumber.Contains(search) ||
                    x.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();

                if (status.Equals(Common.RFQ_LIVE_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(x =>
                        x.StartDate <= now &&
                        x.EndDate >= now &&
                        x.Status != Common.RFQ_FREEZING_STATUS &&
                        x.Status != Common.RFQ_AWARDED_STATUS);
                }
                else
                {
                    query = query.Where(x => x.Status == status);
                }
            }

            var result = await query
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .Select(rfq => new RFQListDto
                {
                    RFQNumber = rfq.RFQNumber,
                    Title = rfq.Title,
                    EndDate = rfq.EndDate,
                    DeliveryLocation = rfq.DeliveryLocation,
                    OrganizationName = buyer.OrganizationName,
                    RFQId = rfq.Id,
                    Description = rfq.Description,
                    Id = rfq.Id
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}
