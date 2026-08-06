using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Profile.Commands
{
    public class CreateBuyerProfileCommandHandler : IRequestHandler<CreateBuyerProfileCommand, Guid>
    {
        private readonly ILoggerManager _logger;
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;

        public CreateBuyerProfileCommandHandler(ILoggerManager logger, IRepositoryWrapper repository, IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Guid> Handle(CreateBuyerProfileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo("Starting buyer profile creation process.");
            var existingProfile = await _repository.BuyerBusinessProfile.FindFirstByConditionAsync(x => x.OrganizationId == request.CreateBuyerDto.OrganizationId);
            if (existingProfile != null)
            {
                _logger.LogError($"A buyer profile for organization ID {request.CreateBuyerDto.OrganizationId} already exists.");
                throw new PreConditionFailedCustomException("A buyer profile for this organization already exists.", $"A buyer profile for this organization already exists for organization ID {request.CreateBuyerDto.OrganizationId}.");
            }

            BuyerBusinessProfile buyerBusinessProfile = new BuyerBusinessProfile
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.CreateBuyerDto.OrganizationId,
                SNID = request.CreateBuyerDto.SNID,
                OrganizationName = request.CreateBuyerDto.OrganizationName,
                Email = request.CreateBuyerDto.Email,
                Phone = request.CreateBuyerDto.Phone,
                Country = request.CreateBuyerDto.Country,
                AddressLine1 = request.CreateBuyerDto.AddressLine1,
                AddressLine2 = request.CreateBuyerDto.AddressLine2,
                City = request.CreateBuyerDto.City,
                State = request.CreateBuyerDto.State,
                PinCode = request.CreateBuyerDto.PinCode,
                Industry = request.CreateBuyerDto.Industry,
                BusinessType = request.CreateBuyerDto.BusinessType,
                EmployeeCount = request.CreateBuyerDto.EmployeeCount,
                AnnualTurnover = request.CreateBuyerDto.AnnualTurnover,
                Currency = request.CreateBuyerDto.Currency,
                YearEstablished = request.CreateBuyerDto.YearEstablished,
                Website = request.CreateBuyerDto.Website,
                Description = request.CreateBuyerDto.Description,
                Status=Common.PENDING_STATUS
            };
            await _repository.BuyerBusinessProfile.CreateAsync(buyerBusinessProfile);

            List<BuyerCategory> buyerCategories = new List<BuyerCategory>();

            foreach (BuyerCategoryDto category in request.CreateBuyerDto.BuyerCategories)
            {
                BuyerCategory buyerCategory = new BuyerCategory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerBusinessProfile.Id,
                    Segment = category.Segment,
                    SegmentTitle = category.SegmentTitle,
                    Family = category.Family,
                    FamilyTitle = category.FamilyTitle,
                    Class = category.Class,
                    ClassTitle = category.ClassTitle,
                    Commodity = category.Commodity,
                    CommodityTitle = category.CommodityTitle
                };
                buyerCategories.Add(buyerCategory);
            }
            await _repository.BuyerCategory.CreateRangeAsync(buyerCategories);

            List<BuyerBankAccount> buyerBankAccounts = new List<BuyerBankAccount>();
            foreach (BuyerBankAccountDto bankAccount in request.CreateBuyerDto.BuyerBankAccounts)
            {
                BuyerBankAccount buyerBankAccount = new BuyerBankAccount
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerBusinessProfile.Id,
                    AccountHolderName = bankAccount.AccountHolderName,
                    BankName = bankAccount.BankName,
                    BranchName = bankAccount.BranchName,
                    AccountNumber = bankAccount.AccountNumber,
                    IFSCCode = bankAccount.IFSCCode,
                    SWIFTCode = bankAccount.SWIFTCode,
                Currency = bankAccount.Currency,
        IsPrimary = bankAccount.IsPrimary
    };

    buyerBankAccounts.Add(buyerBankAccount);
            }
            await _repository.BuyerBankAccount.CreateRangeAsync(buyerBankAccounts);
            List<BuyerRegistration> buyerDocumentRegistrations = new List<BuyerRegistration>();

            foreach (BuyerDocumentRegistrationDto registration in request.CreateBuyerDto.BuyerDocumentRegistrations)
            {
                BuyerRegistration buyerRegistration = new BuyerRegistration
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerBusinessProfile.Id,
                    RegistrationNumber = registration.RegistrationNumber,
                    RegistrationName = registration.RegistrationName,
                    ExpiryDate = registration.ExpiryDate,
                    RegistrationType = registration.RegistrationType
                };

                if (registration.RegistrationDocument != null)
                {
                    Guid assetId = await _mediator.Send(new UploadAssetCommand(registration.RegistrationDocument));
                    buyerRegistration.AssetId = assetId;
                }
                buyerDocumentRegistrations.Add(buyerRegistration);
            }
            await _repository.BuyerRegistration.CreateRangeAsync(buyerDocumentRegistrations);

            List<BuyerDeliveryLocation> buyerDeliveryLocations = new List<BuyerDeliveryLocation>();

            foreach (BuyerDeliveryLocationDto location in request.CreateBuyerDto.BuyerDeliveryLocations)
            {
                BuyerDeliveryLocation buyerDeliveryLocation = new BuyerDeliveryLocation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerBusinessProfile.Id,
                    LocationName = location.LocationName,
                    AddressLine1 = location.AddressLine1,
                    AddressLine2 = location.AddressLine2,
                    Country = location.Country,
                    City = location.City,
                    ContactPerson = location.ContactPerson,
                    ContactPhone = location.ContactPhone,
                    IsDefault = location.IsDefault,
                    PinCode=location.PinCode,
                    State=location.State
                };
                buyerDeliveryLocations.Add(buyerDeliveryLocation);
            }

            await _repository.BuyerDeliveryLocation.CreateRangeAsync(buyerDeliveryLocations);
            await _repository.SaveAsync();
            _logger.LogInfo($"Successfully Registerd the Buyer Profile Information for Id : {request.CreateBuyerDto.OrganizationId}");
            return buyerBusinessProfile.Id;
        }
    }
}