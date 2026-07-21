using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class UploadDepartmentCostCenterCommand : IRequest<bool>
    {
        public UploadDepartmentCostCenterDto UploadDepartmentCostCenterDto { get; set; }

        public UploadDepartmentCostCenterCommand(UploadDepartmentCostCenterDto dto)
        {
            UploadDepartmentCostCenterDto = dto;
        }
    }
}