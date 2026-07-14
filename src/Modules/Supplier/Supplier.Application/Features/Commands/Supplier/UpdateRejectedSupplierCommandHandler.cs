using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Application.Contracts;
using Supplier.Domain.Dto;
using Microsoft.AspNetCore.Http;




namespace Supplier.Application.Features.Commands.Supplier.UpdateRejectedSupplier
{
    public class UpdateRejectedSupplierCommandHandler
        : IRequestHandler<UpdateRejectedSupplierCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IIdentityApiClient _identityApiClient;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMetadataApiClient _metadataApiClient;

        public UpdateRejectedSupplierCommandHandler(
            IRepositoryWrapper repository,
             IIdentityApiClient identityApiClient,
             IMetadataApiClient metadataApiClient,
            IConfiguration configuration,
            IMediator mediator,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _identityApiClient = identityApiClient;
            _configuration = configuration;
            _mediator = mediator;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _metadataApiClient = metadataApiClient;
        }

        public async Task<bool> Handle(
            UpdateRejectedSupplierCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Starting update process for rejected supplier with ID: {request.Supplier.SupplierId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.Id == request.Supplier.SupplierId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError($"Supplier with ID {request.Supplier.SupplierId} not found.");
                throw new NotFoundCustomException(
                    "Supplier not found",
                    "Supplier does not exist.");
            }



            if (!supplier.Status.Equals(Common.REJECTED_STATUS,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Supplier with ID {request.Supplier.SupplierId} is not in REJECTED status. Current status: {supplier.Status}");
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



            // Fetch metadata
            var metadataList = await _metadataApiClient.GetReferenceList(
                new List<string> { Common.METADATA_DOCUMENT_TYPE });

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


            if (organizationChanged)
            {
                _logger.LogInfo($"Organization details have changed for supplier ID {request.Supplier.SupplierId}. Updating Identity service.");
                var token = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("Access token is missing in the request cookies.");
                    throw new UnauthorizedAccessException("Access token is missing.");
                }

                await _identityApiClient.UpdateOrganization(
                    new UpdateOrganizationRequestDto
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
                    },
                    token,
                    cancellationToken);
            }
            //--------------------------------------
            // Update Supplier Business Profile
            //--------------------------------------
            if (profile.OrganizationName != supplier.OrganizationName)
            {
                _logger.LogInfo($"Updating OrganizationName for supplier ID {request.Supplier.SupplierId} from '{supplier.OrganizationName}' to '{profile.OrganizationName}'.");
                supplier.OrganizationName = profile.OrganizationName;
            }
            if (profile.Email != supplier.Email)
            {
                _logger.LogInfo($"Updating Email for supplier ID {request.Supplier.SupplierId} from '{supplier.Email}' to '{profile.Email}'.");
                supplier.Email = profile.Email;
            }
            if (profile.Phone != supplier.Phone)
            {
                _logger.LogInfo($"Updating Phone for supplier ID {request.Supplier.SupplierId} from '{supplier.Phone}' to '{profile.Phone}'.");
                supplier.Phone = profile.Phone;
            }
            if (profile.Country != supplier.Country)
            {
                _logger.LogInfo($"Updating Country for supplier ID {request.Supplier.SupplierId} from '{supplier.Country}' to '{profile.Country}'.");
                supplier.Country = profile.Country;
            }

            if (profile.AddressLine1 != supplier.AddressLine1)
            {
                _logger.LogInfo($"Updating AddressLine1 for supplier ID {request.Supplier.SupplierId} from '{supplier.AddressLine1}' to '{profile.AddressLine1}'.");
                supplier.AddressLine1 = profile.AddressLine1;
            }
            if (profile.AddressLine2 != supplier.AddressLine2)
            {
                _logger.LogInfo($"Updating AddressLine2 for supplier ID {request.Supplier.SupplierId} from '{supplier.AddressLine2}' to '{profile.AddressLine2}'.");
                supplier.AddressLine2 = profile.AddressLine2;
            }
            if (profile.City != supplier.City)
            {
                _logger.LogInfo($"Updating City for supplier ID {request.Supplier.SupplierId} from '{supplier.City}' to '{profile.City}'.");
                supplier.City = profile.City;
            }
            if (profile.State != supplier.State)
            {
                _logger.LogInfo($"Updating State for supplier ID {request.Supplier.SupplierId} from '{supplier.State}' to '{profile.State}'.");
                supplier.State = profile.State;
            }
            if (profile.PinCode != supplier.PinCode)
            {
                _logger.LogInfo($"Updating PinCode for supplier ID {request.Supplier.SupplierId} from '{supplier.PinCode}' to '{profile.PinCode}'.");
                supplier.PinCode = profile.PinCode;
            }

            if (profile.Industry != supplier.Industry)
            {
                _logger.LogInfo($"Updating Industry for supplier ID {request.Supplier.SupplierId} from '{supplier.Industry}' to '{profile.Industry}'.");
                supplier.Industry = profile.Industry;
            }
            if (profile.BusinessType != supplier.BusinessType)
            {
                _logger.LogInfo($"Updating BusinessType for supplier ID {request.Supplier.SupplierId} from '{supplier.BusinessType}' to '{profile.BusinessType}'.");
                supplier.BusinessType = profile.BusinessType;
            }
            if (profile.EmployeeCount != supplier.EmployeeCount)
            {
                _logger.LogInfo($"Updating EmployeeCount for supplier ID {request.Supplier.SupplierId} from '{supplier.EmployeeCount}' to '{profile.EmployeeCount}'.");
                supplier.EmployeeCount = profile.EmployeeCount;
            }
            if (profile.AnnualTurnover != supplier.AnnualTurnover)
            {
                _logger.LogInfo($"Updating AnnualTurnover for supplier ID {request.Supplier.SupplierId} from '{supplier.AnnualTurnover}' to '{profile.AnnualTurnover}'.");
                supplier.AnnualTurnover = profile.AnnualTurnover;
            }
            if (profile.Currency != supplier.Currency)
            {
                _logger.LogInfo($"Updating Currency for supplier ID {request.Supplier.SupplierId} from '{supplier.Currency}' to '{profile.Currency}'.");
                supplier.Currency = profile.Currency;
            }
            if (profile.YearEstablished != supplier.YearEstablished)
            {
                _logger.LogInfo($"Updating YearEstablished for supplier ID {request.Supplier.SupplierId} from '{supplier.YearEstablished}' to '{profile.YearEstablished}'.");
                supplier.YearEstablished = profile.YearEstablished;
            }
            if (profile.Website != supplier.Website)
            {
                _logger.LogInfo($"Updating Website for supplier ID {request.Supplier.SupplierId} from '{supplier.Website}' to '{profile.Website}'.");
                supplier.Website = profile.Website;
            }
            if (profile.Description != supplier.Description)
            {
                _logger.LogInfo($"Updating Description for supplier ID {request.Supplier.SupplierId} from '{supplier.Description}' to '{profile.Description}'.");
                supplier.Description = profile.Description;
            }

            //-------------------------------------------------
            // Registration Update
            //-------------------------------------------------

            foreach (var item in request.Supplier.Registrations)
            {
                _logger.LogInfo($"Updating registration with ID {item.Id} for supplier ID {request.Supplier.SupplierId}.");
                var registration = _repository.SupplierRegistration
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (registration == null)
                {
                    _logger.LogError($"Registration with ID {item.Id} not found for supplier ID {request.Supplier.SupplierId}.");
                    throw new NotFoundCustomException(
                        "Registration not found.",
                        $"Registration with Id '{item.Id}' was not found.");
                }

                if (!metadataLookup.TryGetValue(item.RegistrationType, out Guid metadataId))
                {
                    _logger.LogError($"Registration type '{item.RegistrationType}' not found in metadata for supplier ID {request.Supplier.SupplierId}.");
                    throw new NotFoundCustomException(
                        "Registration type not found.",
                        $"Registration type '{item.RegistrationType}' not found.");
                }

                if (registration.RegistrationType != metadataId)
                {
                    _logger.LogInfo($"Updating RegistrationType for registration ID {item.Id} from '{registration.RegistrationType}' to '{metadataId}'.");
                    registration.RegistrationType = metadataId;
                }

                if (registration.RegistrationNumber != item.RegistrationNumber)
                {
                    _logger.LogInfo($"Updating RegistrationNumber for registration ID {item.Id} from '{registration.RegistrationNumber}' to '{item.RegistrationNumber}'.");
                    registration.RegistrationNumber = item.RegistrationNumber;
                }

                if (registration.RegistrationName != item.RegistrationName)
                {
                    _logger.LogInfo($"Updating RegistrationName for registration ID {item.Id} from '{registration.RegistrationName}' to '{item.RegistrationName}'.");
                    registration.RegistrationName = item.RegistrationName;
                }

                if (registration.ExpiryDate != item.ExpiryDate)
                {
                    _logger.LogInfo($"Updating ExpiryDate for registration ID {item.Id} from '{registration.ExpiryDate}' to '{item.ExpiryDate}'.");
                    registration.ExpiryDate = item.ExpiryDate;
                }

                if (item.Asset != null)
                {
                    _logger.LogInfo($"Uploading new asset for registration ID {item.Id}.");
                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(item.Asset),
                        cancellationToken);

                    registration.AssetId = assetId;
                }

                registration.IsVerified = false;
                registration.VerifiedOn = null;

                _repository.SupplierRegistration.Update(registration);
            }

