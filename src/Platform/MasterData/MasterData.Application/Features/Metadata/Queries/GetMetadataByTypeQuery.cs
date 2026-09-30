using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Metadata.Queries;

public class GetMetadataByTypeQuery : IRequest<List<MetadataDto>>
{
    public List<string> Type { get; set; }

    public GetMetadataByTypeQuery(List<string> type)
    {
        Type = type;
    }
}