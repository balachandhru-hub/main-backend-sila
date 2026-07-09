using MediatR;
//using SharedKernel.Dto;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Asset
{
    public record UploadAssetCommand(
        AssetUploadDto assetUploadDto
    ) : IRequest<Guid>;
}