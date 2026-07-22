using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public record UploadItemBuyerMasterCommand(
    UploadItemBuyerMasterDto UploadDto
) : IRequest<ExcelUploadResultDto>;
}