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

namespace Operations.Application.Features.Commands.UpsertSupplier
{
    public class UpsertSupplierCommandHandler : IRequestHandler<UpsertSupplierCommand, SupplierResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpsertSupplierCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierResponseDto> Handle(UpsertSupplierCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving supplier. SupplierId: {request.SupplierId}, SupplierCode: {request.Request.SupplierCode}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            SupplierUpsertRequestDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.SupplierCode) || string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogError($"Supplier code or name is missing. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Supplier code and name are required.", "Enter the supplier code and the supplier name.");
            }

            string entityCode = string.IsNullOrWhiteSpace(dto.EntityCode) ? Common.ENTITY_CODE_DEFAULT : dto.EntityCode.Trim();
            string supplierCode = dto.SupplierCode.Trim();
            SupplierMaster? supplier = null;
            if (request.SupplierId != null)
            {
                supplier = await _repository.SupplierMaster.FindFirstByConditionAsync(
                    x => x.Id == request.SupplierId && x.OrganizationId == request.OrganizationId);
                if (supplier == null)
                {
                    _logger.LogError($"Supplier not found. SupplierId: {request.SupplierId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Supplier not found.", "The supplier does not exist in your organization.");
                }
            }

            bool duplicate = await _repository.SupplierMaster
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.EntityCode == entityCode
                    && x.SupplierCode == supplierCode && (request.SupplierId == null || x.Id != request.SupplierId))
                .AnyAsync(cancellationToken);
            if (duplicate)
            {
                _logger.LogError($"Supplier code already exists. SupplierCode: {supplierCode}, EntityCode: {entityCode}, OrganizationId: {request.OrganizationId}");
                throw new ConflictCustomException("Supplier already exists.", "A supplier with this code already exists in this entity.");
            }

            if (supplier == null)
            {
                supplier = new SupplierMaster
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    SourceSystem = Common.SOURCE_SYSTEM_MANUAL
                };
                _repository.SupplierMaster.Create(supplier);
            }

            supplier.EntityCode = entityCode;
            supplier.SupplierCode = supplierCode;
            supplier.Name = dto.Name.Trim();
            supplier.NormalizedName = InvoiceWorkflow.Normalize(dto.Name);
            supplier.SearchName = dto.SearchName?.Trim();
            supplier.BusinessPartnerId = dto.BusinessPartnerId?.Trim();
            supplier.LegalName = dto.LegalName?.Trim();
            supplier.TaxNumber = dto.TaxNumber?.Trim();
            supplier.Trn = dto.Trn?.Trim();
            supplier.Email = dto.Email?.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Country = dto.Country?.Trim();
            supplier.City = dto.City?.Trim();
            supplier.PostalCode = dto.PostalCode?.Trim();
            supplier.Street = dto.Street?.Trim();
            supplier.Currency = dto.Currency?.Trim().ToUpperInvariant();
            supplier.IsBlocked = dto.IsBlocked;
            supplier.IsDeleted = dto.IsDeleted;
            supplier.Status = dto.Status;

            // An alias is the supplier's name as it appears on invoices; it must be unique in the entity.
            List<string> aliases = (dto.Aliases ?? new List<string>())
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Select(alias => alias.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<string> existingAliases = await _repository.SupplierAlias
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.EntityCode == entityCode)
                .Select(x => x.NormalizedAlias)
                .ToListAsync(cancellationToken);
            foreach (string alias in aliases)
            {
                string normalized = InvoiceWorkflow.Normalize(alias);
                if (normalized.Length == 0 || existingAliases.Contains(normalized))
                {
                    continue;
                }

                existingAliases.Add(normalized);
                _repository.SupplierAlias.Create(new SupplierAlias
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    SupplierId = supplier.Id,
                    EntityCode = entityCode,
                    Alias = alias,
                    NormalizedAlias = normalized,
                    SourceSystem = Common.SOURCE_SYSTEM_MANUAL,
                    IsConfirmed = true
                });
            }

            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, request.SupplierId == null ? "SUPPLIER_CREATED" : "SUPPLIER_UPDATED", "SupplierMaster", supplier.Id, supplier.SupplierCode);
            await _repository.SaveAsync();

            List<SupplierResponseDto> result = await ResponseBuilder.SuppliersAsync(_repository, new List<SupplierMaster> { supplier }, cancellationToken);
            _logger.LogInfo($"Supplier saved. SupplierId: {supplier.Id}, OrganizationId: {request.OrganizationId}");
            return result[0];
        }
    }
}
