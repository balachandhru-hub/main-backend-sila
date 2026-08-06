using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;
using Supplier.Domain.Entities;
using System.Net.Http.Json;
using Supplier.Domain.Common;
using Microsoft.Extensions.Configuration;
using Supplier.Application.Features.Commands.Asset;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Supplier
{
    public class CreateSupplierProfileCommandHandler
        : IRequestHandler<CreateSupplierProfileCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        public CreateSupplierProfileCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            HttpClient httpClient,
            IConfiguration configuration,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _mediator = mediator;
        }

        public async Task<Guid> Handle(
    CreateSupplierProfileCommand request,
    CancellationToken cancellationToken)
{
    _logger.LogInfo("Starting supplier profile creation process.");

    var existingProfile = await _repository.SupplierBusinessProfile
        .FindFirstByConditionAsync(x => x.OrganizationId == request.SupplierProfileDto.OrganizationId);

    if (existingProfile != null)
    {
        _logger.LogError($"Supplier profile already exists for organization {request.SupplierProfileDto.OrganizationId}");

        throw new PreConditionFailedCustomException(
            "A supplier profile for this organization already exists.",
            $"A supplier profile already exists for organization {request.SupplierProfileDto.OrganizationId}.");
    }

    _logger.LogInfo("Creating Supplier Business Profile.");

    SupplierBusinessProfile supplierProfile = new SupplierBusinessProfile
    {
        Id = Guid.NewGuid(),
        OrganizationId = request.SupplierProfileDto.OrganizationId,
        SNID = request.SupplierProfileDto.SNID,

        OrganizationName = request.SupplierProfileDto.BusinessProfile.OrganizationName,
        Email = request.SupplierProfileDto.BusinessProfile.Email,
        Phone = request.SupplierProfileDto.BusinessProfile.Phone,

        Country = request.SupplierProfileDto.BusinessProfile.Country,
        AddressLine1 = request.SupplierProfileDto.BusinessProfile.AddressLine1,
        AddressLine2 = request.SupplierProfileDto.BusinessProfile.AddressLine2,
        City = request.SupplierProfileDto.BusinessProfile.City,
        State = request.SupplierProfileDto.BusinessProfile.State,
        PinCode = request.SupplierProfileDto.BusinessProfile.PinCode,

        Industry = request.SupplierProfileDto.BusinessProfile.Industry,
        BusinessType = request.SupplierProfileDto.BusinessProfile.BusinessType,
        EmployeeCount = request.SupplierProfileDto.BusinessProfile.EmployeeCount,
        AnnualTurnover = request.SupplierProfileDto.BusinessProfile.AnnualTurnover,
        Currency = request.SupplierProfileDto.BusinessProfile.Currency,
        YearEstablished = request.SupplierProfileDto.BusinessProfile.YearEstablished,
        Website = request.SupplierProfileDto.BusinessProfile.Website,
        Description = request.SupplierProfileDto.BusinessProfile.Description,
        Status = Common.PENDING_STATUS
    };

    await _repository.SupplierBusinessProfile.CreateAsync(supplierProfile);

    //---------------------------------------------------------
    // Registrations
    //---------------------------------------------------------

    if (request.SupplierProfileDto.Registrations != null && request.SupplierProfileDto.Registrations.Any())
    {
        _logger.LogInfo("Fetching document metadata.");

        string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

        var response = await _httpClient.PostAsJsonAsync(
            $"{masterDataUrl}/api/v1/masterdata/metadata/reference-list",
            new List<string> { Common.METADATA_DOCUMENT_TYPE },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Unable to fetch document metadata.");

            throw new PreConditionFailedCustomException(
                "Unable to fetch document metadata.",
                "Unable to fetch document metadata from MasterData.");
        }

        var metadataList = await response.Content.ReadFromJsonAsync<List<MetadataDto>>(
            cancellationToken: cancellationToken);

        if (metadataList == null || !metadataList.Any())
        {
            _logger.LogError("Document metadata not found.");

            throw new NotFoundCustomException(
                "Document metadata not found.",
                "Document metadata not found.");
        }

        var metadataLookup = metadataList.ToDictionary(
            x => x.Key,
            x => x.Id,
            StringComparer.OrdinalIgnoreCase);

        List<SupplierRegistration> registrations = new();

        foreach (var registration in request.SupplierProfileDto.Registrations)
        {
            if (!metadataLookup.TryGetValue(registration.RegistrationType, out Guid metadataId))
            {
                _logger.LogError($"Document type '{registration.RegistrationType}' not found.");

                throw new NotFoundCustomException(
                    "Registration type not found.",
                    $"Document type '{registration.RegistrationType}' not found.");
            }

            Guid? assetId = null;

            if (registration.Asset != null)
            {
                _logger.LogInfo($"Uploading asset for {registration.RegistrationName}");

                assetId = await _mediator.Send(
                    new UploadAssetCommand(registration.Asset),
                    cancellationToken);
            }

            SupplierRegistration supplierRegistration = new SupplierRegistration
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierProfile.Id,
                RegistrationType = registration.RegistrationType,
                RegistrationNumber = registration.RegistrationNumber,
                RegistrationName = registration.RegistrationName,
                ExpiryDate = registration.ExpiryDate,
                AssetId = assetId,
                IsVerified = false
            };

            registrations.Add(supplierRegistration);
        }

        await _repository.SupplierRegistration.CreateRangeAsync(registrations);
    }

    //---------------------------------------------------------
    // Bank Accounts
    //---------------------------------------------------------

    if (request.SupplierProfileDto.BankAccounts != null && request.SupplierProfileDto.BankAccounts.Any())
    {
        _logger.LogInfo("Creating Supplier Bank Accounts.");

        List<SupplierBankAccount> bankAccounts = new();

        foreach (var account in request.SupplierProfileDto.BankAccounts)
        {
            SupplierBankAccount bankAccount = new SupplierBankAccount
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierProfile.Id,

                AccountHolderName = account.AccountHolderName,
                BankName = account.BankName,
                BranchName = account.BranchName,
                AccountNumber = account.AccountNumber,
                IFSCCode = account.IFSCCode,
                SWIFTCode = account.SWIFTCode,
                IBAN = account.IBAN,
                Currency = account.Currency,
                IsPrimary = account.IsPrimary,
                IsVerified = false
            };

            bankAccounts.Add(bankAccount);
        }

        await _repository.SupplierBankAccount.CreateRangeAsync(bankAccounts);
    }

    //---------------------------------------------------------
    // Dispatch Locations
    //---------------------------------------------------------

    if (request.SupplierProfileDto.DispatchLocations != null && request.SupplierProfileDto.DispatchLocations.Any())
    {
        _logger.LogInfo("Creating Supplier Dispatch Locations.");

        List<SupplierDispatchLocation> dispatchLocations = new();

        foreach (var location in request.SupplierProfileDto.DispatchLocations)
        {
            SupplierDispatchLocation dispatchLocation = new SupplierDispatchLocation
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierProfile.Id,

                LocationName = location.LocationName,
                AddressLine1 = location.AddressLine1,
                AddressLine2 = location.AddressLine2,
                City = location.City,
                State = location.State,
                Country = location.Country,
                PinCode = location.PinCode,

                ContactPerson = location.ContactPerson,
                ContactEmail = location.ContactEmail,
                ContactPhone = location.ContactPhone,
                IsDefault = location.IsDefault
            };

            dispatchLocations.Add(dispatchLocation);
        }

        await _repository.SupplierDispatchLocation.CreateRangeAsync(dispatchLocations);
    }

    //---------------------------------------------------------
    // Categories
    //---------------------------------------------------------

    if (request.SupplierProfileDto.SupplierCategories != null && request.SupplierProfileDto.SupplierCategories.Any())
    {
        _logger.LogInfo("Creating Supplier Categories.");

        List<SupplierCategory> categories = new();

        foreach (var category in request.SupplierProfileDto.SupplierCategories)
        {
            categories.Add(new SupplierCategory
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierProfile.Id,
                Segment = category.Segment,
                SegmentTitle = category.SegmentTitle,
                Family = category.Family,
                FamilyTitle = category.FamilyTitle,
                Class = category.Class,
                ClassTitle = category.ClassTitle,
                Commodity = category.Commodity,
                CommodityTitle = category.CommodityTitle
            });
        }

        await _repository.SupplierCategory.CreateRangeAsync(categories);
    }

    //---------------------------------------------------------
    // Save
    //---------------------------------------------------------

    await _repository.SaveAsync();

    _logger.LogInfo($"Successfully registered Supplier Profile for Organization Id : {request.SupplierProfileDto.OrganizationId}");

    return supplierProfile.Id;
}
    }
}