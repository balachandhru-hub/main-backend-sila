
using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.SaveOrganizationModelMapping
{
    public class SaveOrganizationModelsCommand : IRequest<bool>
{
   

    public SaveOrganizationModelsDto Model { get; set; }
}
}