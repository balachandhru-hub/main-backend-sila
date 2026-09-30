using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateSupplierQuotation
{
    public class UpdateSupplierQuotationCommand
        : IRequest<UpdateSupplierQuotationResultDto>
    {
        public UpdateSupplierQuotationDto Quotation { get; }

        public string? TemporaryVerificationToken { get; }

        public UpdateSupplierQuotationCommand(
            UpdateSupplierQuotationDto quotation,
            string? temporaryVerificationToken)
        {
            Quotation = quotation;
            TemporaryVerificationToken = temporaryVerificationToken;
        }
    }
}