            //-------------------------------------------------
            // Bank Accounts
            //-------------------------------------------------

            foreach (var item in request.Supplier.BankAccounts)
            {
                _logger.LogInfo($"Updating bank account with ID {item.Id} for supplier ID {request.Supplier.SupplierId}.");
                var bank = _repository.SupplierBankAccount
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (bank == null)
                {
                    _logger.LogError($"Bank account with ID {item.Id} not found for supplier ID {request.Supplier.SupplierId}.");
                    throw new NotFoundCustomException(
                        "Bank account not found.",
                        $"Bank account with Id '{item.Id}' was not found.");
                }
                if (bank.AccountHolderName != item.AccountHolderName)
                {
                    _logger.LogInfo($"Updating AccountHolderName for bank account ID {item.Id} from '{bank.AccountHolderName}' to '{item.AccountHolderName}'.");
                    bank.AccountHolderName = item.AccountHolderName;
                }
                if (bank.BankName != item.BankName)
                {
                    _logger.LogInfo($"Updating BankName for bank account ID {item.Id} from '{bank.BankName}' to '{item.BankName}'.");
                    bank.BankName = item.BankName;
                }
                if (bank.BranchName != item.BranchName)
                {
                    _logger.LogInfo($"Updating BranchName for bank account ID {item.Id} from '{bank.BranchName}' to '{item.BranchName}'.");
                    bank.BranchName = item.BranchName;
                }

                if (bank.AccountNumber != item.AccountNumber)
                {
                    _logger.LogInfo($"Updating AccountNumber for bank account ID {item.Id} from '{bank.AccountNumber}' to '{item.AccountNumber}'.");
                    bank.AccountNumber = item.AccountNumber;
                }
                if (bank.IFSCCode != item.IFSCCode)
                {
                    _logger.LogInfo($"Updating IFSCCode for bank account ID {item.Id} from '{bank.IFSCCode}' to '{item.IFSCCode}'.");
                    bank.IFSCCode = item.IFSCCode;
                }
                if (bank.SWIFTCode != item.SWIFTCode)
                {
                    _logger.LogInfo($"Updating SWIFTCode for bank account ID {item.Id} from '{bank.SWIFTCode}' to '{item.SWIFTCode}'.");
                    bank.SWIFTCode = item.SWIFTCode;
                }
                if (bank.IBAN != item.IBAN)
                {
                    _logger.LogInfo($"Updating IBAN for bank account ID {item.Id} from '{bank.IBAN}' to '{item.IBAN}'.");
                    bank.IBAN = item.IBAN;
                }
                if (bank.Currency != item.Currency)
                {
                    _logger.LogInfo($"Updating Currency for bank account ID {item.Id} from '{bank.Currency}' to '{item.Currency}'.");
                    bank.Currency = item.Currency;
                }
                if (bank.IsPrimary != item.IsPrimary)
                {
                    _logger.LogInfo($"Updating IsPrimary for bank account ID {item.Id} from '{bank.IsPrimary}' to '{item.IsPrimary}'.");
                    bank.IsPrimary = item.IsPrimary;
                }


                _repository.SupplierBankAccount.Update(bank);
            }

