using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetPurchaseOrder
{
    public class GetPurchaseOrderQueryHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPurchaseOrderQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PurchaseOrderResponseDto> Handle(GetPurchaseOrderQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching purchase order. PoNumber: {request.PoNumber}, EntityCode: {request.EntityCode}, OrganizationId: {request.OrganizationId}");

            string? entityCode = string.IsNullOrWhiteSpace(request.EntityCode) ? null : request.EntityCode;
            PurchaseOrder? order = await _repository.PurchaseOrder
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.PoNumber == request.PoNumber
                    && (entityCode == null || x.EntityCode == entityCode))
                .OrderBy(x => x.EntityCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (order == null)
            {
                _logger.LogError($"Purchase order not found. PoNumber: {request.PoNumber}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Purchase order not found.", "The purchase order does not exist in your organization.");
            }

            List<PurchaseOrderResponseDto> result = await ResponseBuilder.PurchaseOrdersAsync(_repository, new List<PurchaseOrder> { order }, cancellationToken);
            _logger.LogInfo($"Purchase order fetched. PurchaseOrderId: {order.Id}");
            return result[0];
        }
    }
}
