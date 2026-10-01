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

namespace Operations.Application.Features.Queries.GetSupplier
{
    public class GetSupplierQueryHandler : IRequestHandler<GetSupplierQuery, SupplierResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierResponseDto> Handle(GetSupplierQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier. SupplierId: {request.SupplierId}, OrganizationId: {request.OrganizationId}");

            SupplierMaster? supplier = await _repository.SupplierMaster
                .FindByCondition(x => x.Id == request.SupplierId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (supplier == null)
            {
                _logger.LogError($"Supplier not found. SupplierId: {request.SupplierId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Supplier not found.", "The supplier does not exist in your organization.");
            }

            List<SupplierResponseDto> result = await ResponseBuilder.SuppliersAsync(_repository, new List<SupplierMaster> { supplier }, cancellationToken);
            _logger.LogInfo($"Supplier fetched. SupplierId: {supplier.Id}");
            return result[0];
        }
    }
}
