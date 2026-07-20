using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Buyer.Application.Features.Commands.Department;
using Buyer.Infrastructure.Contracts.IRepository;

public class UpdateBuyerDepartmentCommandHandler
    : IRequestHandler<UpdateBuyerDepartmentCommand, Guid>
{
     private readonly IRepositoryWrapper _repository;

    public UpdateBuyerDepartmentCommandHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(UpdateBuyerDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _repository.BuyerDepartment
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (department == null)
            throw new NotFoundCustomException("Department not found.", "");

        department.Department = request.BuyerDepartmentDto.Department;

        _repository.BuyerDepartment.Update(department);
        await _repository.SaveAsync();

        return department.Id;
    }
}