            //-------------------------------------------------
            // Dispatch Locations
            //-------------------------------------------------

            foreach (var item in request.Supplier.DispatchLocations)
            {
                _logger.LogInfo($"Updating dispatch location with ID {item.Id} for supplier ID {request.Supplier.SupplierId}.");
                var location = _repository.SupplierDispatchLocation
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (location == null)
                {
                    _logger.LogError($"Dispatch location with ID {item.Id} not found for supplier ID {request.Supplier.SupplierId}.");
                    throw new NotFoundCustomException(
                        "Dispatch location not found.",
                        $"Dispatch location with Id '{item.Id}' was not found.");
                }
                if (location.LocationName != item.LocationName)
                {
                    _logger.LogInfo($"Updating LocationName for dispatch location ID {item.Id} from '{location.LocationName}' to '{item.LocationName}'.");
                    location.LocationName = item.LocationName;
                }
                if (location.AddressLine1 != item.AddressLine1)
                {
                    _logger.LogInfo($"Updating AddressLine1 for dispatch location ID {item.Id} from '{location.AddressLine1}' to '{item.AddressLine1}'.");
                    location.AddressLine1 = item.AddressLine1;
                }
                if (location.AddressLine2 != item.AddressLine2)
                {
                    _logger.LogInfo($"Updating AddressLine2 for dispatch location ID {item.Id} from '{location.AddressLine2}' to '{item.AddressLine2}'.");
                    location.AddressLine2 = item.AddressLine2;
                }
                if (location.City != item.City)
                {
                    _logger.LogInfo($"Updating City for dispatch location ID {item.Id} from '{location.City}' to '{item.City}'.");
                    location.City = item.City;
                }
                if (location.State != item.State)
                {
                    _logger.LogInfo($"Updating State for dispatch location ID {item.Id} from '{location.State}' to '{item.State}'.");
                    location.State = item.State;
                }
                if (location.Country != item.Country)
                {
                    _logger.LogInfo($"Updating Country for dispatch location ID {item.Id} from '{location.Country}' to '{item.Country}'.");
                    location.Country = item.Country;
                }
                if (location.PinCode != item.PinCode)
                {
                    _logger.LogInfo($"Updating PinCode for dispatch location ID {item.Id} from '{location.PinCode}' to '{item.PinCode}'.");
                    location.PinCode = item.PinCode;
                }
                if (location.ContactPerson != item.ContactPerson)
                {
                    _logger.LogInfo($"Updating ContactPerson for dispatch location ID {item.Id} from '{location.ContactPerson}' to '{item.ContactPerson}'.");
                    location.ContactPerson = item.ContactPerson;
                }
                if (location.ContactEmail != item.ContactEmail)
                {
                    _logger.LogInfo($"Updating ContactEmail for dispatch location ID {item.Id} from '{location.ContactEmail}' to '{item.ContactEmail}'.");
                    location.ContactEmail = item.ContactEmail;
                }
                if (location.ContactPhone != item.ContactPhone)
                {
                    _logger.LogInfo($"Updating ContactPhone for dispatch location ID {item.Id} from '{location.ContactPhone}' to '{item.ContactPhone}'.");
                    location.ContactPhone = item.ContactPhone;
                }
                if (location.IsDefault != item.IsDefault)
                {
                    _logger.LogInfo($"Updating IsDefault for dispatch location ID {item.Id} from '{location.IsDefault}' to '{item.IsDefault}'.");
                    location.IsDefault = item.IsDefault;
                }


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
