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

namespace Operations.Application.Features.Queries.SearchPurchaseOrders
{
    public class SearchPurchaseOrdersQueryHandler : IRequestHandler<SearchPurchaseOrdersQuery, List<PurchaseOrderResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SearchPurchaseOrdersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<PurchaseOrderResponseDto>> Handle(SearchPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Searching purchase orders. Query: {request.Request.Query}, OpenOnly: {request.Request.OpenOnly}, OrganizationId: {request.OrganizationId}");

            List<PurchaseOrder> orders = await _repository.PurchaseOrder.SearchAsync(request.OrganizationId, request.Request, cancellationToken);
            List<PurchaseOrderResponseDto> result = await ResponseBuilder.PurchaseOrdersAsync(_repository, orders, cancellationToken);

            _logger.LogInfo($"Purchase orders found. Count: {result.Count}, OrganizationId: {request.OrganizationId}");
            return result;
        }
    }
}
