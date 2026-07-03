using Contracts.IRepository;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpCommandHandler
        : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
    {
        private readonly IRepositoryWrapper _repository;

        public VerifyOtpCommandHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<VerifyOtpResponse> Handle(
            VerifyOtpCommand request,
            CancellationToken cancellationToken)
        {
            var otp =  _repository.EmailVerification
                  .FindByConditionAsync(x => x.Email == request.Email &&x.IsActive).FirstOrDefault();

            if (otp == null)
            {
                return new VerifyOtpResponse
                {
                    Success = false,
                    Message = "OTP not found."
                };
            }

            // OTP does not match
            if (otp.OtpHash != request.Otp)
            {
                if (otp.ExpiresOn <= DateTime.UtcNow)
                {
                    otp.IsActive = false;

                    _repository.EmailVerification.Update(otp);
                    await _repository.SaveAsync();

                    return new VerifyOtpResponse
                    {
                        Success = false,
                        Message = "OTP has expired."
                    };
                }

                return new VerifyOtpResponse
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            // OTP matches but expired
            if (otp.ExpiresOn <= DateTime.UtcNow)
            {
                otp.IsActive = false;

                _repository.EmailVerification.Update(otp);
                await _repository.SaveAsync();

                return new VerifyOtpResponse
                {
                    Success = false,
                    Message = "OTP has expired."
                };
            }

            // OTP verified successfully
            otp.IsVerified = true;
            otp.TemporaryVerificationToken = Guid.NewGuid().ToString();
            otp.TemporaryVerificationTokenExpiresOn = DateTime.UtcNow.AddMinutes(30);

            _repository.EmailVerification.Update(otp);

            await _repository.SaveAsync();

            return new VerifyOtpResponse
            {
                Success = true,
                Message = "OTP verified successfully.",
                TemporaryVerificationToken = otp.TemporaryVerificationToken
            };
        }
    }
}