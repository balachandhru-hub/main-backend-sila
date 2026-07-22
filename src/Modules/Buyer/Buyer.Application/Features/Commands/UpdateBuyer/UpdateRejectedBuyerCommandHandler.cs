using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Microsoft.AspNetCore.Http;
using Buyer.Domain.Entities;




namespace Buyer.Application.Features.Commands.Buyer.UpdateRejectedBuyer
{
    public class UpdateRejectedBuyerCommandHandler
        : IRequestHandler<UpdateRejectedBuyerCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IIdentityApiClient _identityApiClient;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMetadataApiClient _metadataApiClient;

        public UpdateRejectedBuyerCommandHandler(
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
            UpdateRejectedBuyerCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Starting update process for rejected buyer with ID: {request.Buyer.BuyerId}");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.Id == request.Buyer.BuyerId &&
                    x.IsActive);

            if (buyer == null)
            {
                _logger.LogError($"Buyer with ID {request.Buyer.BuyerId} not found.");
                throw new NotFoundCustomException(
                    "Buyer not found",
                    "Buyer does not exist.");
            }



            if (!buyer.Status.Equals(Common.REJECTED_STATUS,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Buyer with ID {request.Buyer.BuyerId} is not in REJECTED status. Current status: {buyer.Status}");
                throw new BadRequestCustomException(
                    "Buyer cannot be edited.",
                    "Only rejected buyers can edit their profile.");
            }



            var profile = request.Buyer.BusinessProfile;

            bool organizationChanged =
                   buyer.OrganizationName != profile.OrganizationName
                || buyer.Email != profile.Email
                || buyer.Phone != profile.Phone
                || buyer.Country != profile.Country
                || buyer.AddressLine1 != profile.AddressLine1
                || buyer.AddressLine2 != profile.AddressLine2
                || buyer.City != profile.City
                || buyer.State != profile.State
                || buyer.PinCode != profile.PinCode;
            //-------------------------------------------------
            // Update Identity Organization if required
            //-------------------------------------------------



            // Fetch metadata



            if (organizationChanged)
            {
                _logger.LogInfo($"Organization details have changed for buyer ID {request.Buyer.BuyerId}. Updating Identity service.");
                var token = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("Access token is missing in the request cookies.");
                    throw new UnauthorizedAccessException("Access token is missing.");
                }

                await _identityApiClient.UpdateOrganization(
                    new UpdateBuyerBusinessProfileDto
                    {
                        OrganizationId = buyer.OrganizationId,
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
            // Update Buyer Business Profile
            //--------------------------------------
            if (profile.OrganizationName != buyer.OrganizationName)
            {
                _logger.LogInfo($"Updating OrganizationName for buyer ID {request.Buyer.BuyerId} from '{buyer.OrganizationName}' to '{profile.OrganizationName}'.");
                buyer.OrganizationName = profile.OrganizationName;
            }
            if (profile.Email != buyer.Email)
            {
                _logger.LogInfo($"Updating Email for buyer ID {request.Buyer.BuyerId} from '{buyer.Email}' to '{profile.Email}'.");
                buyer.Email = profile.Email;
            }
            if (profile.Phone != buyer.Phone)
            {
                _logger.LogInfo($"Updating Phone for buyer ID {request.Buyer.BuyerId} from '{buyer.Phone}' to '{profile.Phone}'.");
                buyer.Phone = profile.Phone;
            }
            if (profile.Country != buyer.Country)
            {
                _logger.LogInfo($"Updating Country for buyer ID {request.Buyer.BuyerId} from '{buyer.Country}' to '{profile.Country}'.");
                buyer.Country = profile.Country;
            }

            if (profile.AddressLine1 != buyer.AddressLine1)
            {
                _logger.LogInfo($"Updating AddressLine1 for buyer ID {request.Buyer.BuyerId} from '{buyer.AddressLine1}' to '{profile.AddressLine1}'.");
                buyer.AddressLine1 = profile.AddressLine1;
            }
            if (profile.AddressLine2 != buyer.AddressLine2)
            {
                _logger.LogInfo($"Updating AddressLine2 for buyer ID {request.Buyer.BuyerId} from '{buyer.AddressLine2}' to '{profile.AddressLine2}'.");
                buyer.AddressLine2 = profile.AddressLine2;
            }
            if (profile.City != buyer.City)
            {
                _logger.LogInfo($"Updating City for buyer ID {request.Buyer.BuyerId} from '{buyer.City}' to '{profile.City}'.");
                buyer.City = profile.City;
            }
            if (profile.State != buyer.State)
            {
                _logger.LogInfo($"Updating State for buyer ID {request.Buyer.BuyerId} from '{buyer.State}' to '{profile.State}'.");
                buyer.State = profile.State;
            }
            if (profile.PinCode != buyer.PinCode)
            {
                _logger.LogInfo($"Updating PinCode for buyer ID {request.Buyer.BuyerId} from '{buyer.PinCode}' to '{profile.PinCode}'.");
                buyer.PinCode = profile.PinCode;
            }

            if (profile.Industry != buyer.Industry)
            {
                _logger.LogInfo($"Updating Industry for buyer ID {request.Buyer.BuyerId} from '{buyer.Industry}' to '{profile.Industry}'.");
                buyer.Industry = profile.Industry;
            }
            if (profile.BusinessType != buyer.BusinessType)
            {
                _logger.LogInfo($"Updating BusinessType for buyer ID {request.Buyer.BuyerId} from '{buyer.BusinessType}' to '{profile.BusinessType}'.");
                buyer.BusinessType = profile.BusinessType;
            }
            if (profile.EmployeeCount != buyer.EmployeeCount)
            {
                _logger.LogInfo($"Updating EmployeeCount for buyer ID {request.Buyer.BuyerId} from '{buyer.EmployeeCount}' to '{profile.EmployeeCount}'.");
                buyer.EmployeeCount = profile.EmployeeCount;
            }
            if (profile.AnnualTurnover != buyer.AnnualTurnover)
            {
                _logger.LogInfo($"Updating AnnualTurnover for buyer ID {request.Buyer.BuyerId} from '{buyer.AnnualTurnover}' to '{profile.AnnualTurnover}'.");
                buyer.AnnualTurnover = profile.AnnualTurnover;
            }
            if (profile.Currency != buyer.Currency)
            {
                _logger.LogInfo($"Updating Currency for buyer ID {request.Buyer.BuyerId} from '{buyer.Currency}' to '{profile.Currency}'.");
                buyer.Currency = profile.Currency;
            }
            if (profile.YearEstablished != buyer.YearEstablished)
            {
                _logger.LogInfo($"Updating YearEstablished for buyer ID {request.Buyer.BuyerId} from '{buyer.YearEstablished}' to '{profile.YearEstablished}'.");
                buyer.YearEstablished = profile.YearEstablished;
            }
            if (profile.Website != buyer.Website)
            {
                _logger.LogInfo($"Updating Website for buyer ID {request.Buyer.BuyerId} from '{buyer.Website}' to '{profile.Website}'.");
                buyer.Website = profile.Website;
            }
            if (profile.Description != buyer.Description)
            {
                _logger.LogInfo($"Updating Description for buyer ID {request.Buyer.BuyerId} from '{buyer.Description}' to '{profile.Description}'.");
                buyer.Description = profile.Description;
            }
            //-------------------------------------------------
            // Buyer Categories 
            //-------------------------------------------------

        

            var existingCategories = _repository.BuyerCategory
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToList();

            var requestCategories = request.Buyer.BuyerCategories ?? new List<UpdateBuyerCategoryDto>();

            var categoriesToCreate = new List<BuyerCategory>();
            var categoriesToUpdate = new List<BuyerCategory>();

            foreach (var item in requestCategories)
            {
                var category = existingCategories.FirstOrDefault(x =>
                    x.Segment == item.Segment &&
                    x.Family == item.Family &&
                    x.Class == item.Class &&
                    x.Commodity == item.Commodity);

                if (category == null)
                {
                    _logger.LogInfo(
                        $"Category not found for buyer ID {buyer.Id}. Creating new category with Segment: {item.Segment}, Family: {item.Family}, Class: {item.Class}, Commodity: {item.Commodity}.");

                    category = new BuyerCategory
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        Segment = item.Segment,
                        SegmentTitle = item.SegmentTitle,
                        Family = item.Family,
                        FamilyTitle = item.FamilyTitle,
                        Class = item.Class,
                        ClassTitle = item.ClassTitle,
                        Commodity = item.Commodity,
                        CommodityTitle = item.CommodityTitle,
                        IsActive = true
                    };

                    categoriesToCreate.Add(category);
                }
                else
                {
                    _logger.LogInfo(
                        $"Existing category found for buyer ID {buyer.Id}. Checking for updates. Segment: {item.Segment}, Family: {item.Family}, Class: {item.Class}, Commodity: {item.Commodity}");

                    if (category.Segment != item.Segment)
                        category.Segment = item.Segment;

                    if (category.SegmentTitle != item.SegmentTitle)
                        category.SegmentTitle = item.SegmentTitle;

                    if (category.Family != item.Family)
                        category.Family = item.Family;

                    if (category.FamilyTitle != item.FamilyTitle)
                        category.FamilyTitle = item.FamilyTitle;

                    if (category.Class != item.Class)
                        category.Class = item.Class;

                    if (category.ClassTitle != item.ClassTitle)
                        category.ClassTitle = item.ClassTitle;

                    if (category.Commodity != item.Commodity)
                        category.Commodity = item.Commodity;

                    if (category.CommodityTitle != item.CommodityTitle)
                        category.CommodityTitle = item.CommodityTitle;

                    categoriesToUpdate.Add(category);
                }
            }


            foreach (var existing in existingCategories)
            {
                bool exists = requestCategories.Any(x =>
                    x.Segment == existing.Segment &&
                    x.Family == existing.Family &&
                    x.Class == existing.Class &&
                    x.Commodity == existing.Commodity);

                if (!exists)
                {
                    existing.IsActive = false;
                    categoriesToUpdate.Add(existing);
                }
            }


            if (categoriesToCreate.Any())
            {
                _repository.BuyerCategory.CreateRange(categoriesToCreate);
            }


            if (categoriesToUpdate.Any())
            {
                _repository.BuyerCategory.UpdateRange(categoriesToUpdate);
            }
            //-------------------------------------------------
            // Registration Update
            //-------------------------------------------------

            foreach (var item in request.Buyer.BuyerDocumentRegistrations)
            {
                _logger.LogInfo($"Updating registration with ID {item.Id} for buyer ID {request.Buyer.BuyerId}.");
                var registration = _repository.BuyerRegistration
                    .FindFirstByCondition(x =>
                        x.BuyerId == buyer.Id &&
                        x.IsActive);

                if (registration == null)
                {
                    _logger.LogError($"Registration with ID {item.Id} not found for buyer ID {request.Buyer.BuyerId}.");
                    throw new NotFoundCustomException(
                        "Registration not found.",
                        $"Registration with Id '{item.Id}' was not found.");
                }

                if (registration.RegistrationType != item.RegistrationType)
                {


                    registration.RegistrationType = item.RegistrationType;
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

                if (item.RegistrationDocument != null)
                {
                    _logger.LogInfo($"Uploading new asset for registration ID {item.Id}.");

                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(item.RegistrationDocument),
                        cancellationToken);

                    registration.AssetId = assetId;
                }

                registration.IsVerified = false;
                registration.VerifiedOn = null;

                _repository.BuyerRegistration.Update(registration);
            }

            //-------------------------------------------------
            // Bank Accounts
            //-------------------------------------------------

            foreach (var item in request.Buyer.BuyerBankAccounts)
            {
                _logger.LogInfo($"Updating bank account with ID {item.Id} for buyer ID {request.Buyer.BuyerId}.");
                var bank = _repository.BuyerBankAccount
                    .FindFirstByCondition(x =>
                     x.BuyerId == buyer.Id &&
                        x.IsActive);

                if (bank == null)
                {
                    _logger.LogError($"Bank account with ID {item.Id} not found for buyer ID {request.Buyer.BuyerId}.");
                    throw new NotFoundCustomException(
                        "Bank account not found.",
                        $"Bank account with Id '{item.Id}' was not found.");
                }
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
                if (bank.Currency != item.Currency)
                {

                    bank.Currency = item.Currency;
                }
                if (bank.IsPrimary != item.IsPrimary)
                {

                    bank.IsPrimary = item.IsPrimary;
                }


                _repository.BuyerBankAccount.Update(bank);
            }

            //-------------------------------------------------
            // Dispatch Locations
            //-------------------------------------------------

            foreach (var item in request.Buyer.BuyerDeliveryLocations)
            {
                _logger.LogInfo($"Updating dispatch location with ID {item.Id} for buyer ID {request.Buyer.BuyerId}.");
                var location = _repository.BuyerDeliveryLocation
                    .FindFirstByCondition(x =>
                     x.BuyerId == buyer.Id &&
                        x.IsActive);

                if (location == null)
                {
                    _logger.LogError($"Dispatch location with ID {item.Id} not found for buyer ID {request.Buyer.BuyerId}.");
                    throw new NotFoundCustomException(
                        "Dispatch location not found.",
                        $"Dispatch location with Id '{item.Id}' was not found.");
                }
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

                if (location.ContactPhone != item.ContactPhone)
                {

                    location.ContactPhone = item.ContactPhone;
                }
                if (location.IsDefault != item.IsDefault)
                {

                    location.IsDefault = item.IsDefault;
                }


                _repository.BuyerDeliveryLocation.Update(location);
            }

            //-------------------------------------------------
            // Move to ReVerification
            //-------------------------------------------------

            buyer.Status = Common.REVERIFICATION_STATUS;
            buyer.Comment = null;

            _repository.BuyerBusinessProfile.Update(buyer);

            await _repository.SaveAsync();

            _logger.LogInfo($"Buyer {buyer.Id} updated successfully and moved to ReVerification.");

            return true;
        }
    }
}
