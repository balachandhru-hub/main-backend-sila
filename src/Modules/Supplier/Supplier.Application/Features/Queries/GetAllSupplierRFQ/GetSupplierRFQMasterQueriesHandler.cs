using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace Supplier.Application.Features.Queries.GetAllSupplierRFQ
{
    public class GetRFQListQueryHandler
        : IRequestHandler<GetSupplierRFQListQuery, List<SupplierRFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetRFQListQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger=logger;
        }

        public async Task<List<SupplierRFQListDto>> Handle(
     GetSupplierRFQListQuery request,
     CancellationToken cancellationToken)
        {
            _logger.LogInfo("Get All RFQ Master Data");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier does not exist.");
            }

            var query = _repository.SupplierRFQ
                .FindByCondition(x => x.SupplierId == supplier.Id);

            if (!request.RoleId.Equals(Common.SUPPLIER_ADMIN_ROLE_ID))
            {
                var invitedSupplierRFQIds = _repository.RFQOrganizationUserMapping
                    .FindByCondition(x =>
                        x.SupplierId == supplier.Id &&
                        x.UserId == request.UserId &&
                        x.IsActive)
                    .Select(x => x.SupplierRFQId);

                query = query.Where(x => invitedSupplierRFQIds.Contains(x.Id));
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
                    query = query.Where(x => x.StartDate <= now && x.EndDate >= now);
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
                .Select(rfq => new SupplierRFQListDto
                {
                    RFQNumber = rfq.RFQNumber,
                    Title = rfq.Title,
                    EndDate = rfq.EndDate,
                    OrganizationName = supplier.OrganizationName,
                    DeliveryLocation = rfq.DeliveryLocation,
                    RFQId = rfq.BuyerRFQId,
                    SupplierRFQId = rfq.Id
              
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}
