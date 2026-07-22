using MediatR;
using Buyer.Domain.Dto;
using Buyer.Application.Features.Queries.GetOrganizationProfile;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.Dto;

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
            throw new  NoContentCustomException(
                "Organization Not Found",
                $"Organization with ID {request.OrganizationId} not found.");
        }

        var response = new OrganizationDto
        {
            Id = organization.Id,
            OrganizationId = organization.OrganizationId,
            BusinessProfile = new BusinessProfileDto
            {
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
                Description = organization.Description,
                Status=organization.Status,
                Comments=organization.Comment
            }
        };

        // Registrations
        var registrations = _repositoryWrapper.BuyerRegistration
            .FindByCondition(x => x.BuyerId == organization.Id && x.IsActive)
            .ToList();

        foreach (var registration in registrations)
        {
            AssetDto? assetDto = null;

            if (registration.AssetId.HasValue)
            {
                var asset = _repositoryWrapper.Asset
                    .FindFirstByCondition(x => x.Id == registration.AssetId.Value);

                if (asset != null)
                {
                    assetDto = new AssetDto
                    {
                        Id = asset.Id,
                        AssetType = asset.AssetType?.ToString(),
                        AssetName = asset.AssetName,
                        FileType = asset.FileType.ToString(),
                        FileName = asset.FileName
                    };
                }
            }

            response.Registrations.Add(new RegistrationDto
            {
                RegistrationType = registration.RegistrationType,
                RegistrationNumber = registration.RegistrationNumber,
                RegistrationName = registration.RegistrationName,
                ExpiryDate = registration.ExpiryDate,
                Asset = assetDto
            });
        }

        // Bank Accounts
        response.BankAccounts = _repositoryWrapper.BuyerBankAccount
            .FindByCondition(x => x.BuyerId == organization.Id && x.IsActive)
            .Select(x => new BankAccountDto
            {
                AccountHolderName = x.AccountHolderName,
                BankName = x.BankName,
                BranchName = x.BranchName,
                AccountNumber = x.AccountNumber,
                IFSCCode = x.IFSCCode,
                SWIFTCode = x.SWIFTCode,
                Currency = x.Currency,
                IsPrimary = x.IsPrimary,
                IsVerified = x.IsVerified
            })
            .ToList();

        // Delivery Locations
        response.DispatchLocations = _repositoryWrapper.BuyerDeliveryLocation
            .FindByCondition(x => x.BuyerId == organization.Id && x.IsActive)
            .Select(x => new DeliveryLocationDto
            {
                LocationName = x.LocationName,
                AddressLine1 = x.AddressLine1,
                AddressLine2 = x.AddressLine2,
                City = x.City,
                State = x.State,
                Country = x.Country,
                PinCode = x.PinCode,
                ContactPerson = x.ContactPerson,
                ContactPhone = x.ContactPhone,
                IsDefault = x.IsDefault
            })
            .ToList();
        //category
        response.Categories = _repositoryWrapper.BuyerCategory
            .FindByCondition(x => x.BuyerId == organization.Id && x.IsActive)
            .Select(x => new CategoryDto
            {
            
                Segment = x.Segment,
                SegmentTitle = x.SegmentTitle,
                Family = x.Family,
                FamilyTitle = x.FamilyTitle,
                Class = x.Class,
                ClassTitle = x.ClassTitle,
                Commodity = x.Commodity,
                CommodityTitle = x.CommodityTitle
            })
            .ToList();
                return response;
            }
}