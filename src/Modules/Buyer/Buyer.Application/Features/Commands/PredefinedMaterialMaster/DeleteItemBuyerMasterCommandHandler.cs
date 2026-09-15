using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class DeletePredefinedMaterialCommandHandler
        : IRequestHandler<DeletePredefinedMaterialCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeletePredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            DeletePredefinedMaterialCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting PredefinedMaterial : {request.Id}");

            var entity = await _repository.ItemBuyerMaster
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Predefined Material not found.",
                    "");
            }

            _repository.ItemBuyerMaster.Delete(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"PredefinedMaterial deleted : {entity.Id}");

            return true;
        }
    }
}