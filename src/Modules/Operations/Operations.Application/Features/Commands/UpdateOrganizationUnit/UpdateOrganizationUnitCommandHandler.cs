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

namespace Operations.Application.Features.Commands.UpdateOrganizationUnit
{
    public class UpdateOrganizationUnitCommandHandler : IRequestHandler<UpdateOrganizationUnitCommand, OrganizationUnitResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateOrganizationUnitCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OrganizationUnitResponseDto> Handle(UpdateOrganizationUnitCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating organization unit. UnitId: {request.UnitId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            UpdateOrganizationUnitRequestDto dto = request.Request;
            OrganizationUnit? unit = await _repository.OrganizationUnit.FindFirstByConditionAsync(
                x => x.Id == request.UnitId && x.OrganizationId == request.OrganizationId && x.IsActive);
            if (unit == null)
            {
                _logger.LogError($"Organization unit not found. UnitId: {request.UnitId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Unit not found.", "The unit does not exist in your organization.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BadRequestCustomException("Unit name is required.", "Enter the name of the unit.");
            }

            if (dto.ParentUnitId != null)
            {
                await OperationsScope.EnsureUnitAsync(_repository, _logger, request.OrganizationId, dto.ParentUnitId, cancellationToken);

                // A unit cannot be moved under itself or under one of its own descendants.
                Dictionary<Guid, Guid?> parents = await _repository.OrganizationUnit
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                    .ToDictionaryAsync(x => x.Id, x => x.ParentUnitId, cancellationToken);
                Guid? current = dto.ParentUnitId;
                HashSet<Guid> visited = new HashSet<Guid>();
                while (current != null && visited.Add(current.Value))
                {
                    if (current.Value == unit.Id)
                    {
                        _logger.LogError($"Unit cannot be its own ancestor. UnitId: {unit.Id}, ParentUnitId: {dto.ParentUnitId}");
                        throw new BadRequestCustomException("Invalid parent unit.", "A unit cannot be placed under itself or under one of its sub-units.");
                    }

                    current = parents.TryGetValue(current.Value, out Guid? parent) ? parent : null;
                }
            }

            unit.Name = dto.Name.Trim();
            unit.Kind = dto.Kind;
            unit.ParentUnitId = dto.ParentUnitId;
            unit.Status = dto.Status;
            AuditTrail.Add(_repository, request.OrganizationId, unit.Id, request.UserId, "ORGANIZATION_UNIT_UPDATED", "OrganizationUnit", unit.Id, unit.Code);
            await _repository.SaveAsync();

            _logger.LogInfo($"Organization unit updated. UnitId: {unit.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.Unit(unit);
        }
    }
}
