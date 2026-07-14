using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;
using System.Net.Http.Json;

namespace Supplier.Application.Features.Commands.Supplier.UpdateRejectedSupplier
{
    public class UpdateRejectedSupplierCommandHandler
        : IRequestHandler<UpdateRejectedSupplierCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public UpdateRejectedSupplierCommandHandler(
            IRepositoryWrapper repository,
            HttpClient httpClient,
            IConfiguration configuration,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateRejectedSupplierCommand request,
            CancellationToken cancellationToken)
        {
            

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.Id == request.Supplier.SupplierId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found",
                    "Supplier does not exist.");
            }

          

            if (!supplier.Status.Equals(Common.REJECTED_STATUS,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException(
                    "Supplier cannot be edited.",
                    "Only rejected suppliers can edit their profile.");
            }

         

            supplier.Industry = request.Supplier.BusinessProfile.Industry;
            supplier.BusinessType = request.Supplier.BusinessProfile.BusinessType;
            supplier.EmployeeCount = request.Supplier.BusinessProfile.EmployeeCount;
            supplier.AnnualTurnover = request.Supplier.BusinessProfile.AnnualTurnover;
            supplier.Currency = request.Supplier.BusinessProfile.Currency;
            supplier.YearEstablished = request.Supplier.BusinessProfile.YearEstablished;
            supplier.Website = request.Supplier.BusinessProfile.Website;
            supplier.Description = request.Supplier.BusinessProfile.Description;

            //-------------------------------------------------
            // Fetch Metadata once
            //-------------------------------------------------

            string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

            var metadataResponse = await _httpClient.PostAsJsonAsync(
                $"{masterDataUrl}/api/v1/masterdata/metadata/reference-list",
                new List<string> { Common.METADATA_DOCUMENT_TYPE },
                cancellationToken);

            if (!metadataResponse.IsSuccessStatusCode)
            {
                throw new PreConditionFailedCustomException(
                    "Unable to fetch document metadata.",
                    "Unable to fetch document metadata.");
            }

            var metadataList =
                await metadataResponse.Content.ReadFromJsonAsync<List<MetadataDto>>(
                    cancellationToken: cancellationToken);

            if (metadataList == null || !metadataList.Any())
            {
                throw new NotFoundCustomException(
                    "Document metadata not found.",
                    "Document metadata not found.");
            }

            var metadataLookup = metadataList.ToDictionary(
                x => x.Key,
                x => x.Id,
                StringComparer.OrdinalIgnoreCase);

            //-------------------------------------------------
            // Registration Update
            //-------------------------------------------------

            foreach (var item in request.Supplier.Registrations)
            {
                var registration = _repository.SupplierRegistration
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (registration == null)
                    continue;

                if (!metadataLookup.TryGetValue(item.RegistrationType, out Guid metadataId))
                {
                    throw new NotFoundCustomException(
                        "Registration type not found.",
                        $"Registration type '{item.RegistrationType}' not found.");
                }

                Guid? assetId = registration.AssetId;

                if (item.Asset != null)
                {
                    _logger.LogInfo($"Uploading asset for {item.RegistrationName}");

                    assetId = await _mediator.Send(
                        new UploadAssetCommand(item.Asset),
                        cancellationToken);
                }

                registration.RegistrationType = metadataId;
                registration.RegistrationNumber = item.RegistrationNumber;
                registration.RegistrationName = item.RegistrationName;
                registration.ExpiryDate = item.ExpiryDate;
                registration.AssetId = assetId;

                registration.IsVerified = false;
                registration.VerifiedOn = null;

                _repository.SupplierRegistration.Update(registration);
            }

            //-------------------------------------------------
            // Bank Accounts
            //-------------------------------------------------

            foreach (var item in request.Supplier.BankAccounts)
            {
                var bank = _repository.SupplierBankAccount
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (bank == null)
                    continue;

                bank.AccountHolderName = item.AccountHolderName;
                bank.BankName = item.BankName;
                bank.BranchName = item.BranchName;
                bank.AccountNumber = item.AccountNumber;
                bank.IFSCCode = item.IFSCCode;
                bank.SWIFTCode = item.SWIFTCode;
                bank.IBAN = item.IBAN;
                bank.Currency = item.Currency;
                bank.IsPrimary = item.IsPrimary;

                _repository.SupplierBankAccount.Update(bank);
            }

            //-------------------------------------------------
            // Dispatch Locations
            //-------------------------------------------------

            foreach (var item in request.Supplier.DispatchLocations)
            {
                var location = _repository.SupplierDispatchLocation
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (location == null)
                    continue;

                location.LocationName = item.LocationName;
                location.AddressLine1 = item.AddressLine1;
                location.AddressLine2 = item.AddressLine2;
                location.City = item.City;
                location.State = item.State;
                location.Country = item.Country;
                location.PinCode = item.PinCode;
                location.ContactPerson = item.ContactPerson;
                location.ContactEmail = item.ContactEmail;
                location.ContactPhone = item.ContactPhone;
                location.IsDefault = item.IsDefault;

                _repository.SupplierDispatchLocation.Update(location);
            }

            //-------------------------------------------------
            // Move to ReVerification
            //-------------------------------------------------

            supplier.Status = Common.REVERIFICATION_STATUS;
            supplier.Comment = null;

            _repository.SupplierBusinessProfile.Update(supplier);

            await _repository.SaveAsync();

            _logger.LogInfo($"Supplier {supplier.Id} updated successfully and moved to ReVerification.");

            return true;
        }
    }
}