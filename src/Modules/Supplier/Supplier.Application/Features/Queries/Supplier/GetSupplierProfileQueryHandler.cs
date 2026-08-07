using MediatR;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierProfileQueryHandler
        : IRequestHandler<GetSupplierProfileQuery, OrganizationDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetSupplierProfileQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public Task<OrganizationDto> Handle(
            GetSupplierProfileQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier profile for OrganizationId : {request.OrganizationId}");

            var organization = _repositoryWrapper.SupplierBusinessProfile
                .FindFirstByCondition(o => o.IsActive && o.OrganizationId == request.OrganizationId);

            if (organization == null)
            {
                _logger.LogError($"Organization with ID {request.OrganizationId} not found.");
                throw new  NoContentCustomException(
                    "Organization Not Found",
                    $"Organization with ID {request.OrganizationId} not found.");
            }
            var supplier = _repositoryWrapper.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                    throw new NoContentCustomException(
                        "No supplier profile data found.",
                        "Supplier profile exists, but no related data is available.");
                }

            var registrations = _repositoryWrapper.SupplierRegistration
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var bankAccounts = _repositoryWrapper.SupplierBankAccount
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var dispatchLocations = _repositoryWrapper.SupplierDispatchLocation
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();
                var categories = _repositoryWrapper.SupplierCategory
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    x.IsActive)
                .ToList();

            var result = new OrganizationDto
            {
                Id = organization.Id,
                OrganizationId = supplier.OrganizationId,
               SNID=supplier.SNID,

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
                },

                Registrations = registrations.Select(x =>
                {
                    AssetDto? assetDto = null;

                    if (x.AssetId.HasValue)
                    {
                        var asset = _repositoryWrapper.Asset.FindFirstByCondition(a =>
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
                        RegistrationType = x.RegistrationType,
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
                }).ToList(),
                SupplierCategories = categories.Select(x => new SupplierCategoryDto
                {
                    Segment = x.Segment,
                    SegmentTitle = x.SegmentTitle,
                    Family = x.Family,
                    FamilyTitle = x.FamilyTitle,
                    Class = x.Class,
                    ClassTitle = x.ClassTitle,
                    Commodity = x.Commodity,
                    CommodityTitle = x.CommodityTitle
                }).ToList()
            };

            _logger.LogInfo($"Supplier profile fetched successfully for OrganizationId : {request.OrganizationId}");

            return Task.FromResult(result);
        }
    }
}