using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class UpdatePredefinedMaterialCommandHandler
        : IRequestHandler<UpdatePredefinedMaterialCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdatePredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdatePredefinedMaterialCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating ItemBuyerMaster : {request.Id}");

            var entity = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.Id == request.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Item Buyer Master not found.",
                    "");
            }

            var duplicate = await _repository.ItemBuyerMaster
                .FindByCondition(x =>
                    x.MaterialCode == request.ItemBuyerMasterDto.MaterialCode &&
                    x.Id != request.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate != null)
            {
                throw new PreConditionFailedCustomException(
                    "Material Code already exists.",
                    "");
            }

            entity.Description = request.ItemBuyerMasterDto.Description;
            entity.MaterialCode = request.ItemBuyerMasterDto.MaterialCode;
            entity.MaterialGroup = request.ItemBuyerMasterDto.MaterialGroup;

            _repository.ItemBuyerMaster.Update(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"ItemBuyerMaster updated : {entity.Id}");

            return entity.Id;
        }
    }
}