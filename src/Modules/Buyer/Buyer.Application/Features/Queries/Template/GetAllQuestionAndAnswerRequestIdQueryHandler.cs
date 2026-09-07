using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailbyRequestIdQueryHandler
        : IRequestHandler<
            GetSupplierVerificationRequestDetailbyRequestIdQuery,
            SupplierVerificationRequestDetailQuestinandAnswerDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public GetSupplierVerificationRequestDetailbyRequestIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<SupplierVerificationRequestDetailQuestinandAnswerDto> Handle(
            GetSupplierVerificationRequestDetailbyRequestIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Supplier Verification Request : {request.RequestId}");



            var result = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.Id == request.RequestId &&
                    x.IsActive)
                .Select(x => new SupplierVerificationRequestDetailQuestinandAnswerDto
                {
                    RequestId = x.Id,
                    RFQId = x.RFQId,
                    RFQNumber = x.RFQNumber,
                    BuyerId = x.BuyerOrganizationId,
                    SupplierOrganizationId = x.SupplierOrganizationId,
                    TemplateId = x.RFQVerificationTemplateId,
                    Status = x.Status,
                    Remarks = x.Remarks,
                    DueDate = x.DueDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogError(
                    $"Supplier Verification Request not found. " +
                    $"RequestId : {request.RequestId}");

                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Request Id.");
            }



            if (request.RoleId == Common.BUYER_ADMIN_ROLE_ID)
            {
                var supplier = await _supplierApiClient.GetSupplierById(
                    result.SupplierOrganizationId,
                    cancellationToken);

                if (supplier != null)
                {
                    result.SNID =
                        supplier.BusinessProfile?.SNID;

                    result.OrganizationName =
                        supplier.BusinessProfile?.OrganizationName;

                    result.Description =
                        supplier.BusinessProfile?.Description;
                }
            }
            else if (request.RoleId == Common.SUPPLIER_ADMIN_ROLE_ID)
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x =>
                        x.OrganizationId == result.BuyerId)
                    .Select(x => new
                    {
                        x.SNID,
                        x.OrganizationName,
                        x.Description
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer != null)
                {
                    result.SNID = buyer.SNID;
                    result.OrganizationName = buyer.OrganizationName;
                    result.Description = buyer.Description;
                }
            }


            // =========================================================

            var questions = await _repository.VerificationTemplateQuestion
                .FindByCondition(x =>
                    x.VerificationTemplateId == result.TemplateId &&
                    x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            result.Questions =
                new List<SupplierVerificationQuestionAndAnswerDto>();



            if (questions.Any())
            {
                foreach (var question in questions)
                {
                    var questionDto =
                        new SupplierVerificationQuestionAndAnswerDto
                        {
                            VerificationTemplateQuestionId = question.Id,
                            Question = question.Question,
                            QuestionType = question.QuestionType,
                            IsRequired = question.IsRequired,
                            DisplayOrder = question.DisplayOrder,

                            Answer = null,
                            AssetId = null,
                            VerificationTemplateQuestionOptionId = null
                        };

                    questionDto.Options = await _repository
                        .VerificationTemplateQuestionOptionRepository
                        .FindByCondition(x =>
                            x.VerificationTemplateQuestionId == question.Id)
                        .OrderBy(x => x.DisplayOrder)
                        .Select(x =>
                            new SupplierVerificationQuestionOptionDto
                            {
                                Id = x.Id,
                                OptionText = x.OptionText,
                                DisplayOrder = x.DisplayOrder
                            })
                        .ToListAsync(cancellationToken);

                    result.Questions.Add(questionDto);
                }
            }
            else
            {


                _logger.LogInfo(
                    $"No custom template questions found for TemplateId : " +
                    $"{result.TemplateId}. Checking default questions.");

                var defaultQuestions =
                    await _repository
                        .DefaultVerificationTemplateQuestionRepository
                        .FindByCondition(x =>
                            x.DefaultVerificationTemplateId == result.TemplateId &&
                            x.IsActive)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync(cancellationToken);

                foreach (var question in defaultQuestions)
                {
                    var questionDto =
                        new SupplierVerificationQuestionAndAnswerDto
                        {
                            VerificationTemplateQuestionId = question.Id,
                            Question = question.Question,
                            QuestionType = question.QuestionType,
                            IsRequired = false,
                            DisplayOrder = question.DisplayOrder,

                            Answer = null,
                            AssetId = null,
                            VerificationTemplateQuestionOptionId = null,

                            Options =
                                new List<SupplierVerificationQuestionOptionDto>()
                        };

                    result.Questions.Add(questionDto);
                }

                _logger.LogInfo(
                    $"Default template questions fetched successfully. " +
                    $"TemplateId : {result.TemplateId}, " +
                    $"QuestionCount : {defaultQuestions.Count}");
            }


            bool showAnswers = false;

            if (request.RoleId == Common.SUPPLIER_ADMIN_ROLE_ID)
            {
                showAnswers =
                    result.Status.Equals(
                        Common.SUBMITTED,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    result.Status.Equals(
                        Common.DEFAULT,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    result.Status.Equals(
                        Common.DRAFT,
                        StringComparison.OrdinalIgnoreCase);
            }
            else if (request.RoleId == Common.BUYER_ADMIN_ROLE_ID)
            {
                showAnswers =
                    !result.Status.Equals(
                        Common.DRAFT,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !result.Status.Equals(
                        Common.PENDING,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !result.Status.Equals(
                        Common.DEFAULT,
                        StringComparison.OrdinalIgnoreCase);
            }



            if (showAnswers)
            {
                try
                {


                    if (result.TemplateId ==
                        Common.DEFAULT_VERIFICATION_TEMPLATE_ID)
                    {
                        _logger.LogInfo(
                            $"Default template detected. " +
                            $"Fetching supplier profile using SupplierId : " +
                            $"{result.SupplierOrganizationId}");

                        SupplierProfileDto? supplier =
                            await _supplierApiClient.GetSupplierById(
                                result.SupplierOrganizationId,
                                cancellationToken);

                        if (supplier != null)
                        {
                            foreach (var question in result.Questions)
                            {
                                SetDefaultQuestionAnswer(
                                    question,
                                    supplier);
                            }
                        }
                        else
                        {
                            _logger.LogInfo(
                                $"Supplier not found. " +
                                $"SupplierId : {result.SupplierOrganizationId}");
                        }
                    }

                    else
                    {
                        _logger.LogInfo(
                            $"Custom template detected. " +
                            $"Fetching supplier answers for RequestId : " +
                            $"{result.RequestId}");

                        var supplierAnswers =
                            await _supplierApiClient
                                .GetQuestionsAnswersForSupplier(
                                    result.RequestId,
                                    cancellationToken);

                        if (supplierAnswers?.Questions != null)
                        {
                            foreach (var question in result.Questions)
                            {
                                var answer =
                                    supplierAnswers.Questions
                                        .FirstOrDefault(x =>
                                            x.VerificationTemplateQuestionId ==
                                            question.VerificationTemplateQuestionId);

                                if (answer != null)
                                {
                                    question.Answer =
                                        answer.Answer;

                                    question.AssetId =
                                        answer.AssetId;

                                    question.VerificationTemplateQuestionOptionId =
                                        answer.VerificationTemplateQuestionOptionId;
                                }
                            }
                        }
                    }
                }
                catch (BadRequestCustomException)
                {
                    _logger.LogInfo(
                        $"No supplier answers available for RequestId : " +
                        $"{result.RequestId}. Returning questions only.");
                }
            }


            _logger.LogInfo(
                $"Supplier Verification Request fetched successfully : " +
                $"{request.RequestId}");

            return result;
        }


        private static void SetDefaultQuestionAnswer(
            SupplierVerificationQuestionAndAnswerDto question,
            SupplierProfileDto supplier)
        {
            if (question == null || supplier == null)
                return;

            var questionName =
                question.Question?.Trim();

            if (string.IsNullOrWhiteSpace(questionName))
                return;

            var businessProfile =
                supplier.BusinessProfile;


            var bankAccount =
                supplier.BankAccounts?
                    .FirstOrDefault(x => x.IsPrimary)
                ??
                supplier.BankAccounts?.FirstOrDefault();



            var gstRegistration =
                supplier.Registrations?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.RegistrationType) &&
                        (
                            x.RegistrationType.Trim()
                                .Equals(
                                    "GST",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "GSTIN",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "GST Number",
                                    StringComparison.OrdinalIgnoreCase)
                        ));

            var panRegistration =
                supplier.Registrations?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.RegistrationType) &&
                        (
                            x.RegistrationType.Trim()
                                .Equals(
                                    "PAN",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "PAN Number",
                                    StringComparison.OrdinalIgnoreCase)
                        ));

            var companyRegistration =
                supplier.Registrations?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.RegistrationType) &&
                        (
                            x.RegistrationType.Trim()
                                .Equals(
                                    "CIN",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "Company Registration",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "Company Registration Number",
                                    StringComparison.OrdinalIgnoreCase)
                        ));

            var msmeRegistration =
                supplier.Registrations?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.RegistrationType) &&
                        (
                            x.RegistrationType.Trim()
                                .Equals(
                                    "MSME",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "MSME Certificate",
                                    StringComparison.OrdinalIgnoreCase)
                        ));

            var isoRegistration =
                supplier.Registrations?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.RegistrationType) &&
                        (
                            x.RegistrationType.Trim()
                                .Equals(
                                    "ISO",
                                    StringComparison.OrdinalIgnoreCase)
                            ||
                            x.RegistrationType.Trim()
                                .Equals(
                                    "ISO Certificate",
                                    StringComparison.OrdinalIgnoreCase)
                        ));



            switch (questionName.ToLowerInvariant())
            {


                case "company name":

                    question.Answer =
                        businessProfile?.OrganizationName;

                    break;

                case "company registration number":

                    question.Answer =
                        companyRegistration?.RegistrationNumber;

                    if (companyRegistration?.Asset != null)
                    {
                        question.AssetId =
                            companyRegistration.Asset.Id;
                    }

                    break;



                case "gst number":

                    question.Answer =
                        gstRegistration?.RegistrationNumber;

                    if (gstRegistration?.Asset != null)
                    {
                        question.AssetId =
                            gstRegistration.Asset.Id;
                    }

                    break;

                case "pan number":

                    question.Answer =
                        panRegistration?.RegistrationNumber;

                    if (panRegistration?.Asset != null)
                    {
                        question.AssetId =
                            panRegistration.Asset.Id;
                    }

                    break;



                case "bank name":

                    question.Answer =
                        bankAccount?.BankName;

                    break;

                case "account number":

                    question.Answer =
                        bankAccount?.AccountNumber;

                    break;

                case "ifsc code":

                    question.Answer =
                        bankAccount?.IFSCCode;

                    break;

                case "bank branch":

                    question.Answer =
                        bankAccount?.BranchName;

                    break;



                case "email":

                    question.Answer =
                        businessProfile?.Email;

                    break;

                case "phone number":

                    question.Answer =
                        businessProfile?.Phone;

                    break;


                case "country":

                    question.Answer =
                        businessProfile?.Country;

                    break;

                case "state":

                    question.Answer =
                        businessProfile?.State;

                    break;

                case "city":

                    question.Answer =
                        businessProfile?.City;

                    break;

                case "address":

                    question.Answer =
                        BuildAddress(businessProfile);

                    break;



                case "msme certificate":

                    if (msmeRegistration?.Asset != null)
                    {
                        question.AssetId =
                            msmeRegistration.Asset.Id;
                    }

                    break;

                case "iso certificate":

                    if (isoRegistration?.Asset != null)
                    {
                        question.AssetId =
                            isoRegistration.Asset.Id;
                    }

                    break;

                default:

                    break;
            }
        }



        private static string? BuildAddress(
            SupplierBusinessProfileDto? businessProfile)
        {
            if (businessProfile == null)
                return null;

            var addressParts =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                businessProfile.AddressLine1))
            {
                addressParts.Add(
                    businessProfile.AddressLine1.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                businessProfile.AddressLine2))
            {
                addressParts.Add(
                    businessProfile.AddressLine2.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                businessProfile.City))
            {
                addressParts.Add(
                    businessProfile.City.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                businessProfile.State))
            {
                addressParts.Add(
                    businessProfile.State.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                businessProfile.Country))
            {
                addressParts.Add(
                    businessProfile.Country.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                businessProfile.PinCode))
            {
                addressParts.Add(
                    businessProfile.PinCode.Trim());
            }

            return addressParts.Count > 0
                ? string.Join(", ", addressParts)
                : null;
        }
    }
}