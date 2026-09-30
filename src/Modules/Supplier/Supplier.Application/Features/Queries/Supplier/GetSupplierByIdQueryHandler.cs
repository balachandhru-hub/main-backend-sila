using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.Dto;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierByIdQueryHandler
        : IRequestHandler<GetSupplierByIdQuery, OrganizationDto>
    {
        private readonly IRepositoryWrapper _repository;

        public GetSupplierByIdQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<OrganizationDto> Handle(
            GetSupplierByIdQuery request,
            CancellationToken cancellationToken)
        {
            var supplier = await _repository.SupplierBusinessProfile
                .FindByCondition(x =>
                    x.Id == request.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier does not exist.");
            }

            var dto = new OrganizationDto
            {
                
                OrganizationId = supplier.OrganizationId,
                SNID = supplier.SNID,

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
                    Status = supplier.Status,
                    Comment = supplier.Comment,
                    SNID = supplier.SNID,
                }
            };

            dto.Registrations = await _repository.SupplierRegistration
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive)
                .Select(x => new SupplierRegistrationResponseDto
                {
                    RegistrationType = x.RegistrationType,
                    RegistrationName = x.RegistrationName,
                    RegistrationNumber = x.RegistrationNumber,
                    ExpiryDate = x.ExpiryDate
                })
                .ToListAsync(cancellationToken);

            dto.BankAccounts = await _repository.SupplierBankAccount
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive)
                .Select(x => new SupplierBankAccountDto
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
                })
                .ToListAsync(cancellationToken);

            dto.DispatchLocations = await _repository.SupplierDispatchLocation
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive)
                .Select(x => new SupplierDispatchLocationDto
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
                })
                .ToListAsync(cancellationToken);
           dto.SupplierCategories = await _repository.SupplierCategory
            .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive)
            .Select(x => new SupplierCategoryDto
            {
                Segment = x.Segment,
                SegmentTitle = x.SegmentTitle,
                Family = x.Family,
                FamilyTitle = x.FamilyTitle,
                Class = x.Class,
                ClassTitle = x.ClassTitle,
                Commodity = x.Commodity,
                CommodityTitle = x.CommodityTitle
            })
            .ToListAsync(cancellationToken);
            return dto;
        }
    }
}