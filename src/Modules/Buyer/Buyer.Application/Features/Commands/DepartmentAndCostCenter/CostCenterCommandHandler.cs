using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Buyer.Application.Features.Commands.CostCenter;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class CreateBuyerCostCenterCommandHandler
        : IRequestHandler<CreateBuyerCostCenterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;


        public CreateBuyerCostCenterCommandHandler(
            IRepositoryWrapper repository
            )
        {
            _repository = repository;

        }
        public async Task<Guid> Handle(CreateBuyerCostCenterCommand request, CancellationToken cancellationToken)
        {
            var department = await _repository.BuyerDepartment
                .FindByCondition(x => x.Id == request.BuyerCostCenterDto.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (department == null)
                throw new NotFoundCustomException("Department not found.", "");

            var costCenter = new BuyerCostCenter
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                CostCenter = request.BuyerCostCenterDto.CostCenter
            };

            await _repository.BuyerCostCenter.CreateAsync(costCenter);
            await _repository.SaveAsync();

            return costCenter.Id;
        }

    }
}