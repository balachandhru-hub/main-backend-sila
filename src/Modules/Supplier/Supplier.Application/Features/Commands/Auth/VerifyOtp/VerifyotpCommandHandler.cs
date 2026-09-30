using MediatR;
using HashingSystem;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Application.Contracts;

namespace Supplier.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpCommandHandler
        : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly ILoggerManager _logger;
        private readonly IIdentityApiClient _identityApiClient;

        public VerifyOtpCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing,
            ILoggerManager logger,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _hashing = hashing;
            _logger = logger;
            _identityApiClient = identityApiClient;
        }

        public async Task<VerifyOtpResponse> Handle(
            VerifyOtpCommand request,
            CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                _logger.LogError("OTP verification failed: UserId is required.");

                throw new BadRequestCustomException(
                    "User is required.",
                    "Please provide a valid authenticated user.");
            }

            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                _logger.LogError(
                    $"OTP verification failed: OTP is required for user {request.UserId}.");

                throw new BadRequestCustomException(
                    "OTP is required.",
                    "Please enter the OTP.");
            }

            var identityUsers = await _identityApiClient.GetUsersByIds(
                new List<Guid> { request.UserId },
                cancellationToken);

            var email = identityUsers.FirstOrDefault(u => u.UserId == request.UserId)?.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError($"OTP verification failed: User email not found for UserId {request.UserId}.");

                throw new BadRequestCustomException(
                    "User email not found.",
                    "User email is not available.");
            }

            _logger.LogInfo(
                $"Verifying OTP for email: {email}");

            var otp = await _repository.SupplierEmailVerification
                .FindByCondition(x =>
                    x.Email == email &&
                    x.IsActive &&
                    !x.IsVerified)

                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
            {
                _logger.LogError(
                    $"OTP verification failed: OTP not found for email {email}.");

                throw new BadRequestCustomException(
                    "OTP not found.",
                    "Please request a new OTP.");
            }

            // Check expiry
            if (otp.ExpiresOn <= DateTime.UtcNow)
            {
                _logger.LogError(
                    $"OTP verification failed: OTP expired for email {email}.");

                otp.IsActive = false;

                _repository.SupplierEmailVerification.Update(otp);
                await _repository.SaveAsync();

                throw new BadRequestCustomException(
                    "OTP has expired.",
                    "Please request a new OTP.");
            }

            // Check OTP
            if (!_hashing.VerifyHash(request.Otp, otp.OtpHash))
            {
                otp.AttemptCount++;

                _repository.SupplierEmailVerification.Update(otp);
                await _repository.SaveAsync();

                _logger.LogError(
                    $"OTP verification failed: Invalid OTP for email {email}. " +
                    $"Attempt count: {otp.AttemptCount}");

                throw new BadRequestCustomException(
                    "Invalid OTP.",
                    "The OTP entered is incorrect.");
            }

            // OTP verified successfully
            otp.IsVerified = true;
            otp.AttemptCount++;

            otp.TemporaryVerificationToken =
                Guid.NewGuid().ToString();

            otp.TemporaryVerificationTokenExpiresOn =
                DateTime.UtcNow.AddMinutes(30);

            _repository.SupplierEmailVerification.Update(otp);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"OTP verified successfully for email: {email}. " +
                $"Verification token generated.");

            return new VerifyOtpResponse
            {
                Success = true,
                Message = "OTP verified successfully.",
                TemporaryVerificationToken =
                    otp.TemporaryVerificationToken
            };
        }
    }
}