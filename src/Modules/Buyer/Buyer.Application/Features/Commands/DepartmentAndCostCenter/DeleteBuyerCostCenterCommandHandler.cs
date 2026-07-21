using Buyer.Application.Features.Commands.CostCenter;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

public class DeleteBuyerCostCenterCommandHandler
    : IRequestHandler<DeleteBuyerCostCenterCommand, Guid>
{
    private readonly IRepositoryWrapper _repository;
     private readonly ILoggerManager _logger;

    public DeleteBuyerCostCenterCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> Handle(DeleteBuyerCostCenterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInfo($"Attempting to delete CostCenter with ID {request.Id}.");
        var costCenter = await _repository.BuyerCostCenter
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (costCenter == null)
        {
            _logger.LogError($"CostCenter with ID {request.Id} not found.");
            throw new NotFoundCustomException("Cost Center not found.", "");
        }
        _repository.BuyerCostCenter.Delete(costCenter);

        await _repository.SaveAsync();

        return costCenter.Id;
    }
}