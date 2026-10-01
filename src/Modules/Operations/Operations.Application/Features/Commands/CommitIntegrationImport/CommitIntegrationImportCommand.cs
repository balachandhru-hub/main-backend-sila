using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CommitIntegrationImport
{
    /// <summary>
    /// Imports previewed spreadsheet rows. The rows are validated again and written together: one invalid row and nothing is changed.
    /// </summary>
    public class CommitIntegrationImportCommand : IRequest<IntegrationImportCommitResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationImportCommitInputDto Request { get; set; } = new();
    }
}
