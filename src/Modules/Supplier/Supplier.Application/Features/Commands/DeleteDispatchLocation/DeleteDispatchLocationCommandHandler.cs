using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Commands.DeleteDispatchLocation
{
    public class DeleteDispatchLocationCommandHandler : IRequestHandler<DeleteDispatchLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteDispatchLocationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteDispatchLocationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Soft deleting Dispatch Location Id: {request.Id} for SupplierId: {request.SupplierId}");

            var location = await _repository.SupplierDispatchLocation
                .FindByCondition(x => x.Id == request.Id && x.SupplierId == request.SupplierId)
                .FirstOrDefaultAsync(cancellationToken);

            if (location == null)
            {
                _logger.LogError($"Dispatch Location not found. Id: {request.Id}, SupplierId: {request.SupplierId}");
                throw new NotFoundCustomException("Dispatch location not found.", "The dispatch location does not exist or does not belong to the specified supplier.");
            }

            _logger.LogInfo($"Dispatch Location found. Marking as inactive. Id: {request.Id}");

             _repository.SupplierDispatchLocation.Delete(location);
            _logger.LogInfo($"Saving soft delete for Dispatch Location Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Dispatch Location soft deleted successfully. Id: {request.Id}");
            return request.Id;
        }
    }
}
