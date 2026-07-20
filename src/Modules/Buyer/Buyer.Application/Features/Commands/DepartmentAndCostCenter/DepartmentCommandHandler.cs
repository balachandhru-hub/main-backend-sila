using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Buyer.Application.Features.Commands.Department;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class CreateBuyerDepartmentCommandHandler
        : IRequestHandler<CreateBuyerDepartmentCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;


        public CreateBuyerDepartmentCommandHandler(
            IRepositoryWrapper repository
            )
        {
            _repository = repository;

        }

        public async Task<Guid> Handle(CreateBuyerDepartmentCommand request, CancellationToken cancellationToken)
        {
            Guid buyerId;

            if (request.BuyerDepartmentDto.BuyerId.HasValue &&
                request.BuyerDepartmentDto.BuyerId.Value != Guid.Empty)
            {
                buyerId = request.BuyerDepartmentDto.BuyerId.Value;
            }
            else
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x => x.OrganizationId == request.BuyerDepartmentDto.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer == null)
                    throw new NotFoundCustomException("Buyer is not there", "");

                buyerId = buyer.Id;
            }

         var department = new BuyerDepartment
{
    Id = Guid.NewGuid(),
    BuyerId = buyerId,
    Department = request.BuyerDepartmentDto.Department
};

await _repository.BuyerDepartment.CreateAsync(department);

if (request.BuyerDepartmentDto.CostCenter == null ||
    !request.BuyerDepartmentDto.CostCenter.Any())
{
    throw new BadRequestCustomException("At least one Cost Center is required.", "");
}

foreach (var costCenterName in request.BuyerDepartmentDto.CostCenter)
{
    var costCenter = new BuyerCostCenter
    {
        Id = Guid.NewGuid(),
        DepartmentId = department.Id,
        CostCenter = costCenterName
    };

    await _repository.BuyerCostCenter.CreateAsync(costCenter);
}

await _repository.SaveAsync();

return department.Id;
        }
    }
}