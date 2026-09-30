namespace SharedKernel.Contracts
{
    /// <summary>
    /// Resolves and validates an external-session token (e.g. the one sent
    /// to an external supplier in an RFQ email link) against the RFQId it
    /// is being used for. Implemented per module, since only that module
    /// knows where its session tokens are stored.
    /// </summary>
    public interface ISessionTokenValidator
    {
        /// <summary>
        /// Validates <paramref name="sessionToken"/> for <paramref name="rfqId"/>.
        /// Implementations must throw the appropriate
        /// <c>SharedKernel.ExceptionHandler</c> exception (Unauthorized for a
        /// missing/invalid/mismatched token, Forbidden for an expired one)
        /// rather than returning null, so callers can rely on a non-null result.
        /// </summary>
        Task<SessionTokenValidationResult> ValidateAsync(
            string sessionToken,
            Guid rfqId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The identity resolved from a validated session token.
    /// </summary>
    public class SessionTokenValidationResult
    {
        public Guid RFQId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid SupplierRFQId { get; set; }
    }
}
