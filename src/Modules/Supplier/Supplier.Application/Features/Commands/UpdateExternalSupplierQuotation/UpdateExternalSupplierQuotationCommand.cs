using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateExternalSupplierQuotation
{
    /// <summary>
    /// Updates a quotation on behalf of an external supplier already
    /// authorized via session token (RFQId + SupplierId resolved by
    /// ApiSessionAuthorization) instead of the normal OTP-verified flow
    /// used by <see cref="Supplier.Application.Features.Commands.UpdateSupplierQuotation.UpdateSupplierQuotationCommand"/>.
    /// </summary>
    public class UpdateExternalSupplierQuotationCommand
        : IRequest<UpdateSupplierQuotationResultDto>
    {
        public UpdateSupplierQuotationDto Quotation { get; }

        public Guid RFQId { get; }

        public Guid SupplierId { get; }

        public UpdateExternalSupplierQuotationCommand(
            UpdateSupplierQuotationDto quotation,
            Guid rfqId,
            Guid supplierId)
        {
            Quotation = quotation;
            RFQId = rfqId;
            SupplierId = supplierId;
        }
    }
}
