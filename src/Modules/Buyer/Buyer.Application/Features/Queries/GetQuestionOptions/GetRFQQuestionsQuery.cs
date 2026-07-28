
using Buyer.Domain.Dto;
using MediatR;
namespace Buyer.Application.Features.Queries.GetRFQQuestions
{
public class GetRFQQuestionsQuery : IRequest<List<RFQQuestionResponseDto>>
{
    public Guid RFQId { get; }

    public GetRFQQuestionsQuery(Guid rfqId)
    {
        RFQId = rfqId;
    }
}
}