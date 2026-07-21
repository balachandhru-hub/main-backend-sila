using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Buyer.Application.Features.Commands.CostCenter;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

public class UpdateBuyerCostCenterCommandHandler
    : IRequestHandler<UpdateBuyerCostCenterCommand, Guid>
{
    private readonly IRepositoryWrapper _repository;
     private readonly ILoggerManager _logger;

    public UpdateBuyerCostCenterCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> Handle(UpdateBuyerCostCenterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInfo($"Updating CostCenter with ID {request.Id}.");
        var department = await _repository.BuyerCostCenter
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (department == null)
        {
            _logger.LogError($"CostCenter with ID {request.Id} not found.");
            throw new NotFoundCustomException("CostCenter not found.", "");
        }
        department.CostCenter = request.BuyerDepartmentDto.CostCenter;
        _repository.BuyerCostCenter.Update(department);
        await _repository.SaveAsync();

        return department.Id;
    }
}