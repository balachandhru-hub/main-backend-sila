using Buyer.Application.Features.Commands.CostCenter;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

public class DeleteBuyerCostCenterCommandHandler
    : IRequestHandler<DeleteBuyerCostCenterCommand, Guid>
{
    private readonly IRepositoryWrapper _repository;

    public DeleteBuyerCostCenterCommandHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(DeleteBuyerCostCenterCommand request, CancellationToken cancellationToken)
    {
        var costCenter = await _repository.BuyerCostCenter
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (costCenter == null)
            throw new NotFoundCustomException("Cost Center not found.", "");

        _repository.BuyerCostCenter.Delete(costCenter);

        await _repository.SaveAsync();

        return costCenter.Id;
    }
}