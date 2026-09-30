using Buyer.Application.Features.Queries.GetAllBuyers;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.GetAllBuyers
{
    public class GetAllBuyersQueryHandler : IRequestHandler<GetAllBuyersQuery, List<GetAllBuyerDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GetAllBuyersQueryHandler(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<List<GetAllBuyerDto>> Handle(
            GetAllBuyersQuery request,
            CancellationToken cancellationToken)
        {
            var query = _repositoryWrapper.BuyerBusinessProfile.FindByCondition(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                query = query.Where(x =>
                    x.OrganizationName.Contains(request.OrganizationName));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x =>
                    x.Status == request.Status);
            }

            var buyers = query
                .OrderByDescending(x => x.DateUpdated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            var result = new List<GetAllBuyerDto>();

            foreach (var organization in buyers)
            {
                var dto = new GetAllBuyerDto
                {

                    OrganizationId = organization.OrganizationId,
                    SNID = organization.SNID,
                    OrganizationName = organization.OrganizationName,
                    Email = organization.Email,
                    Phone = organization.Phone,
                    Country = organization.Country,
                    City = organization.City,
                    State = organization.State,
                    Industry = organization.Industry,
                    BusinessType = organization.BusinessType,
                    YearEstablished = organization.YearEstablished,
                    Website = organization.Website,
                    Description = organization.Description,
                    BuyerId = organization.Id
                };




                result.Add(dto);
            }

            return await Task.FromResult(result);
        }
    }
}