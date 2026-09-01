using MediatR;
using HashingSystem;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpCommandHandler
        : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly ILoggerManager _logger;

        public VerifyOtpCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing,
            ILoggerManager logger)
        {
            _repository = repository;
            _hashing = hashing;
            _logger = logger;
        }

        public async Task<VerifyOtpResponse> Handle(
            VerifyOtpCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogError("OTP verification failed: Email is required.");

                throw new BadRequestCustomException(
                    "Email is required.",
                    "Please provide the email address.");
            }

            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                _logger.LogError(
                    $"OTP verification failed: OTP is required for email {request.Email}.");

                throw new BadRequestCustomException(
                    "OTP is required.",
                    "Please enter the OTP.");
            }

            _logger.LogInfo(
                $"Verifying OTP for email: {request.Email}");

            var otp = await _repository.SupplierEmailVerification
                .FindByCondition(x =>
                    x.Email == request.Email &&
                    x.IsActive &&
                    !x.IsVerified)

                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
            {
                _logger.LogError(
                    $"OTP verification failed: OTP not found for email {request.Email}.");

                throw new BadRequestCustomException(
                    "OTP not found.",
                    "Please request a new OTP.");
            }

            // Check expiry
            if (otp.ExpiresOn <= DateTime.UtcNow)
            {
                _logger.LogError(
                    $"OTP verification failed: OTP expired for email {request.Email}.");

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
                    $"OTP verification failed: Invalid OTP for email {request.Email}. " +
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
                $"OTP verified successfully for email: {request.Email}. " +
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