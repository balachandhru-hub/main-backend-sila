using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpsertCatalogMaterialMapping
{
    public class UpsertCatalogMaterialMappingCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public CatalogMaterialMappingWriteDto Request { get; set; } = new();
    }
}
