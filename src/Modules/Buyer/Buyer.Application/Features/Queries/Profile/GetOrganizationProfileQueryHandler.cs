using MediatR;
using Buyer.Domain.Dto;
using Buyer.Application.Features.Queries.GetOrganizationProfile;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;

public class GetOrganizationProfileQueryHandler
    : IRequestHandler<GetOrganizationProfileQuery, OrganizationDto>
{
    private readonly IRepositoryWrapper _repositoryWrapper;

    public GetOrganizationProfileQueryHandler(IRepositoryWrapper repositoryWrapper)
    {
        _repositoryWrapper = repositoryWrapper;
    }

    public async Task<OrganizationDto> Handle(
        GetOrganizationProfileQuery request,
        CancellationToken cancellationToken)
    {
        var organization = _repositoryWrapper.BuyerBusinessProfile
            .FindFirstByCondition(o => o.IsActive && o.OrganizationId == request.OrganizationId);

        if (organization == null)
        {
            throw new NotFoundCustomException(
                "Organization Not Found",
                $"Organization with ID {request.OrganizationId} not found.");
        }

        var dto = new OrganizationDto
        {
            OrganizationId = organization.OrganizationId,
            OrganizationName = organization.OrganizationName,
        
            Email = organization.Email,
            Phone = organization.Phone,
            Country = organization.Country,
            AddressLine1 = organization.AddressLine1,
            AddressLine2 = organization.AddressLine2,
            City = organization.City,
            State = organization.State,
            PinCode = organization.PinCode,
            Industry = organization.Industry,
            BusinessType = organization.BusinessType,
            EmployeeCount = organization.EmployeeCount,
            AnnualTurnover = organization.AnnualTurnover,
            Currency = organization.Currency,
            YearEstablished = organization.YearEstablished,
            Website = organization.Website,
            Description = organization.Description

        };

        return dto;
    }
}