using MediatR;

namespace Buyer.Application.Features.Commands.Template
{
    public class DeleteVerificationTemplateCommand : IRequest<bool>
    {
        public Guid TemplateId { get; set; }

     
    }
}