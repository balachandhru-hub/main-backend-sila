using SharedKernel.Dto;

namespace Buyer.Domain.Dtos
{
    public class UploadItemBuyerMasterDto
    {
        /// <summary>
        /// The uploaded .xlsx file, sent the same way every other document
        /// upload in this app sends one - the client fills EntityId,
        /// EntityType, AssetType, FileBytes, FileName, ContentType and
        /// IsSingletonAsset directly on this, exactly like UploadRFQESign
        /// etc. do with their own Document.
        /// </summary>
        public AssetUploadDto Document { get; set; } = default!;

        public Guid OrganizationId { get; set; }

        public Guid? BuyerId { get; set; }

        /// <summary>Title for this Excel batch (e.g. "Q4 materials upload"), shown in Get Pending Approvals.</summary>
        public string? Title { get; set; }

        /// <summary>
        /// Approval flow to route this WHOLE Excel batch through - one
        /// approval workflow for the entire file, not one per row.
        /// </summary>
        public Guid ApprovalFlowId { get; set; }

        public string? Comment { get; set; }
    }
}
