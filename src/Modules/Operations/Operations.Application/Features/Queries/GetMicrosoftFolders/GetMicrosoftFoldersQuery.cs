using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetMicrosoftFolders
{
    /// <summary>
    /// Lists the sub-folders of a folder (or of the root) of a SharePoint document library.
    /// </summary>
    public class GetMicrosoftFoldersQuery : IRequest<List<MicrosoftFolderResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
        public MicrosoftFolderListRequestDto Request { get; set; } = new();
    }
}
