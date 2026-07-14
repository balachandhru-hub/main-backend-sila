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

           

            // Fetch metadata
            var metadataList = await _metadataApiClient.GetReferenceList(
                new List<string> { Common.METADATA_DOCUMENT_TYPE });

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

          
            if (organizationChanged)
            {
                var token = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

                if (string.IsNullOrWhiteSpace(token))
                {
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
                supplier.OrganizationName = profile.OrganizationName;
            }
            if (profile.Email != supplier.Email)
            {
                supplier.Email = profile.Email;
            }
            if (profile.Phone != supplier.Phone)
            {
                supplier.Phone = profile.Phone;
            }
            if (profile.Country != supplier.Country)
            {
                supplier.Country = profile.Country;
            }

            if (profile.AddressLine1 != supplier.AddressLine1)
            {
                supplier.AddressLine1 = profile.AddressLine1;
            }
            if (profile.AddressLine2 != supplier.AddressLine2)
            {
                supplier.AddressLine2 = profile.AddressLine2;
            }
            if (profile.City != supplier.City)
            {
                supplier.City = profile.City;
            }
            if (profile.State != supplier.State)
            {
                supplier.State = profile.State;
            }
            if (profile.PinCode != supplier.PinCode)
            {
                supplier.PinCode = profile.PinCode;
            }

            if (profile.Industry != supplier.Industry)
            {
                supplier.Industry = profile.Industry;
            }
            if (profile.BusinessType != supplier.BusinessType)
            {
                supplier.BusinessType = profile.BusinessType;
            }
            if (profile.EmployeeCount != supplier.EmployeeCount)
            {
                supplier.EmployeeCount = profile.EmployeeCount;
            }
            if (profile.AnnualTurnover != supplier.AnnualTurnover)
            {
                supplier.AnnualTurnover = profile.AnnualTurnover;
            }
            if (profile.Currency != supplier.Currency)
            {
                supplier.Currency = profile.Currency;
            }
            if (profile.YearEstablished != supplier.YearEstablished)
            {
                supplier.YearEstablished = profile.YearEstablished;
            }
            if (profile.Website != supplier.Website)
            {
                supplier.Website = profile.Website;
            }
            if (profile.Description != supplier.Description)
            {
                supplier.Description = profile.Description;
            }






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
                   {
    throw new NotFoundCustomException(
        "Registration not found.",
        $"Registration with Id '{item.Id}' was not found.");
}

                if (!metadataLookup.TryGetValue(item.RegistrationType, out Guid metadataId))
                {
                    throw new NotFoundCustomException(
                        "Registration type not found.",
                        $"Registration type '{item.RegistrationType}' not found.");
                }

                if (registration.RegistrationType != metadataId)
                {
                    registration.RegistrationType = metadataId;
                }

                if (registration.RegistrationNumber != item.RegistrationNumber)
                {
                    registration.RegistrationNumber = item.RegistrationNumber;
                }

                if (registration.RegistrationName != item.RegistrationName)
                {
                    registration.RegistrationName = item.RegistrationName;
                }

                if (registration.ExpiryDate != item.ExpiryDate)
                {
                    registration.ExpiryDate = item.ExpiryDate;
                }

                if (item.Asset != null)
                {
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
                var bank = _repository.SupplierBankAccount
                    .FindFirstByCondition(x =>
                        x.Id == item.Id &&
                        x.SupplierId == supplier.Id &&
                        x.IsActive);

                if (bank == null)
                    throw new NotFoundCustomException(
                        "Bank account not found.",
                        $"Bank account with Id '{item.Id}' was not found.");

                if (bank.AccountHolderName != item.AccountHolderName)
                {
                    bank.AccountHolderName = item.AccountHolderName;
                }
                if (bank.BankName != item.BankName)
                {
                    bank.BankName = item.BankName;
                }
                if (bank.BranchName != item.BranchName)
                {
                    bank.BranchName = item.BranchName;
                }

                if (bank.AccountNumber != item.AccountNumber)
                {
                    bank.AccountNumber = item.AccountNumber;
                }
                if (bank.IFSCCode != item.IFSCCode)
                {
                    bank.IFSCCode = item.IFSCCode;
                }
                if (bank.SWIFTCode != item.SWIFTCode)
                {
                    bank.SWIFTCode = item.SWIFTCode;
                }
                if (bank.IBAN != item.IBAN)
                {
                    bank.IBAN = item.IBAN;
                }
                if (bank.Currency != item.Currency)
                {
                    bank.Currency = item.Currency;
                }
                if (bank.IsPrimary != item.IsPrimary)
                {
                    bank.IsPrimary = item.IsPrimary;
                }


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
                    throw new NotFoundCustomException(
                        "Dispatch location not found.",
                        $"Dispatch location with Id '{item.Id}' was not found.");

                if (location.LocationName != item.LocationName)
                {
                    location.LocationName = item.LocationName;
                }
                if (location.AddressLine1 != item.AddressLine1)
                {
                    location.AddressLine1 = item.AddressLine1;
                }
                if (location.AddressLine2 != item.AddressLine2)
                {
                    location.AddressLine2 = item.AddressLine2;
                }
                if (location.City != item.City)
                {
                    location.City = item.City;
                }
                if (location.State != item.State)
                {
                    location.State = item.State;
                }
                if (location.Country != item.Country)
                {
                    location.Country = item.Country;
                }
                if (location.PinCode != item.PinCode)
                {
                    location.PinCode = item.PinCode;
                }
                if (location.ContactPerson != item.ContactPerson)
                {
                    location.ContactPerson = item.ContactPerson;
                }
                if (location.ContactEmail != item.ContactEmail)
                {
                    location.ContactEmail = item.ContactEmail;
                }
                if (location.ContactPhone != item.ContactPhone)
                {
                    location.ContactPhone = item.ContactPhone;
                }
                if (location.IsDefault != item.IsDefault)
                {
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
