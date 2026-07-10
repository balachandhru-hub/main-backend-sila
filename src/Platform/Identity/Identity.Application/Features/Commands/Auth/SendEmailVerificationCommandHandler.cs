using MediatR;
using Contracts.IRepository;
using Identity.Domain.Entities;
using SharedKernel.ExceptionHandler;
using HashingSystem;
using Identity.Domain.Dto;
using Microsoft.Extensions.Configuration;
using Identity.Domain.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;


namespace Identity.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, SendEmailVerificationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public SendEmailVerificationCommandHandler(IRepositoryWrapper repository, IBcryptHashing hashing, IConfiguration configuration, HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _hashing = hashing;
            _configuration = configuration;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SendEmailVerificationResponseDto> Handle(
            SendEmailVerificationCommand request,
            CancellationToken cancellationToken)
        {
            string? ipAddress = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Real-IP"].FirstOrDefault() ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4()?.ToString();
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

                existingOtp.OtpHash = _hashing.HashStringWithSalt(otps);

                existingOtp.ExpiresOn = DateTime.UtcNow.AddMinutes(10);
                existingOtp.AttemptCount = 0;
                existingOtp.IsVerified = false;
                existingOtp.IpAddress = ipAddress;
                existingOtp.TemporaryVerificationToken = null;
                existingOtp.TemporaryVerificationTokenExpiresOn = null;


                _repository.EmailVerification.Update(existingOtp);
                await _repository.SaveAsync();
                await SendOtpEmailAsync(request.Email, otps, 10);

                return new SendEmailVerificationResponseDto
                {
                    Success = true,
                    Otp = otps,
                    ValidityMinutes = 10
                };
            }
            // Generate 6-digit OTP
            string otp = Random.Shared.Next(100000, 1000000).ToString();

            var emailVerification = new EmailVerification
            {
                Id = Guid.NewGuid(),
                Email = request.Email,

                ExpiresOn = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 0,
                IsVerified = false,
                IpAddress = ipAddress,
                TemporaryVerificationToken = null,
                TemporaryVerificationTokenExpiresOn = null
            };
            emailVerification.OtpHash = _hashing.HashStringWithSalt(otp);


            await _repository.EmailVerification.CreateAsync(emailVerification);
            await _repository.SaveAsync();
            await SendOtpEmailAsync(request.Email, otp, 10);

            return new SendEmailVerificationResponseDto
            {
                Success = true,
                Otp = otp,
                ValidityMinutes = 10
            };
        }
        private async Task SendOtpEmailAsync(string email, string otp, int validityMinutes)
        {
            string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

            var response = await _httpClient.PostAsJsonAsync(
                $"{masterDataUrl}/api/v1/masterdata/email/send",
                new
                {
                    ToEmail = email,
                    EmailKey = Common.EMAIL_VERIFICATION,
                    Parameters = new Dictionary<string, string>
                    {
                { Common.EMAIL_OTP, otp },
                { Common.EMAIL_OTP_VALIDITY, $"{validityMinutes} minutes" }
                    }
                });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Email API failed. Status: {(int)response.StatusCode}, Response: {responseBody}");
            }
        }
    }
}