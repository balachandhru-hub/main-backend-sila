
using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.SaveOrganizationModelMapping
{
    public class SaveOrganizationModelCommand : IRequest<bool>
{
   

    public SaveOrganizationModelDto Model { get; set; }
}
}