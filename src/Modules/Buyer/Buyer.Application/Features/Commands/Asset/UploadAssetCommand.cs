using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Assets.Commands
{
    public record UploadAssetCommand(
        AssetUploadDto assetUploadDto
    ) : IRequest<Guid>;
}