using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class UpdatePredefinedMaterialCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public UpdateItemBuyerMasterDto ItemBuyerMasterDto { get; set; }

        public UpdatePredefinedMaterialCommand(
            Guid id,
            Guid organizationId,
            UpdateItemBuyerMasterDto dto)
        {
            Id = id;
            OrganizationId = organizationId;
            ItemBuyerMasterDto = dto;
        }
    }
}