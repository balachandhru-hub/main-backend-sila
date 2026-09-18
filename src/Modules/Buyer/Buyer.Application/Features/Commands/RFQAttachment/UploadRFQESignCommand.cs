using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Commands.RFQAttachment
{
    public class UploadRFQESignCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public AssetUploadDto Document { get; set; }
    }
}
