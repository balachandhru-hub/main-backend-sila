using System.Net;
using System.Net.Mail;
using MasterData.Domain.Common;
using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace MasterData.Application.Features.Email.Commands;

public class SendEmailCommandHandler :
    IRequestHandler<SendEmailCommand>
{
    private readonly IRepositoryWrapper _repository;
    private readonly IConfiguration _configuration;
    private readonly ILoggerManager _logger;


    public SendEmailCommandHandler(
        IRepositoryWrapper repository,
        IConfiguration configuration,
        ILoggerManager logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }


    public async Task Handle(
        SendEmailCommand request,
        CancellationToken cancellationToken)
    {
        EmailContent? template = null;

        try
        {
            template =
    await _repository.EmailContent
    .FindFirstByConditionAsync(x =>
        x.IsActive &&
        x.Key == request.EmailKey);


            if (template == null)
            {
                _logger.LogError("Email template not found");
                throw new NotFoundCustomException(
                    "Email template not found",
                    "Email template configuration missing");
            }


            ApiConfig? apiConfig =
                await _repository.ApiConfig
                .FindFirstByConditionAsync(x =>
                    x.IsActive &&
                    x.Description ==
                    Common.EMAIL_API_CREDENTIAL_DESCRIPTION);


            if (apiConfig == null)
            {
                _logger.LogError("Email configuration missing");

                throw new NotFoundCustomException(
                    "Email configuration missing",
                    "Email API configuration not found");
            }


            string body =
    ReplacePlaceholders(
        template.Body!,
        request);


            string subject =
    ReplacePlaceholders(
        template.Subject!,
        request);



            using MailMessage message = new();


            message.From =
                new MailAddress(
                    apiConfig.Username!);


            message.To.Add(request.ToEmail);


            request.CcEmail?
    .Where(email => !string.IsNullOrWhiteSpace(email))
    .ToList()
    .ForEach(email =>
        message.CC.Add(
            new MailAddress(email)));


            message.Subject = subject;

            message.Body = body;

            message.IsBodyHtml = true;



            using SmtpClient smtp =
                new(
                    apiConfig.BaseUrl,
                    int.Parse(
                        _configuration["EmailSettings:Port"]!));


            smtp.EnableSsl = true;


            smtp.Credentials =
                new NetworkCredential(
                    apiConfig.Username,
                    apiConfig.Password);



            await smtp.SendMailAsync(message);
            _logger.LogInfo("Email sent successfully");
            EmailFailedDetail? failed =
    await _repository.EmailFailedDetail
    .FindFirstByConditionAsync(x =>
        x.IsActive &&
        x.EntityId == request.EntityId &&
        x.EmailType == request.EmailKey);


            if (failed != null)
            {
                failed.IsActive = false;

                _repository.EmailFailedDetail.Update(failed);

                await _repository.SaveAsync();
            }



            await CreateEmailSentDetails(request);


            
        }
        catch (BaseCustomException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Email failed : {ex.Message}");

            await CreateEmailFailedDetails(
                request,
                template);
            _logger.LogError($"Email failed : {ex.Message}");
            throw new InternalServerCustomException(
                "Email sending failed",
                ex.Message);
        }
    }



    private string ReplacePlaceholders(
    string input,
    SendEmailCommand request)
    {
        _logger.LogInfo("Replacing email placeholders");

        string content = input
            .Replace(Common.OTP_PLACEHOLDER,
                request.Parameters?.GetValueOrDefault("OTP") ?? "")
            .Replace(Common.OTP_VALIDITY_PLACEHOLDER,
                request.Parameters?.GetValueOrDefault("OTP_VALIDITY") ?? "")
            .Replace(Common.COMPANY_NAME_PLACEHOLDER,
                _configuration["EmailSettings:CompanyName"] ?? "")
            .Replace(Common.SUPPORT_EMAIL_PLACEHOLDER,
                _configuration["EmailSettings:SupportEmail"] ?? "");

        _logger.LogInfo("Replaced email placeholders");

        return content;
    }



    private async Task CreateEmailSentDetails(
        SendEmailCommand request)
    {
        await _repository.EmailSentDetail
            .CreateAsync(
                new EmailSentDetail
                {
                    Id = Guid.NewGuid(),
                    Email = request.ToEmail,
                    EmailType = request.EmailKey,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType
                });


        await _repository.SaveAsync();
    }



    private async Task CreateEmailFailedDetails(
        SendEmailCommand request,
        EmailContent? template)
    {
        EmailFailedDetail? existing =
            await _repository.EmailFailedDetail
            .FindFirstByConditionAsync(x =>
                x.IsActive &&
                x.EntityId == request.EntityId &&
                x.EmailType == request.EmailKey);


        if (existing != null)
        {
            existing.TriggerCount++;

            _repository.EmailFailedDetail
                .Update(existing);
        }
        else
        {
            await _repository.EmailFailedDetail
            .CreateAsync(
                new EmailFailedDetail
                {
                    Id = Guid.NewGuid(),
                    Email = request.ToEmail,
                    EmailType = request.EmailKey,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType,
                    Subject = template?.Subject,
                    Body = template?.Body,
                    TriggerCount = 1
                });
        }


        await _repository.SaveAsync();
    }
}