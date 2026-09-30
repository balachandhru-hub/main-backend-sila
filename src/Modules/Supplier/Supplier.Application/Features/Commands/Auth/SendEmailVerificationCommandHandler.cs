using MediatR;
using Supplier.Domain.Entities;
using SharedKernel.ExceptionHandler;
using HashingSystem;
using Supplier.Domain.Dto;
using Microsoft.Extensions.Configuration;
using Supplier.Domain.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Supplier.Infrastructure.Contracts.IRepository;
using Microsoft.Extensions.Logging;
using SharedKernel.LoggerServices;
using Supplier.Application.Contracts;


namespace Supplier.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, SendEmailVerificationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly ILoggerManager _logger;
        private readonly IIdentityApiClient _identityApiClient;

        public SendEmailVerificationCommandHandler(IRepositoryWrapper repository, IBcryptHashing hashing, IConfiguration configuration, HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILoggerManager logger, IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _hashing = hashing;
            _configuration = configuration;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _identityApiClient = identityApiClient;
        }

        public async Task<SendEmailVerificationResponseDto> Handle(
            SendEmailVerificationCommand request,
            CancellationToken cancellationToken)
        {
            string? ipAddress = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Real-IP"].FirstOrDefault() ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4()?.ToString();
            var supplierProfile = await _repository.SupplierBusinessProfile
            .FindByCondition(x =>
                x.OrganizationId == request.OrganizationId &&
                x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

            if (supplierProfile == null)
            {
                _logger.LogError($"Supplier profile not found for OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier business profile was not found.");
            }

            var identityUsers = await _identityApiClient.GetUsersByIds(
                new List<Guid> { request.UserId },
                cancellationToken);

            var Email = identityUsers.FirstOrDefault(u => u.UserId == request.UserId)?.Email;

            if (string.IsNullOrWhiteSpace(Email))
            {
                _logger.LogError($"User email not found for UserId: {request.UserId}");
                throw new BadRequestCustomException(
                    "User email not found.",
                    "User email is not available.");
            }
            var existingOtp = _repository.SupplierEmailVerification
      .FindByConditionAsync(x =>
          x.Email == Email && x.IsVerified == false &&
          x.IsActive)
      .FirstOrDefault();

            if (existingOtp != null)
            {

                if (existingOtp.ExpiresOn >= DateTime.UtcNow)
                {
                    throw new BadRequestCustomException(
                "An OTP has already been sent. Please verify your OTP and complete your registration.", "An OTP has already been sent for this email address.");
                }

                string otps = Random.Shared.Next(100000, 1000000).ToString();

                existingOtp.OtpHash = _hashing.HashStringWithSalt(otps);

                existingOtp.ExpiresOn = DateTime.UtcNow.AddMinutes(10);
                existingOtp.AttemptCount = 0;
                existingOtp.IsVerified = false;
                existingOtp.IpAddress = ipAddress;
                existingOtp.TemporaryVerificationToken = null;
                existingOtp.TemporaryVerificationTokenExpiresOn = null;


                _repository.SupplierEmailVerification.Update(existingOtp);
                await _repository.SaveAsync();
                await SendOtpEmailAsync(Email, otps, 10);

                return new SendEmailVerificationResponseDto
                {
                    Success = true,
                    Otp = otps,
                    ValidityMinutes = 10
                };
            }
            // Generate 6-digit OTP
            string otp = Random.Shared.Next(100000, 1000000).ToString();

            var emailVerification = new SupplierEmailVerification
            {
                Id = Guid.NewGuid(),
                Email = Email,

                ExpiresOn = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 0,
                IsVerified = false,
                IpAddress = ipAddress,
                TemporaryVerificationToken = null,
                TemporaryVerificationTokenExpiresOn = null
            };
            emailVerification.OtpHash = _hashing.HashStringWithSalt(otp);


            await _repository.SupplierEmailVerification.CreateAsync(emailVerification);
            await _repository.SaveAsync();
            await SendOtpEmailAsync(Email, otp, 10);

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
                _logger.LogError($"Failed to send OTP email to {email}. " + $"Status: {(int)response.StatusCode}, " + $"Response: {responseBody}");
                throw new FailedDependencyCustomException(
                    "Failed to send OTP email.",
                    $"Email API failed. Status: {(int)response.StatusCode}, Response: {responseBody}");
            }
        }
    }
}