using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetSupplierQuotation
{
    public class GetSupplierQuotationByBuyerRFQIdQueryHandler
        : IRequestHandler<GetSupplierQuotationByBuyerRFQIdQuery, GetAllSupplierQuotationDto>
    {
        private readonly IRepositoryWrapper _repository;

        public GetSupplierQuotationByBuyerRFQIdQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<GetAllSupplierQuotationDto> Handle(
            GetSupplierQuotationByBuyerRFQIdQuery request,
            CancellationToken cancellationToken)
        {
            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x => x.BuyerRFQId == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"No Supplier RFQ found for BuyerRFQId: {request.RFQId}");
            }

            var quotation = await _repository.SupplierQuotation
                .FindByCondition(x => x.SupplierRFQId == supplierRFQ.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var quotationItems = new List<SupplierQuotationItemDto>();

            if (quotation != null)
            {
                quotationItems = await _repository.SupplierQuotationItem
                    .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                    .Select(x => new SupplierQuotationItemDto
                    {
                        QuotedPrice = x.QuotedPrice
                    })
                    .ToListAsync(cancellationToken);
            }

            return new GetAllSupplierQuotationDto
            {
                TotalPrice = quotation?.TotalPrice,
                DeliveryCharge = quotation?.DeliveryCharge,
                Tax = quotation?.Tax,
                Discount = quotation?.Discount,
                DeliveryType = quotation?.DeliveryType,
                Status = quotation?.Status,
                SupplierQuotationItems = quotationItems
            };
        }
    }
}