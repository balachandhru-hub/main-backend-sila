using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CreateStorageConnection
{
    /// <summary>
    /// Creates an external document storage connection. A Microsoft connection then needs the Microsoft sign-in and a validated folder.
    /// </summary>
    public class CreateStorageConnectionCommand : IRequest<StorageConnectionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public CreateStorageConnectionRequestDto Request { get; set; } = new();
    }
}
