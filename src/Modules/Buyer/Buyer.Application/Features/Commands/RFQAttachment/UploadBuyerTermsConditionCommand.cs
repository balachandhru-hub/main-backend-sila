using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Commands.RFQAttachment
{
    public class UploadBuyerTermsConditionCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }

        public Guid BuyerId { get; set; }

        /// <summary>
        /// true: deactivate every existing active Buyer Terms and Condition
        /// attachment for this RFQ before adding this one, so only the
        /// newest stays active (combine into one).
        /// false: keep the existing ones active and just add this one.
        /// </summary>
        public bool IsSingletonAsset { get; set; }

        public AssetUploadDto Document { get; set; }
    }
}
