

using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.CreateSupplierRFQ
{
   
public class CreateSupplierRFQCommand : IRequest<Guid>
{
    public CreateSupplierRFQDto RFQ { get; }

    public CreateSupplierRFQCommand(CreateSupplierRFQDto rfq)
    {
        RFQ = rfq;
    }
}
}