using Buyer.Infrastructure.Contracts.IRepository;
using ItemBuyerMasterEntity = Buyer.Domain.Entities.ItemBuyerMaster;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Buyer.Domain.Entities;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public class CreateItemBuyerMasterCommandHandler
        : IRequestHandler<CreateItemBuyerMasterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateItemBuyerMasterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateItemBuyerMasterCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating Item Buyer Master.");

            var dto = request.ItemBuyerMaster;

            var exists = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x =>
                    x.MaterialCode == dto.MaterialCode &&
                    x.IsActive);

            if (exists != null)
            {
                throw new PreConditionFailedCustomException(
                    "Material Code already exists.",
                    $"Material Code '{dto.MaterialCode}' already exists.");
            }

            BuyerBusinessProfile? buyer;

            if (dto.BuyerId.HasValue)
            {
                // Platform flow
                buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.Id == dto.BuyerId.Value &&
                        x.IsActive);
            }
            else
            {
                // Buyer flow
                buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.OrganizationId &&
                        x.IsActive);
            }

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer profile not found.");
            }

            ItemBuyerMasterEntity entity = new ItemBuyerMasterEntity
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                Description = dto.Description,
                MaterialCode = dto.MaterialCode,
                MaterialGroup = dto.MaterialGroup
            };

            await _repository.ItemBuyerMaster.CreateAsync(entity);
            await _repository.SaveAsync();

            _logger.LogInfo($"ItemBuyerMaster created : {entity.Id}");

            return entity.Id;
        }
    }
}