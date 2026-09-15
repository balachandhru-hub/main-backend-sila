using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public record UploadPredefinedMaterialCommand(
    UploadItemBuyerMasterDto UploadDto
) : IRequest<ExcelUploadResultDto>;
}