using Supplier.Domain.Dto;

namespace Supplier.Application.Contracts
{
    public interface IBuyerApiClient
    {
        Task<List<Guid>> GetVerifiedSuppliers(
            GetVerifiedSupplierRequestDto request,
            
            CancellationToken cancellationToken = default);
        Task<GetRFQAttachmentsDto> GetRFQAttachments(
    Guid rfqId,
    CancellationToken cancellationToken = default);
    Task<SupplierVerificationRequestDetailDto> GetSupplierVerificationRequestDetail(
    Guid requestId,
    CancellationToken cancellationToken = default);
    Task UpdateVerificationRequestStatus(
    Guid verificationRequestId,
    string status,
    CancellationToken cancellationToken = default);
    }
    
}