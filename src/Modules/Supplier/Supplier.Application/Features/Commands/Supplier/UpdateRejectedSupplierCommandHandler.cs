using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;
using System.Net.Http.Json;
using Supplier.Domain.Dto;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;


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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateRejectedSupplierCommandHandler(
            IRepositoryWrapper repository,
            HttpClient httpClient,
            IConfiguration configuration,
            IMediator mediator,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            _mediator = mediator;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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



            var profile = request.Supplier.BusinessProfile;

            bool organizationChanged =
                   supplier.OrganizationName != profile.OrganizationName
                || supplier.Email != profile.Email
                || supplier.Phone != profile.Phone
                || supplier.Country != profile.Country
                || supplier.AddressLine1 != profile.AddressLine1
                || supplier.AddressLine2 != profile.AddressLine2
                || supplier.City != profile.City
                || supplier.State != profile.State
                || supplier.PinCode != profile.PinCode;
            //-------------------------------------------------
            // Update Identity Organization if required
            //-------------------------------------------------



            if (organizationChanged)
            {
                var identityUrl = _configuration[Common.IDENTITY_SERVICE_BASE_URL];

                var identityRequest = new
                {
                    organization = new UpdateOrganizationRequestDto
                    {
                        OrganizationId = supplier.OrganizationId,
                        OrganizationName = profile.OrganizationName,
                        Email = profile.Email,
                        Phone = profile.Phone,
                        Country = profile.Country,
                        AddressLine1 = profile.AddressLine1,
                        AddressLine2 = profile.AddressLine2,
                        City = profile.City,
                        State = profile.State,
                        PinCode = profile.PinCode
                    }
                };

                //-------------------------------------------------
                // Forward same JWT token to Identity API
                //-------------------------------------------------

                var token = _httpContextAccessor.HttpContext?
                    .Request.Cookies["access_token"];

                _logger.LogInfo($"Incoming Token : {token}");

                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Put,
                    $"{identityUrl}/api/v1/identity/update-organization");

                requestMessage.Content = JsonContent.Create(identityRequest);

                if (!string.IsNullOrWhiteSpace(token))
                {
                    
                    requestMessage.Headers.Add("Cookie", $"access_token={token}");

               
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                var response = await _httpClient.SendAsync(
                    requestMessage,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInfo($"Identity Response : {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new BadRequestCustomException(
                        "Unable to update organization.",
                        responseBody);
                }
            }
            //--------------------------------------
            // Update Supplier Business Profile
            //--------------------------------------

            supplier.OrganizationName = profile.OrganizationName;
            supplier.Email = profile.Email;
            supplier.Phone = profile.Phone;

            supplier.Country = profile.Country;
            supplier.AddressLine1 = profile.AddressLine1;
            supplier.AddressLine2 = profile.AddressLine2;
            supplier.City = profile.City;
            supplier.State = profile.State;
            supplier.PinCode = profile.PinCode;

            supplier.Industry = profile.Industry;
            supplier.BusinessType = profile.BusinessType;
            supplier.EmployeeCount = profile.EmployeeCount;
            supplier.AnnualTurnover = profile.AnnualTurnover;
            supplier.Currency = profile.Currency;
            supplier.YearEstablished = profile.YearEstablished;
            supplier.Website = profile.Website;
            supplier.Description = profile.Description;

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