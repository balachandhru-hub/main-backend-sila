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

namespace Operations.Application.Features.Commands.CreateOrganizationUnit
{
    public class CreateOrganizationUnitCommandHandler : IRequestHandler<CreateOrganizationUnitCommand, OrganizationUnitResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateOrganizationUnitCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OrganizationUnitResponseDto> Handle(CreateOrganizationUnitCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating organization unit. Code: {request.Request.Code}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            CreateOrganizationUnitRequestDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogError($"Unit code or name is missing. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Unit code and name are required.", "Enter the code and the name of the unit.");
            }

            string code = dto.Code.Trim().ToUpperInvariant();
            bool codeExists = await _repository.OrganizationUnit
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.Code == code)
                .AnyAsync(cancellationToken);
            if (codeExists)
            {
                _logger.LogError($"Unit code already exists. Code: {code}, OrganizationId: {request.OrganizationId}");
                throw new ConflictCustomException("Unit code already exists.", "A unit with this code already exists in your organization.");
            }

            await OperationsScope.EnsureUnitAsync(_repository, _logger, request.OrganizationId, dto.ParentUnitId, cancellationToken);

            OrganizationUnit unit = new OrganizationUnit
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                ParentUnitId = dto.ParentUnitId,
                Code = code,
                Name = dto.Name.Trim(),
                Kind = dto.Kind,
                Status = StatusKind.ACTIVE
            };
            _repository.OrganizationUnit.Create(unit);
            AuditTrail.Add(_repository, request.OrganizationId, unit.Id, request.UserId, "ORGANIZATION_UNIT_CREATED", "OrganizationUnit", unit.Id, unit.Code);
            await _repository.SaveAsync();

            _logger.LogInfo($"Organization unit created. UnitId: {unit.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.Unit(unit);
        }
    }
}
