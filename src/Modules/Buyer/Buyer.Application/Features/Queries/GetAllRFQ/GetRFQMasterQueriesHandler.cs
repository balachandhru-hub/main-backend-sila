using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQListQueryHandler
        : IRequestHandler<GetRFQListQuery, List<RFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetRFQListQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<RFQListDto>> Handle(
     GetRFQListQuery request,
     CancellationToken cancellationToken)
        {

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