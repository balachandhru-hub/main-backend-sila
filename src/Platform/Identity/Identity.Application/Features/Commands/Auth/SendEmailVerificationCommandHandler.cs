using MediatR;
using Contracts.IRepository;
using Identity.Domain.Entities;

namespace Identity.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand , bool>
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
        x.Email == request.Email &&
        x.IsActive)
    .FirstOrDefault();

if (existingOtp != null)
{
    // OTP expired
    if (existingOtp.TemporaryVerificationTokenExpiresOn  <= DateTime.UtcNow)
    {
        existingOtp.IsActive = false;
        _repository.EmailVerification.Update(existingOtp);
        await _repository.SaveAsync();
    }
    else
    {
        // Active OTP still valid
        return true;
    }
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