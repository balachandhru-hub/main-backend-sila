using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.DefaultTemplate
{
    public class GetDefaultVerificationTemplateQueryHandler
        : IRequestHandler<GetDefaultVerificationTemplateQuery, GetDefaultVerificationTemplateDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;

        public GetDefaultVerificationTemplateQueryHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<GetDefaultVerificationTemplateDto> Handle(
            GetDefaultVerificationTemplateQuery request,
            CancellationToken cancellationToken)
        {
            var template = await _repository.DefaultVerificationTemplateRepository
                .FindByCondition(x => x.TemplateCode == "DT001")
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                throw new NotFoundCustomException(
                    "Default template not found.",
                    "Default verification template is not available.");
            }

            var questions = await _repository.DefaultVerificationTemplateQuestionRepository
                .FindByCondition(x => x.DefaultVerificationTemplateId == template.Id)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            // Get Supplier Details
            var supplier = await _supplierApiClient.GetSupplierById(
                request.SupplierId,
                cancellationToken);

            var questionDtos = questions.Select(question =>
 {
     string? answer = string.Empty;

     switch (question.QuestionKey)
     {
         // Business Profile
         case "OrganizationName":
             answer = supplier.BusinessProfile?.OrganizationName;
             break;

         case "Email":
             answer = supplier.BusinessProfile?.Email;
             break;

         case "Phone":
             answer = supplier.BusinessProfile?.Phone;
             break;

         case "Country":
             answer = supplier.BusinessProfile?.Country;
             break;

         case "State":
             answer = supplier.BusinessProfile?.State;
             break;

         case "City":
             answer = supplier.BusinessProfile?.City;
             break;

         case "Address":
             answer = $"{supplier.BusinessProfile?.AddressLine1} {supplier.BusinessProfile?.AddressLine2}".Trim();
             break;

         case "PinCode":
             answer = supplier.BusinessProfile?.PinCode;
             break;

         case "Industry":
             answer = supplier.BusinessProfile?.Industry;
             break;

         case "BusinessType":
             answer = supplier.BusinessProfile?.BusinessType;
             break;

         case "EmployeeCount":
             answer = supplier.BusinessProfile?.EmployeeCount?.ToString();
             break;

         case "AnnualTurnover":
             answer = supplier.BusinessProfile?.AnnualTurnover?.ToString();
             break;

         case "Currency":
             answer = supplier.BusinessProfile?.Currency;
             break;

         case "YearEstablished":
             answer = supplier.BusinessProfile?.YearEstablished?.ToString();
             break;

         case "Website":
             answer = supplier.BusinessProfile?.Website;
             break;

         case "Description":
             answer = supplier.BusinessProfile?.Description;
             break;

         // Registration Details
         case "RegistrationNumber":
             answer = supplier.Registrations.FirstOrDefault()?.RegistrationNumber;
             break;

         case "RegistrationName":
             answer = supplier.Registrations.FirstOrDefault()?.RegistrationName;
             break;

         case "RegistrationType":
             answer = supplier.Registrations.FirstOrDefault()?.RegistrationType;
             break;

         case "ExpiryDate":
             answer = supplier.Registrations.FirstOrDefault()?.ExpiryDate?.ToString("yyyy-MM-dd");
             break;

         // Bank Details
         case "AccountHolderName":
             answer = supplier.BankAccounts.FirstOrDefault()?.AccountHolderName;
             break;

         case "BankName":
             answer = supplier.BankAccounts.FirstOrDefault()?.BankName;
             break;

         case "BranchName":
             answer = supplier.BankAccounts.FirstOrDefault()?.BranchName;
             break;

         case "AccountNumber":
             answer = supplier.BankAccounts.FirstOrDefault()?.AccountNumber;
             break;

         case "IFSCCode":
             answer = supplier.BankAccounts.FirstOrDefault()?.IFSCCode;
             break;

         case "SWIFTCode":
             answer = supplier.BankAccounts.FirstOrDefault()?.SWIFTCode;
             break;

         case "IBAN":
             answer = supplier.BankAccounts.FirstOrDefault()?.IBAN;
             break;

         // Dispatch Location
         case "DispatchLocation":
             answer = supplier.DispatchLocations.FirstOrDefault()?.LocationName;
             break;
     }

     return new DefaultTemplateQuestionDto
     {
         QuestionId = question.Id,
         Question = question.Question,
         QuestionKey = question.QuestionKey,
         QuestionType = question.QuestionType,
         DisplayOrder = question.DisplayOrder,
         Answer = answer
     };
 }).ToList();

            return new GetDefaultVerificationTemplateDto
            {
                TemplateCode = template.TemplateCode,
                TemplateName = template.TemplateName,
                Questions = questionDtos
            };
        }

       
    }
}