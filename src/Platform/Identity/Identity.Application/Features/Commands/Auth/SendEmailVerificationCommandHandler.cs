using MediatR;
using Contracts.IRepository;
using Identity.Domain.Entities;
using SharedKernel.ExceptionHandler;

namespace Identity.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public SendEmailVerificationCommandHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            SendEmailVerificationCommand request,
            CancellationToken cancellationToken)
        {
            var existingOtp = _repository.EmailVerification
      .FindByConditionAsync(x =>
          x.Email == request.Email && x.IsVerified == false &&
          x.IsActive)
      .FirstOrDefault();

            if (existingOtp != null)
            {
                // Active OTP is still valid
                if (existingOtp.ExpiresOn >= DateTime.UtcNow)
                {
                    throw new BadRequestCustomException(
                "An OTP has already been sent. Please verify your OTP and complete your registration.", "An OTP has already been sent for this email address.");
                }

                // OTP expired - generate a new OTP and update the existing record
                string otps = Random.Shared.Next(100000, 1000000).ToString();

                existingOtp.OtpHash = otps;
                existingOtp.ExpiresOn = DateTime.UtcNow.AddMinutes(10);
                existingOtp.AttemptCount = 0;
                existingOtp.IsVerified = false;
                existingOtp.IpAddress = request.IpAddress;
                existingOtp.TemporaryVerificationToken = null;
                existingOtp.TemporaryVerificationTokenExpiresOn = null;


                _repository.EmailVerification.Update(existingOtp);

                return await _repository.SaveAsync();
            }
            // Generate 6-digit OTP
            string otp = Random.Shared.Next(100000, 1000000).ToString();

            var emailVerification = new EmailVerification
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                OtpHash = otp,   // Later replace with hashed OTP
                ExpiresOn = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 0,
                IsVerified = false,
                IpAddress = request.IpAddress,
                TemporaryVerificationToken = null,
                TemporaryVerificationTokenExpiresOn = null
            };

            await _repository.EmailVerification.CreateAsync(emailVerification);

            return await _repository.SaveAsync();
        }
    }
}