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

namespace Operations.Application.Features.Commands.DeleteOrganizationUnit
{
    public class DeleteOrganizationUnitCommandHandler : IRequestHandler<DeleteOrganizationUnitCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteOrganizationUnitCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteOrganizationUnitCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting organization unit. UnitId: {request.UnitId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            OrganizationUnit? unit = await _repository.OrganizationUnit.FindFirstByConditionAsync(
                x => x.Id == request.UnitId && x.OrganizationId == request.OrganizationId && x.IsActive);
            if (unit == null)
            {
                _logger.LogError($"Organization unit not found. UnitId: {request.UnitId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Unit not found.", "The unit does not exist in your organization.");
            }

            bool hasChildren = await _repository.OrganizationUnit
                .FindByCondition(x => x.ParentUnitId == unit.Id && x.OrganizationId == request.OrganizationId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (hasChildren)
            {
                _logger.LogError($"Organization unit has sub-units. UnitId: {unit.Id}");
                throw new ConflictCustomException("Unit has sub-units.", "Delete or move the sub-units of this unit first.");
            }

            // Documents, invoices and receipts keep pointing at the unit, so it is deactivated, not removed.
            unit.IsActive = false;
            unit.Status = StatusKind.INACTIVE;
            AuditTrail.Add(_repository, request.OrganizationId, unit.Id, request.UserId, "ORGANIZATION_UNIT_DELETED", "OrganizationUnit", unit.Id, unit.Code);
            await _repository.SaveAsync();

            _logger.LogInfo($"Organization unit deleted. UnitId: {unit.Id}, OrganizationId: {request.OrganizationId}");
            return unit.Id;
        }
    }
}
