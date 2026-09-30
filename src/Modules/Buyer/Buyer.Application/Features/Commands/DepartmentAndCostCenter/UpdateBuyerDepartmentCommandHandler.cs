using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Buyer.Application.Features.Commands.Department;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

public class UpdateBuyerDepartmentCommandHandler
    : IRequestHandler<UpdateBuyerDepartmentCommand, Guid>
{
    private readonly IRepositoryWrapper _repository;
     private readonly ILoggerManager _logger;

    public UpdateBuyerDepartmentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> Handle(UpdateBuyerDepartmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInfo($"Updating Department with ID {request.Id}.");
        var department = await _repository.BuyerDepartment
            .FindByCondition(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (department == null)
        {
            _logger.LogError($"Department with ID {request.Id} not found.");
            throw new NotFoundCustomException("Department not found.", "");
        }
        _logger.LogInfo($"Updating Department with ID {request.Id} to new value: {request.BuyerDepartmentDto.Department}.");
        department.Department = request.BuyerDepartmentDto.Department;

        _repository.BuyerDepartment.Update(department);
        await _repository.SaveAsync();

        return department.Id;
    }
}