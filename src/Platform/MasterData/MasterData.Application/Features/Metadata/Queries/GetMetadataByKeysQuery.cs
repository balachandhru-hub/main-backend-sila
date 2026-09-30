using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Metadata.Queries
{
    public record GetMetadataByKeysQuery(
        string Type,
        List<string> Keys)
        : IRequest<List<MetadataDto>>;
}