using MediatR;
using SharedKernel.Dto;

namespace Supplier.Application.Features.Commands.Asset
{
    public record UploadAssetCommand(
        AssetUploadDto assetUploadDto
    ) : IRequest<Guid>;
}