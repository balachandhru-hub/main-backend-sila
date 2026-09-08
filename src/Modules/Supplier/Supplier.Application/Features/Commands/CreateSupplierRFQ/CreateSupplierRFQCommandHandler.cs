    using MediatR;
    using Supplier.Infrastructure.Contracts.IRepository;
    using Supplier.Domain.Entities;
    using SharedKernel.LoggerServices;
    using Microsoft.AspNetCore.SignalR;
    using Supplier.Domain.Common;

    namespace Supplier.Application.Features.Commands.CreateSupplierRFQ
    {
        public class CreateSupplierRFQCommandHandler
            : IRequestHandler<CreateSupplierRFQCommand, Guid>
        {
            private readonly IRepositoryWrapper _repository;
            private readonly ILoggerManager _logger;
        

            public CreateSupplierRFQCommandHandler(
                IRepositoryWrapper repository,
                ILoggerManager logger
            )
            {
                _repository = repository;
                _logger = logger;
            
            }

            public async Task<Guid> Handle(
                CreateSupplierRFQCommand request,
                CancellationToken cancellationToken)
            {
                _logger.LogInfo(
                    $"Creating Supplier RFQ. BuyerRFQId : {request.RFQ.BuyerRFQId}");

                var supplierRFQ = new SupplierRFQ
                {
                    Id = Guid.NewGuid(),

                    BuyerRFQId = request.RFQ.BuyerRFQId,
                    RFQNumber = request.RFQ.RFQNumber,

                    BuyerId = request.RFQ.BuyerId,
                    SupplierId = request.RFQ.SupplierId,

                    BuyerName = request.RFQ.BuyerName,

                    Title = request.RFQ.Title,
                    Description = request.RFQ.Description,

                    StartDate = request.RFQ.StartDate,
                    EndDate = request.RFQ.EndDate,

                    AddLotOption = request.RFQ.AddLotOption,
                    Currency = request.RFQ.Currency,
                    Status = request.RFQ.Status,
                    DeliveryLocation=request.RFQ.DeliveryLocation,
                    SessionToken = request.RFQ.SessionToken
                };

                _repository.SupplierRFQ.Create(supplierRFQ);

                var supplierItems = new List<SupplierRFQItem>();

                if (request.RFQ.Items != null)
                {
                    foreach (var item in request.RFQ.Items)
                    {
                        var supplierItem = new SupplierRFQItem
                        {
                            Id = Guid.NewGuid(),

                            SupplierRFQId = supplierRFQ.Id,

                            BuyerRFQItemId = item.BuyerRFQItemId,

                            Description = item.Description,
                            Quantity = item.Quantity,
                            UOM = item.UOM,

                            MaterialCode = item.MaterialCode,
                            MaterialGroup = item.MaterialGroup,
                            CostCenter = item.CostCenter,
                            LineNumber = item.LineNumber
                        };

                        supplierItems.Add(supplierItem);

                        await _repository.SupplierRFQItem.CreateAsync(supplierItem);
                    }
                }

                var mapping = new RFQSupplierMapping
                {
                    Id = Guid.NewGuid(),

                    BuyerRFQId = supplierRFQ.BuyerRFQId,
                    RFQNumber = supplierRFQ.RFQNumber,

                    BuyerId = supplierRFQ.BuyerId,
                    SupplierId = supplierRFQ.SupplierId,

                    SupplierRFQId = supplierRFQ.Id
                };

                await _repository.RFQSupplierMapping.CreateAsync(mapping);

                if (request.RFQ.InvitedUserIds != null)
                {
                    foreach (var userId in request.RFQ.InvitedUserIds)
                    {
                        await _repository.RFQOrganizationUserMapping.CreateAsync(
                            new RFQOrganizationUserMapping
                            {
                                Id = Guid.NewGuid(),

                                BuyerRFQId = supplierRFQ.BuyerRFQId,
                                RFQNumber = supplierRFQ.RFQNumber,

                                BuyerId = supplierRFQ.BuyerId,
                                SupplierId = supplierRFQ.SupplierId,

                                SupplierRFQId = supplierRFQ.Id,

                                OrganizationId = request.RFQ.OrganizationId,
                                UserId = userId
                            });
                    }
                }

                // Create Empty Quotation
                var quotation = new SupplierQuotation
                {
                    Id = Guid.NewGuid(),

                    SupplierRFQId = supplierRFQ.Id,
                    BuyerRFQId = supplierRFQ.BuyerRFQId,

                    RFQNumber = supplierRFQ.RFQNumber,

                    BuyerId = supplierRFQ.BuyerId,
                    SupplierId = supplierRFQ.SupplierId,

                    TotalPrice = 0,

                    DeliveryCharge = null,
                    DeliveryType = null,

                    Discount = null,
                    DiscountType = null,

                    Tax = null,
                    TaxType = null,

                    Status = Common.QUOTATION_STATUS
                };

                _repository.SupplierQuotation.Create(quotation);

                // Create quotation items only for Item-wise RFQ
                if (!supplierRFQ.AddLotOption)
                {
                    foreach (var rfqItem in supplierItems)
                    {
                        var quotationItem = new SupplierQuotationItem
                        {
                            Id = Guid.NewGuid(),

                            SupplierQuotationId = quotation.Id,

                            SupplierRFQItemId = rfqItem.Id,
                            BuyerRFQItemId = rfqItem.BuyerRFQItemId,

                            BuyerRFQId = quotation.BuyerRFQId,
                            RFQNumber = quotation.RFQNumber,

                            BuyerId = quotation.BuyerId,
                            SupplierId = quotation.SupplierId,

                            QuotedPrice = 0,
                            QuotedAmount = 0,
                            SubTotal = 0,
                        };

                        await _repository.SupplierQuotationItem.CreateAsync(quotationItem);
                    }
                }

                await _repository.SaveAsync();
                

                _logger.LogInfo(
                    $"Supplier RFQ created successfully : {supplierRFQ.Id}");

                return supplierRFQ.Id;
            }
        }
    }