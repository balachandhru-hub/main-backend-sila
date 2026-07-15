using MediatR;
using SharedKernel.Dto;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetAllSuppliersQueryHandler
        : IRequestHandler<GetAllSuppliersQuery, List<SupplierProfileDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GetAllSuppliersQueryHandler(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }


        public async Task<List<SupplierProfileDto>> Handle(
 GetAllSuppliersQuery request,
 CancellationToken cancellationToken)
        {
            var query = _repositoryWrapper.SupplierBusinessProfile
                .FindByCondition(x => x.IsActive);

            // Filter by Organization Name
            if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                query = query.Where(x =>
                    x.OrganizationName.Contains(request.OrganizationName));
            }

            // Filter by Status
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x =>
                    x.Status == request.Status);
            }

            var suppliers = query
                .OrderByDescending(x => x.DateUpdated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            var result = new List<SupplierProfileDto>();



            foreach (var supplier in suppliers)
            {
                var dto = new SupplierProfileDto
                {
                    Id = supplier.Id,
                    OrganizationId = supplier.OrganizationId,

                    BusinessProfile = new SupplierBusinessProfileDto
                    {
                        OrganizationName = supplier.OrganizationName,
                        Email = supplier.Email,
                        Phone = supplier.Phone,

                        Country = supplier.Country,
                        AddressLine1 = supplier.AddressLine1,
                        AddressLine2 = supplier.AddressLine2,
                        City = supplier.City,
                        State = supplier.State,
                        PinCode = supplier.PinCode,

                        Industry = supplier.Industry,
                        BusinessType = supplier.BusinessType,
                        EmployeeCount = supplier.EmployeeCount,
                        AnnualTurnover = supplier.AnnualTurnover,
                        Currency = supplier.Currency,
                        YearEstablished = supplier.YearEstablished,
                        Website = supplier.Website,
                        Description = supplier.Description,
                        Status=supplier.Status,
                        Comment=supplier.Comment
                    }
                };


                // Registrations
                var registrations = _repositoryWrapper.SupplierRegistration
                    .FindByCondition(x =>
                        x.SupplierId == supplier.Id &&
                        x.IsActive)
                    .ToList();


                foreach (var registration in registrations)
                {
                    AssetDto? assetDto = null;


                    if (registration.AssetId.HasValue)
                    {
                        var asset = _repositoryWrapper.Asset
                            .FindFirstByCondition(x =>
                                x.Id == registration.AssetId.Value);


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


                    dto.Registrations.Add(
                        new SupplierRegistrationResponseDto
                        {
                            RegistrationType = registration.RegistrationType.ToString(),
                            RegistrationNumber = registration.RegistrationNumber,
                            RegistrationName = registration.RegistrationName,
                            ExpiryDate = registration.ExpiryDate,
                            Asset = assetDto
                        });
                }


                // Bank Accounts
                dto.BankAccounts = _repositoryWrapper.SupplierBankAccount
                    .FindByCondition(x =>
                        x.SupplierId == supplier.Id &&
                        x.IsActive)
                    .Select(x => new SupplierBankAccountDto
                    {
                        AccountHolderName = x.AccountHolderName,
                        BankName = x.BankName,
                        BranchName = x.BranchName,
                        AccountNumber = x.AccountNumber,
                        IFSCCode = x.IFSCCode,
                        SWIFTCode = x.SWIFTCode,
                        IBAN = x.IBAN,
                        Currency = x.Currency,
                        IsPrimary = x.IsPrimary
                    })
                    .ToList();


                // Dispatch Locations
                dto.DispatchLocations = _repositoryWrapper.SupplierDispatchLocation
                    .FindByCondition(x =>
                        x.SupplierId == supplier.Id &&
                        x.IsActive)
                    .Select(x => new SupplierDispatchLocationDto
                    {
                        LocationName = x.LocationName,
                        AddressLine1 = x.AddressLine1,
                        AddressLine2 = x.AddressLine2,
                        City = x.City,
                        State = x.State,
                        Country = x.Country,
                        PinCode = x.PinCode,
                        ContactPerson = x.ContactPerson,
                        ContactEmail = x.ContactEmail,
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