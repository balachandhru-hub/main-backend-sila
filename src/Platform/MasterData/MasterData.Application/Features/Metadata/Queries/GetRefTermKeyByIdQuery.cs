using MediatR;

namespace MasterData.Application.Features.Metadata.Queries.GetRefTermKeyById
{
    public class GetRefTermKeyByIdQuery : IRequest<string>
    {
        public Guid Id { get; set; }

        public GetRefTermKeyByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}