    using Supplier.Domain.Dto;
    using MediatR;
    namespace Supplier.Application.Features.Commands.SupplierAnswers
    {
    public class SaveSupplierRFQAnswerCommand : IRequest<bool>
    {
        public SaveSupplierRFQAnswerDto Answer { get; }

        public SaveSupplierRFQAnswerCommand(
            SaveSupplierRFQAnswerDto answer)
        {
            Answer = answer;
        }
    }
    }