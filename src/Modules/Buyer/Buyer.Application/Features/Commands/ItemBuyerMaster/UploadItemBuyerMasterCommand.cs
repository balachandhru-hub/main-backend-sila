using MediatR;
using Microsoft.AspNetCore.Http;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public record UploadItemBuyerMasterCommand(
        IFormFile File,
        Guid OrganizationId
    ) : IRequest<int>;
}