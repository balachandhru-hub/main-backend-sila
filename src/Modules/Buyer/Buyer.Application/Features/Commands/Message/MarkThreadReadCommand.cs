using MediatR;

namespace Buyer.Application.Features.Commands.CreateMessage
{
    public class MarkThreadReadCommand : IRequest<bool>
    {
        public Guid ThreadId { get; }
        public Guid OrganizationId { get; }
        public string OrganizationType { get; }

        public MarkThreadReadCommand(Guid threadId, Guid organizationId, string organizationType)
        {
            ThreadId = threadId;
            OrganizationId = organizationId;
            OrganizationType = organizationType;
        }
    }
}
