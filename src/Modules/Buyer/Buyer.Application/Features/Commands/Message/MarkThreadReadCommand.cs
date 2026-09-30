using MediatR;

namespace Buyer.Application.Features.Commands.CreateMessage
{
    public class MarkThreadReadCommand : IRequest<bool>
    {
        public Guid ThreadId { get; }
        public Guid OrganizationId { get; }
        public string OrganizationType { get; }

        /// <summary>Set only when the caller is a session-token-authenticated ExternalSupplier.</summary>
        public Guid? ExternalSupplierCallerId { get; }

        public MarkThreadReadCommand(Guid threadId, Guid organizationId, string organizationType)
        {
            ThreadId = threadId;
            OrganizationId = organizationId;
            OrganizationType = organizationType;
        }

        public MarkThreadReadCommand(Guid threadId, Guid externalSupplierCallerId)
        {
            ThreadId = threadId;
            ExternalSupplierCallerId = externalSupplierCallerId;
        }
    }
}
