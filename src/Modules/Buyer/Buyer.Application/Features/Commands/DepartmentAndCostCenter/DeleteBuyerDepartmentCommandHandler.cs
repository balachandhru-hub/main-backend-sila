using Buyer.Application.Features.Commands.Department;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Department
{
    public class DeleteBuyerDepartmentCommandHandler
        : IRequestHandler<DeleteBuyerDepartmentCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteBuyerDepartmentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteBuyerDepartmentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Attempting to delete Department with ID {request.Id}.");
            var department = await _repository.BuyerDepartment
                .FindByCondition(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (department == null)
            {
                _logger.LogError($"Department with ID {request.Id} not found.");
                throw new NotFoundCustomException("Department not found.", "");
            }
            var costCenters = await _repository.BuyerCostCenter
                .FindByCondition(x => x.DepartmentId == department.Id)
                .ToListAsync(cancellationToken);

            if (costCenters.Any())
            {
                _logger.LogInfo($"Deleting {costCenters.Count} associated CostCenters for Department ID {request.Id}.");
                _repository.BuyerCostCenter.DeleteRange(costCenters);
            }

            _repository.BuyerDepartment.Delete(department);

            await _repository.SaveAsync();

            return department.Id;
        }
    }
}