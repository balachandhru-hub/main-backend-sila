using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQListQueryHandler
        : IRequestHandler<GetRFQListQuery, List<RFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetRFQListQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger=logger;
        }

        public async Task<List<RFQListDto>> Handle(
     GetRFQListQuery request,
     CancellationToken cancellationToken)
        {
            _logger.LogInfo("Get All RFQ Masetr Data");

            var rfqs = await _repository.RFQ
                .FindByCondition(x => x.BuyerId == request.BuyerId)
                .ToListAsync(cancellationToken);
            var buyers = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.Id == request.BuyerId)
                .ToListAsync(cancellationToken);
            var result = await (
                from rfq in _repository.RFQ.FindByCondition(x => x.BuyerId == request.BuyerId)
                join buyer in _repository.BuyerBusinessProfile.FindByCondition(x => x.Id == request.BuyerId)
                    on rfq.BuyerId equals buyer.Id
                orderby rfq.DateCreated descending
                select new RFQListDto
                {
                    RFQNumber = rfq.RFQNumber,
                    Title = rfq.Title,
                    EndDate = rfq.EndDate,
                    DeliveryLocation = rfq.DeliveryLocation,
                    OrganizationName = buyer.OrganizationName
                })
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);



            return result;

        }
    }
}