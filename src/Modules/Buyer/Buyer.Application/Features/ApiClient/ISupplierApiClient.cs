using Buyer.Domain.Dto;

namespace Buyer.Application.Contracts
{
    public interface ISupplierApiClient
    {
        Task<bool> ValidateExternalSessionToken(
            string sessionToken,
            Guid rfqId,
            CancellationToken cancellationToken = default);
        Task CreateSupplierRFQ(
            CreateSupplierRFQRequestDto rfq,

            CancellationToken cancellationToken = default);
        Task<GetAllSupplierQuotationDto> GetSupplierQuotation(
Guid RFQId,
CancellationToken cancellationToken = default);
        Task<SupplierProfileDto> GetSupplierById(
        Guid supplierId,
        CancellationToken cancellationToken = default);
        Task<List<SupplierNameDto>> GetSupplierNamesByIds(
        List<Guid> supplierIds,
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
        Task<BidCompareResponseDto> GetBidCompare(
            Guid rfqId,
            CancellationToken cancellationToken = default);
        Task NotifyNewMessage(
            MessageResponseDto message,
            CancellationToken cancellationToken = default);
    }
}