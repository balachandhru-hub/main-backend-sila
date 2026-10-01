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

namespace Operations.Application.Features.Queries.GetSuppliers
{
    public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSuppliersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SupplierResponseDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching suppliers. Query: {request.Request.Query}, EntityCode: {request.Request.EntityCode}, OrganizationId: {request.OrganizationId}");

            StatusKind? status = Enum.TryParse(request.Request.Status, true, out StatusKind parsedStatus) ? parsedStatus : null;
            List<SupplierMaster> suppliers = await _repository.SupplierMaster.SearchAsync(
                request.OrganizationId,
                request.Request.EntityCode,
                string.IsNullOrWhiteSpace(request.Request.Query) ? null : request.Request.Query.Trim(),
                status,
                500,
                cancellationToken);
            List<SupplierResponseDto> result = await ResponseBuilder.SuppliersAsync(_repository, suppliers, cancellationToken);

            _logger.LogInfo($"Suppliers fetched. Count: {result.Count}, OrganizationId: {request.OrganizationId}");
            return result;
        }
    }
}
