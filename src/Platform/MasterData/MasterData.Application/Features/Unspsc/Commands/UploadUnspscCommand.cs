using MediatR;
using Microsoft.AspNetCore.Http;

namespace MasterData.Application.Features.Unspsc.Commands;

public record UploadUnspscCommand(
    IFormFile File
) : IRequest<int>;