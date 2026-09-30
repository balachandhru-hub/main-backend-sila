using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Wishlist
{
    public class CreateWishlistCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public WishlistWriteDto Request { get; set; } = new();
    }

    public class UpdateWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public WishlistWriteDto Request { get; set; } = new();
    }

    public class GetWishlistQuery : IRequest<WishlistResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid WishlistId { get; set; }
    }

    public class GetWishlistsQuery : IRequest<List<WishlistListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }

    public class SubmitWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
    }

    public class DecideWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public WishlistDecisionDto Decision { get; set; } = new();
    }

    public class CancelWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
    }

    public class RetryWishlistIntegrationCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
    }

    public class CreateOutletCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public OutletWriteDto Request { get; set; } = new();
    }

    public class GetOutletsQuery : IRequest<List<OutletResponseDto>>
    {
        public Guid OrganizationId { get; set; }
    }

    public class GetErpIntegrationQuery : IRequest<ErpIntegrationResponseDto?>
    {
        public Guid OrganizationId { get; set; }
    }

    public class SaveErpIntegrationCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public ErpIntegrationWriteDto Request { get; set; } = new();
    }

    public class CreateWishlistCommandHandler : IRequestHandler<CreateWishlistCommand, Guid>
    {
        private readonly IWishlistWorkflow _workflow;
        public CreateWishlistCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<Guid> Handle(CreateWishlistCommand request, CancellationToken cancellationToken) =>
            _workflow.CreateAsync(request.OrganizationId, request.UserId, request.Request, cancellationToken);
    }

    public class UpdateWishlistCommandHandler : IRequestHandler<UpdateWishlistCommand, Unit>
    {
        private readonly IWishlistWorkflow _workflow;
        public UpdateWishlistCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public async Task<Unit> Handle(UpdateWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.UpdateAsync(request.OrganizationId, request.UserId, request.WishlistId, request.Request, cancellationToken);
            return Unit.Value;
        }
    }

    public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistResponseDto>
    {
        private readonly IWishlistWorkflow _workflow;
        public GetWishlistQueryHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<WishlistResponseDto> Handle(GetWishlistQuery request, CancellationToken cancellationToken) =>
            _workflow.GetAsync(request.OrganizationId, request.WishlistId, cancellationToken);
    }

    public class GetWishlistsQueryHandler : IRequestHandler<GetWishlistsQuery, List<WishlistListItemDto>>
    {
        private readonly IWishlistWorkflow _workflow;
        public GetWishlistsQueryHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<List<WishlistListItemDto>> Handle(GetWishlistsQuery request, CancellationToken cancellationToken) =>
            _workflow.ListAsync(request.OrganizationId, request.Index, request.Limit, cancellationToken);
    }

    public class SubmitWishlistCommandHandler : IRequestHandler<SubmitWishlistCommand, Unit>
    {
        private readonly IWishlistWorkflow _workflow;
        public SubmitWishlistCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public async Task<Unit> Handle(SubmitWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.SubmitAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }

    public class DecideWishlistCommandHandler : IRequestHandler<DecideWishlistCommand, Unit>
    {
        private readonly IWishlistWorkflow _workflow;
        public DecideWishlistCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public async Task<Unit> Handle(DecideWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.DecideAsync(request.OrganizationId, request.UserId, request.WishlistId, request.Decision, cancellationToken);
            return Unit.Value;
        }
    }

    public class CancelWishlistCommandHandler : IRequestHandler<CancelWishlistCommand, Unit>
    {
        private readonly IWishlistWorkflow _workflow;
        public CancelWishlistCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public async Task<Unit> Handle(CancelWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.CancelAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }

    public class RetryWishlistIntegrationCommandHandler : IRequestHandler<RetryWishlistIntegrationCommand, Unit>
    {
        private readonly IWishlistWorkflow _workflow;
        public RetryWishlistIntegrationCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public async Task<Unit> Handle(RetryWishlistIntegrationCommand request, CancellationToken cancellationToken)
        {
            await _workflow.RetryAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }

    public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Guid>
    {
        private readonly IWishlistWorkflow _workflow;
        public CreateOutletCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<Guid> Handle(CreateOutletCommand request, CancellationToken cancellationToken) =>
            _workflow.CreateOutletAsync(request.OrganizationId, request.Request, cancellationToken);
    }

    public class GetOutletsQueryHandler : IRequestHandler<GetOutletsQuery, List<OutletResponseDto>>
    {
        private readonly IWishlistWorkflow _workflow;
        public GetOutletsQueryHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<List<OutletResponseDto>> Handle(GetOutletsQuery request, CancellationToken cancellationToken) =>
            _workflow.ListOutletsAsync(request.OrganizationId, cancellationToken);
    }

    public class GetErpIntegrationQueryHandler : IRequestHandler<GetErpIntegrationQuery, ErpIntegrationResponseDto?>
    {
        private readonly IWishlistWorkflow _workflow;
        public GetErpIntegrationQueryHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<ErpIntegrationResponseDto?> Handle(GetErpIntegrationQuery request, CancellationToken cancellationToken) =>
            _workflow.GetErpConfigurationAsync(request.OrganizationId, cancellationToken);
    }

    public class SaveErpIntegrationCommandHandler : IRequestHandler<SaveErpIntegrationCommand, Guid>
    {
        private readonly IWishlistWorkflow _workflow;
        public SaveErpIntegrationCommandHandler(IWishlistWorkflow workflow) => _workflow = workflow;
        public Task<Guid> Handle(SaveErpIntegrationCommand request, CancellationToken cancellationToken) =>
            _workflow.SaveErpConfigurationAsync(request.OrganizationId, request.Request, cancellationToken);
    }
}
