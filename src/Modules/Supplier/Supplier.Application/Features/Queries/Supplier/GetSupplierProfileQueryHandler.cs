using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierProfileQueryHandler
        : IRequestHandler<GetSupplierProfileQuery, SupplierProfileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierProfileQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SupplierProfileDto> Handle(
            GetSupplierProfileQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier profile for OrganizationId : {request.OrganizationId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier profile does not exist.");
            }

            var registrations = _repository.SupplierRegistration
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var bankAccounts = _repository.SupplierBankAccount
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var dispatchLocations = _repository.SupplierDispatchLocation
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var result = new SupplierProfileDto
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
                    Description = supplier.Description
                },

                Registrations = registrations.Select(x =>
                {
                    AssetDto? assetDto = null;

                    if (x.AssetId.HasValue)
                    {
                        var asset = _repository.Asset.FindFirstByCondition(a =>
                            a.Id == x.AssetId.Value &&
                            a.IsActive);

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

                    return new SupplierRegistrationResponseDto
                    {
                        RegistrationType = x.RegistrationType.ToString(),
                        RegistrationNumber = x.RegistrationNumber,
                        RegistrationName = x.RegistrationName,
                        Asset = assetDto,
                        ExpiryDate = x.ExpiryDate
                    };
                }).ToList(),

                BankAccounts = bankAccounts.Select(x => new SupplierBankAccountDto
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
                }).ToList(),

                DispatchLocations = dispatchLocations.Select(x => new SupplierDispatchLocationDto
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
                }).ToList()
            };

            _logger.LogInfo($"Supplier profile fetched successfully for OrganizationId : {request.OrganizationId}");

            return Task.FromResult(result);
        }
    }
}