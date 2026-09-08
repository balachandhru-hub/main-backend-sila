using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Contracts;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    /// <summary>
    /// Fetches RFQ details for an external (unregistered) supplier that has
    /// been authorized via a session token instead of the normal
    /// organization/JWT based flow, so it skips the organization and
    /// invited-user checks that <see cref="GetSupplierRFQByIdQueryHandler"/>
    /// performs.
    /// </summary>
    public class GetExternalSupplierRFQByIdQueryHandler
        : IRequestHandler<GetExternalSupplierRFQByIdQuery, GetRFQByIdDto>
    {
        private readonly IRepositoryWrapper _repositorywrapper;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly ILoggerManager _logger;

        public GetExternalSupplierRFQByIdQueryHandler(
            IRepositoryWrapper repository,
            IBuyerApiClient buyerApiClient,
            ILoggerManager logger)
        {
            _repositorywrapper = repository;
            _buyerApiClient = buyerApiClient;
            _logger = logger;
        }

        public async Task<GetRFQByIdDto> Handle(
            GetExternalSupplierRFQByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching external supplier RFQ details for RFQId: {request.RFQId}");

            var rfq = await _repositorywrapper.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == request.SupplierId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId}.");
            }

            var supplierRFQId = rfq.Id;

            var attachmentResponse = await _buyerApiClient.GetRFQAttachments(
                request.RFQId,
                cancellationToken);
            var questions = await _buyerApiClient.GetRFQQuestions(
                request.RFQId,
                cancellationToken);

            var rfqItems = await _repositorywrapper.SupplierRFQItem
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                .ToListAsync(cancellationToken);

            var items = new List<GetRFQItemDto>();

            foreach (var item in rfqItems)
            {
                var itemAttachment = attachmentResponse.ItemAttachments
                    .FirstOrDefault(x => x.RFQItemId == item.BuyerRFQItemId);

                string? costCenterName = null;

                if (!string.IsNullOrWhiteSpace(item.CostCenter) &&
                    Guid.TryParse(item.CostCenter, out var costCenterId))
                {
                    var costCenter = await _buyerApiClient.GetCostCenterById(
                        costCenterId,
                        cancellationToken);

                    costCenterName = costCenter?.CostCenter;
                }

                items.Add(new GetRFQItemDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UOM = item.UOM,
                    MaterialCode = item.MaterialCode,
                    MaterialGroup = item.MaterialGroup,
                    CostCenter = item.CostCenter,
                    CostCenterName = costCenterName,
                    Attachments = itemAttachment?.Attachments ?? new List<AssetDto>(),
                    SupplierRFQItemId = item.Id,
                    SupplierRFQId = item.SupplierRFQId,
                    BuyerRFQItemId = item.BuyerRFQItemId
                });
            }

            var technicalDocuments = attachmentResponse.TechnicalSpecificationDocuments;
            var termsDocuments = attachmentResponse.TermsConditionDocuments;

            var quotation = await _repositorywrapper.SupplierQuotation
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            var lowestQuotation = await _repositorywrapper.SupplierQuotation
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefaultAsync(cancellationToken);

            var quotationItems = new List<SupplierQuotationItemDto>();

            if (quotation != null)
            {
                quotationItems = await _repositorywrapper.SupplierQuotationItem
                    .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                    .Select(x => new SupplierQuotationItemDto
                    {
                        ItemQuotationId = x.Id,
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
                Status = rfq.Status,
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
                            QutationId = quotation.Id,
                            IsLead = quotation.Id == lowestQuotation?.Id
                        }
                    },
                SupplierQuotationItems = quotationItems,
                InvitedUsers = null
            };
        }
    }
}
