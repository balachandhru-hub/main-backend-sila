using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Buyer.Application.Features.Commands.Department;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class CreateBuyerDepartmentCommandHandler
        : IRequestHandler<CreateBuyerDepartmentCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
         private readonly ILoggerManager _logger;

        public CreateBuyerDepartmentCommandHandler(
            IRepositoryWrapper repository, ILoggerManager logger
            )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBuyerDepartmentCommand request, CancellationToken cancellationToken)
        {
            Guid buyerId;

            if (request.BuyerDepartmentDto.BuyerId.HasValue &&
                request.BuyerDepartmentDto.BuyerId.Value != Guid.Empty)
            {
                _logger.LogInfo($"Using provided BuyerId: {request.BuyerDepartmentDto.BuyerId.Value}");
                buyerId = request.BuyerDepartmentDto.BuyerId.Value;
            }
            else
            {
                _logger.LogInfo($"Fetching BuyerId for OrganizationId: {request.BuyerDepartmentDto.OrganizationId}");
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x => x.OrganizationId == request.BuyerDepartmentDto.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer == null)
                {
                    _logger.LogError($"Buyer with OrganizationId {request.BuyerDepartmentDto.OrganizationId} not found.");
                    throw new NotFoundCustomException("Buyer is not there", "");
                }
                buyerId = buyer.Id;
            }

            var department = new BuyerDepartment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                Department = request.BuyerDepartmentDto.Department
            };

            await _repository.BuyerDepartment.CreateAsync(department);

            if (request.BuyerDepartmentDto.CostCenter == null ||
                !request.BuyerDepartmentDto.CostCenter.Any())
            {
                _logger.LogError("No Cost Centers provided for the department.");
                throw new BadRequestCustomException("At least one Cost Center is required.", "");
            }

            foreach (var costCenterName in request.BuyerDepartmentDto.CostCenter)
            {
                _logger.LogInfo($"Creating CostCenter '{costCenterName}' for Department '{department.Department}'.");
                var costCenter = new BuyerCostCenter
                {
                    Id = Guid.NewGuid(),
                    DepartmentId = department.Id,
                    CostCenter = costCenterName
                };

                await _repository.BuyerCostCenter.CreateAsync(costCenter);
            }

            await _repository.SaveAsync();

            return department.Id;
        }
    }
}