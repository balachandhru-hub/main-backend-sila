using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateSupplierQuotation
{
    public class UpdateSupplierQuotationCommand : IRequest<UpdateSupplierQuotationResultDto>
    {
        public UpdateSupplierQuotationDto Quotation { get; }

        public UpdateSupplierQuotationCommand(UpdateSupplierQuotationDto quotation)
        {
            Quotation = quotation;
        }
    }
}