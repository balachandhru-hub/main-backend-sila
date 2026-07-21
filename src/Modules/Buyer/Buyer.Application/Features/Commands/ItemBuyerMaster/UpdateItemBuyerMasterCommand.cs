using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public class UpdateItemBuyerMasterCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public UpdateItemBuyerMasterDto ItemBuyerMasterDto { get; set; }

        public UpdateItemBuyerMasterCommand(
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