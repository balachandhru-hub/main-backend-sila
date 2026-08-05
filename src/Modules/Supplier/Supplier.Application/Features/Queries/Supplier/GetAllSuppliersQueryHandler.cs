using MediatR;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetAllSuppliersQueryHandler
        : IRequestHandler<GetAllSuppliersQuery, List<SupplierProfileDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetAllSuppliersQueryHandler(IRepositoryWrapper repositoryWrapper, ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }


        public async Task<List<SupplierProfileDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching all suppliers with filters - OrganizationName: {request.OrganizationName}, Status: {request.Status}, Index: {request.Index}, Limit: {request.Limit}");
            var query = _repositoryWrapper.SupplierBusinessProfile
                .FindByCondition(x => x.IsActive);


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
                        Status = supplier.Status,
                        Comment = supplier.Comment
                    }
                };



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
                            RegistrationType = registration.RegistrationType,
                            RegistrationNumber = registration.RegistrationNumber,
                            RegistrationName = registration.RegistrationName,
                            ExpiryDate = registration.ExpiryDate,
                            Asset = assetDto
                        });
                }



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