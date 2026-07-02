using MediatR;
using Microsoft.AspNetCore.Http;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Commands;

public record UploadUnspscCommand(
    IFormFile File
) : IRequest<ExcelUploadResultDto>;