using MediatR;

namespace Buyer.Application.Features.Queries.GetExternalSupplierByEmail
{
    /// <summary>
    /// Resolves the ExternalSupplierId (if any) for an email address, so a
    /// registering supplier can reuse the same id as their
    /// SupplierBusinessProfile.Id instead of generating a new one.
    /// </summary>
    public class GetExternalSupplierByEmailQuery : IRequest<Guid?>
    {
        public string Email { get; }

        public GetExternalSupplierByEmailQuery(string email)
        {
            Email = email;
        }
    }
}
