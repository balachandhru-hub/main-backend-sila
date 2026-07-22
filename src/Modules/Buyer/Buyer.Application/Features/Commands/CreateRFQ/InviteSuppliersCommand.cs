using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.InviteSuppliers
{
    public class InviteSuppliersCommand : IRequest<bool>
    {
        public InviteSuppliersDto Invite { get; set; }

        public InviteSuppliersCommand(InviteSuppliersDto invite)
        {
            Invite = invite;
        }
    }
}