using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Contracts;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    public class GetSupplierRFQByIdQueryHandler : IRequestHandler<GetSupplierRFQByIdQuery, GetRFQByIdDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMetadataApiClient _metadataClient;
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;

        public GetSupplierRFQByIdQueryHandler(IRepositoryWrapper repository, IMetadataApiClient metadataApiClient, ILoggerManager logger, IBuyerApiClient buyerApiClient)
        {
            _repository = repository;
            _metadataClient = metadataApiClient;
            _logger = logger;
            _buyerApiClient = buyerApiClient;
        }

        public async Task<GetRFQByIdDto> Handle(
            GetSupplierRFQByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching RFQ details for RFQId: {request.RFQId}");

            var rfq = await _repository.SupplierRFQ
                .FindByCondition(x => x.BuyerRFQId == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId}.");
            }

            var buyerId = rfq.BuyerId;
            var supplierRFQId = rfq.Id;
            _logger.LogInfo($"Fetching verified suppliers for BuyerId: {buyerId}");
            var attachmentResponse = await _buyerApiClient.GetRFQAttachments(
                request.RFQId,
                cancellationToken);
            var questions = await _buyerApiClient.GetRFQQuestions(
                request.RFQId,
                cancellationToken);

            _logger.LogInfo($"Fetching RFQ items for SupplierRFQId: {supplierRFQId}");
            var rfqItems = await _repository.SupplierRFQItem
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                .ToListAsync(cancellationToken);

            var items = new List<GetRFQItemDto>();

            foreach (var item in rfqItems)
            {
                _logger.LogInfo($"Fetching attachments for RFQItemId: {item.BuyerRFQItemId}");
                var itemAttachment = attachmentResponse.ItemAttachments
                    .FirstOrDefault(x => x.RFQItemId == item.BuyerRFQItemId);

                items.Add(new GetRFQItemDto
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UOM = item.UOM,
                    MaterialCode = item.MaterialCode,
                    MaterialGroup = item.MaterialGroup,
                    CostCenter = item.CostCenter,
                    Attachments = itemAttachment?.Attachments ?? new List<AssetDto>(),
                    SupplierRFQItemId = item.Id,
                    SupplierRFQId = item.SupplierRFQId,
                    BuyerRFQItemId = item.BuyerRFQItemId


                });
            }

            _logger.LogInfo($"Fetching technical specification and terms documents for RFQId: {request.RFQId}");
            var technicalDocuments = attachmentResponse.TechnicalSpecificationDocuments;
            var termsDocuments = attachmentResponse.TermsConditionDocuments;
            // Supplier Quotation Header
            var quotation = await _repository.SupplierQuotation
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            // Supplier Quotation Items
            var quotationItems = new List<SupplierQuotationItemDto>();

            if (quotation != null)
            {
                quotationItems = await _repository.SupplierQuotationItem
                    .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                    .Select(x => new SupplierQuotationItemDto
                    {
                        ItemQutationId = x.Id,
                        QuotedPrice = x.QuotedPrice
                    })
                    .ToListAsync(cancellationToken);
            }




            return new GetRFQByIdDto
            {
                Title = rfq.Title,
                Description = rfq.Description,
                DeliveryLocation = rfq.DeliveryLocation,
                StartDate = rfq.StartDate,
                EndDate = rfq.EndDate,
                AddLotOption = rfq.AddLotOption,
                TechnicalSpecificationDocuments = technicalDocuments,
                TermsConditionDocuments = termsDocuments,

                Items = items,
                Questions = questions,
                SupplierQuotation = quotation == null
        ? new List<GetSupplierQuotationDto>()
        : new List<GetSupplierQuotationDto>
        {
            new GetSupplierQuotationDto
            {
                TotalPrice = quotation.TotalPrice,
                DeliveryCharge = quotation.DeliveryCharge,
                Tax = quotation.Tax,
                Discount = quotation.Discount,
                DeliveryType = quotation.DeliveryType,
                Status = quotation.Status,
                QutationId=quotation.Id
            }
        },

                SupplierQuotationItems = quotationItems,


            };
        }
    }
}