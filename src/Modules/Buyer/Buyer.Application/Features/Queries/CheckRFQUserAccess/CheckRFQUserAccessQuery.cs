using MediatR;

namespace Buyer.Application.Features.Queries.CheckRFQUserAccess
{
    public class CheckRFQUserAccessQuery : IRequest<bool>
    {
        public Guid RFQId { get; set; }

        public Guid UserId { get; set; }
    }
}