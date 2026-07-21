using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Buyer.Application.Features.Commands.CostCenter;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class CreateBuyerCostCenterCommandHandler
        : IRequestHandler<CreateBuyerCostCenterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateBuyerCostCenterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger
            )
        {
            _repository = repository;
            _logger = logger;
        }
        public async Task<Guid> Handle(CreateBuyerCostCenterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating CostCenter for Department ID {request.BuyerCostCenterDto.DepartmentId}.");
            var department = await _repository.BuyerDepartment
                .FindByCondition(x => x.Id == request.BuyerCostCenterDto.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (department == null)
            {
                _logger.LogError($"Department with ID {request.BuyerCostCenterDto.DepartmentId} not found.");
                throw new NotFoundCustomException("Department not found.", "");
            }
            var costCenter = new BuyerCostCenter
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                CostCenter = request.BuyerCostCenterDto.CostCenter
            };

            await _repository.BuyerCostCenter.CreateAsync(costCenter);
            await _repository.SaveAsync();

            return costCenter.Id;
        }

    }
}