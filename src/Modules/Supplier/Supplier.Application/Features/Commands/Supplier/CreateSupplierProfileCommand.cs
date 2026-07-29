using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Supplier
{
    public class CreateSupplierProfileCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }

        public SupplierBusinessProfileDto BusinessProfile { get; set; }

        public List<SupplierRegistrationDto> Registrations { get; set; }

        public List<SupplierBankAccountDto> BankAccounts { get; set; }

        public List<SupplierDispatchLocationDto> DispatchLocations { get; set; }

        public List<SupplierCategoryDto> SupplierCategories { get; set; }
    }
}