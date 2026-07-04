using System.Net;
using System.Net.Mail;
using MasterData.Domain.Common;
using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Email.Commands;

public class SendEmailCommandHandler :
    IRequestHandler<SendEmailCommand, bool>
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


    public async Task<bool> Handle(
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
                return false;
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
                return false;
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


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Email failed : {ex.Message}");


            await CreateEmailFailedDetails(
                request,
                template);


            return false;
        }
    }



    // private string ReplacePlaceholders(
    // string input,
    // SendEmailCommand request)
    // {
    //     _logger.LogInfo("Replacing email placeholders");

    //     string content = input
    //         .Replace("{OTP}", request.Otp ?? "")
    //         .Replace("{OTP_VALIDITY}", request.OtpValidity ?? "")
    //         .Replace("{COMPANY_NAME}",
    //             _configuration["EmailSettings:CompanyName"] ?? "")
    //         .Replace("{SUPPORT_EMAIL}",
    //             _configuration["EmailSettings:SupportEmail"] ?? "");

    //     _logger.LogInfo("Replaced email placeholders");

    //     return content;
    // }

    private string ReplacePlaceholders(
    string input,
    SendEmailCommand request)
    {
        _logger.LogInfo("Replacing email placeholders");

        request.Parameters?
            .ToList()
            .ForEach(x =>
                input = input.Replace(
                    "{" + x.Key + "}",
                    x.Value));

        input = input
            .Replace("{COMPANY_NAME}",
                _configuration["EmailSettings:CompanyName"] ?? "")
            .Replace("{SUPPORT_EMAIL}",
                _configuration["EmailSettings:SupportEmail"] ?? "");

        return input;
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