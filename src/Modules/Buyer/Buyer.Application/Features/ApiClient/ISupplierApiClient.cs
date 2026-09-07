using Buyer.Domain.Dto;

namespace Buyer.Application.Contracts
{
    public interface ISupplierApiClient
    {
        Task CreateSupplierRFQ(
            CreateSupplierRFQRequestDto rfq,

            CancellationToken cancellationToken = default);
        Task<GetAllSupplierQuotationDto> GetSupplierQuotation(
Guid RFQId,
CancellationToken cancellationToken = default);
        Task<SupplierProfileDto> GetSupplierById(
        Guid supplierId,
        CancellationToken cancellationToken = default);
        Task<GetQuestionsAnswersForSupplierDto> GetQuestionsAnswersForSupplier(
        Guid requestId,
        CancellationToken cancellationToken = default);
        Task<SupplierRFQAnswerDto?> GetSupplierRFQAnswers(
         Guid buyerRFQId,
         CancellationToken cancellationToken);
        Task<Guid> GetSupplierId(
    CancellationToken cancellationToken = default);
    Task UpdateSupplierRFQStatus(Guid rfqId,string status,CancellationToken cancellationToken = default);
    }
}