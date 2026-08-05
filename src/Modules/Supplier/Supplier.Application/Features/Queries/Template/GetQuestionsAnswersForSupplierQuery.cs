using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries
{
    public class GetQuestionsAnswersForSupplierQuery
        : IRequest<GetQuestionsAnswersForSupplierDto>
    {
        public Guid SupplierVerificationRequestId { get; set; }
    }
}