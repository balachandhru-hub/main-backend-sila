using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Buyer.Application.Features.Commands.CostCenter;
using Buyer.Infrastructure.Contracts.IRepository;

public class UpdateBuyerCostCenterCommandHandler
    : IRequestHandler<UpdateBuyerCostCenterCommand, Guid>
{
     private readonly IRepositoryWrapper _repository;

    public UpdateBuyerCostCenterCommandHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(UpdateBuyerCostCenterCommand request, CancellationToken cancellationToken)
    {
        var department = await _repository.BuyerCostCenter
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (department == null)
            throw new NotFoundCustomException("CostCenter not found.", "");

        department.CostCenter = request.BuyerDepartmentDto.CostCenter;

        _repository.BuyerCostCenter.Update(department);
        await _repository.SaveAsync();

        return department.Id;
    }
}