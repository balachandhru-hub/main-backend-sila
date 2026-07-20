using Buyer.Application.Features.Queries.GetAllBuyers;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.GetAllBuyers
{
    public class GetAllBuyersQueryHandler : IRequestHandler<GetAllBuyersQuery, List<OrganizationDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GetAllBuyersQueryHandler(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<List<OrganizationDto>> Handle(
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

            var result = new List<OrganizationDto>();

            foreach (var organization in buyers)
            {
                var dto = new OrganizationDto
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
                        Status = organization.Status,
                        Comments = organization.Comment
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

                    dto.Registrations.Add(new RegistrationDto
                    {
                        RegistrationType = registration.RegistrationType,
                        RegistrationNumber = registration.RegistrationNumber,
                        RegistrationName = registration.RegistrationName,
                        ExpiryDate = registration.ExpiryDate,
                        Asset = assetDto
                    });
                }

                // Bank Accounts
                dto.BankAccounts = _repositoryWrapper.BuyerBankAccount
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
                dto.DispatchLocations = _repositoryWrapper.BuyerDeliveryLocation
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

                result.Add(dto);
            }

            return await Task.FromResult(result);
        }
    }
}