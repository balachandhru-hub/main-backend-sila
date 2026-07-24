using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.GetAllSupplierRFQ
{
    public class GetRFQListQueryHandler
        : IRequestHandler<GetSupplierRFQListQuery, List<SupplierRFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetRFQListQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger=logger;
        }

        public async Task<List<SupplierRFQListDto>> Handle(
     GetSupplierRFQListQuery request,
     CancellationToken cancellationToken)
        {
            _logger.LogInfo("Get All RFQ Masetr Data");

            var rfqs = await _repository.SupplierRFQ
                .FindByCondition(x => x.SupplierId == request.SupplierId)
                .ToListAsync(cancellationToken);
            var buyers = await _repository.SupplierBusinessProfile
                .FindByCondition(x => x.Id == request.SupplierId)
                .ToListAsync(cancellationToken);
            var result = await (
                from rfq in _repository.SupplierRFQ.FindByCondition(x => x.SupplierId == request.SupplierId)
                join buyer in _repository.SupplierBusinessProfile.FindByCondition(x => x.Id == request.SupplierId)
                    on rfq.SupplierId equals buyer.Id
                orderby rfq.DateCreated descending
                select new SupplierRFQListDto
                {
                    RFQNumber = rfq.RFQNumber,
                    Title = rfq.Title,
                    EndDate = rfq.EndDate,
                    OrganizationName = buyer.OrganizationName,
                    DeliveryLocation=rfq.DeliveryLocation,
                    RFQId=rfq.BuyerRFQId
                })
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);



            return result;

        }
    }
}