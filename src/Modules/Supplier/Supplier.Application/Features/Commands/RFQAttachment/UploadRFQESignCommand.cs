using MediatR;
using SharedKernel.Dto;

namespace Supplier.Application.Features.Commands.RFQAttachment
{
    public class UploadRFQESignCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }
        public AssetUploadDto Document { get; set; }
    }
}
