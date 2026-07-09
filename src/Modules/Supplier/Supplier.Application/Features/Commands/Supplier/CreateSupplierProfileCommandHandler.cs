using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;
using Supplier.Domain.Entities;
using System.Net.Http.Json;
using Supplier.Domain.Common;
using Microsoft.Extensions.Configuration;
using Supplier.Application.Features.Commands.Asset;

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
            _logger.LogInfo("Creating supplier profile.");

           //Business Profile

            var supplierProfile = new SupplierBusinessProfile
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,

                OrganizationName = request.BusinessProfile.OrganizationName,
                Email = request.BusinessProfile.Email,
                Phone = request.BusinessProfile.Phone,
            

                Country = request.BusinessProfile.Country,
                AddressLine1 = request.BusinessProfile.AddressLine1,
                AddressLine2 = request.BusinessProfile.AddressLine2,
                City = request.BusinessProfile.City,
                State = request.BusinessProfile.State,
                PinCode = request.BusinessProfile.PinCode,

                Industry = request.BusinessProfile.Industry,
                BusinessType = request.BusinessProfile.BusinessType,
                EmployeeCount = request.BusinessProfile.EmployeeCount,
                AnnualTurnover = request.BusinessProfile.AnnualTurnover,
                Currency = request.BusinessProfile.Currency,
                YearEstablished = request.BusinessProfile.YearEstablished,
                Website = request.BusinessProfile.Website,
                Description = request.BusinessProfile.Description
            };

            await _repository.SupplierBusinessProfile.CreateAsync(supplierProfile);

           

           

         // Registrations

if (request.Registrations != null && request.Registrations.Any())
{
    string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

   
    var response = await _httpClient.PostAsJsonAsync(
        $"{masterDataUrl}/api/v1/metadata/reference-list",
        new List<string> { Common.METADATA_DOCUMENT_TYPE },
        cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception("Unable to fetch document metadata from MasterData.");
    }

    var metadataList = await response.Content.ReadFromJsonAsync<List<MetadataDto>>(
        cancellationToken: cancellationToken);

    if (metadataList == null || !metadataList.Any())
    {
        throw new Exception("Document metadata not found.");
    }

    
    var metadataLookup = metadataList.ToDictionary(
        x => x.Key,
        x => x.Id,
        StringComparer.OrdinalIgnoreCase);

    var registrations = new List<SupplierRegistration>();

    foreach (var item in request.Registrations)
    {
        _logger.LogInfo($"UI Value: '{item.RegistrationType}'");
        if (!metadataLookup.TryGetValue(item.RegistrationType, out var metadataId))
        {
          _logger.LogError($"Document type '{item.RegistrationType}' not found in metadata.");
            throw new Exception(
                $"Document type '{item.RegistrationType}' not found.");
        }
Guid? assetId = null;

if (item.Asset != null)
{
    assetId = await _mediator.Send(
        new UploadAssetCommand(item.Asset),
        cancellationToken);
}
                registrations.Add(new SupplierRegistration
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierProfile.Id,

            RegistrationType = metadataId,

            RegistrationNumber = item.RegistrationNumber,
            RegistrationName = item.RegistrationName,

            AssetId = assetId,

            ExpiryDate = item.ExpiryDate,
            IsVerified = false
        });
            }

    await _repository.SupplierRegistration.CreateRangeAsync(registrations);
}
            //  Bank Accounts

            if (request.BankAccounts != null && request.BankAccounts.Any())
            {
                var bankAccounts = request.BankAccounts
                    .Select(x => new SupplierBankAccount
                    {
                        Id = Guid.NewGuid(),
                        SupplierId = supplierProfile.Id,

                        AccountHolderName = x.AccountHolderName,
                        BankName = x.BankName,
                        BranchName = x.BranchName,
                        AccountNumber = x.AccountNumber,
                        IFSCCode = x.IFSCCode,
                        SWIFTCode = x.SWIFTCode,
                        IBAN = x.IBAN,
                        Currency = x.Currency,
                        IsPrimary = x.IsPrimary,
                        IsVerified = false
                    })
                    .ToList();

                await _repository.SupplierBankAccount
                    .CreateRangeAsync(bankAccounts);
            }

            //  Dispatch Locations

            if (request.DispatchLocations != null &&
                request.DispatchLocations.Any())
            {
                var dispatchLocations = request.DispatchLocations
                    .Select(x => new SupplierDispatchLocation
                    {
                        Id = Guid.NewGuid(),
                        SupplierId = supplierProfile.Id,

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

                await _repository.SupplierDispatchLocation
                    .CreateRangeAsync(dispatchLocations);
            }

            //  Save Changes

            await _repository.SaveAsync();

            _logger.LogInfo("Supplier profile created successfully.");

            return supplierProfile.Id;
        }
    
   
}
}
