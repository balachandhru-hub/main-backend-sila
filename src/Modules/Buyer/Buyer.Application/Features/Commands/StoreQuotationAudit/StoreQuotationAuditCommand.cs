using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.StoreQuotationAudit
{
    public class StoreQuotationAuditCommand : IRequest
    {
        public QuotationAuditRequestDto Request { get; }

        public StoreQuotationAuditCommand(QuotationAuditRequestDto request)
        {
            Request = request;
        }
    }
}