using Buyer.Application.Features.Commands.Department;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.Department
{
    public class DeleteBuyerDepartmentCommandHandler
        : IRequestHandler<DeleteBuyerDepartmentCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;

        public DeleteBuyerDepartmentCommandHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(DeleteBuyerDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = await _repository.BuyerDepartment
                .FindByCondition(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (department == null)
                throw new NotFoundCustomException("Department not found.", "");

            var costCenters = await _repository.BuyerCostCenter
                .FindByCondition(x => x.DepartmentId == department.Id)
                .ToListAsync(cancellationToken);

            if (costCenters.Any())
            {
                _repository.BuyerCostCenter.DeleteRange(costCenters);
            }

            _repository.BuyerDepartment.Delete(department);

            await _repository.SaveAsync();

            return department.Id;
        }
    }
}