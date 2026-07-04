using MediatR;

namespace MasterData.Application.Features.Email.Commands;

public class SendEmailCommand : IRequest<bool>
{
    public string ToEmail { get; set; }
    public string EmailKey { get; set; }

    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }

    public List<string>? CcEmail { get; set; }

    public Dictionary<string,string>? Parameters { get; set; }
}