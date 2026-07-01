using ClosedXML.Excel;
using MediatR;
using MasterData.Application.Contracts.IRepository;
using MasterData.Domain.Entities;

namespace MasterData.Application.Features.Unspsc.Commands;

public class UploadUnspscCommandHandler
    : IRequestHandler<UploadUnspscCommand, int>
{
    private readonly IRepositoryWrapper _repository;

    public UploadUnspscCommandHandler(IRepositoryWrapper repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(
        UploadUnspscCommand request,
        CancellationToken cancellationToken)
    {
        var categories = new List<UnspscCategory>();

        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var entity = new UnspscCategory
            {
                Version = row.Cell(1).GetString(),

                Key = int.TryParse(row.Cell(2).GetString(), out var key)
                    ? key
                    : 0,

                Segment = long.TryParse(row.Cell(3).GetString(), out var segment)
                    ? segment
                    : 0,

                SegmentTitle = row.Cell(4).GetString(),
                SegmentDefinition = row.Cell(5).GetString(),

                Family = long.TryParse(row.Cell(6).GetString(), out var family)
                    ? family
                    : null,

                FamilyTitle = row.Cell(7).GetString(),
                FamilyDefinition = row.Cell(8).GetString(),

                Class = long.TryParse(row.Cell(9).GetString(), out var classCode)
                    ? classCode
                    : null,

                ClassTitle = row.Cell(10).GetString(),
                ClassDefinition = row.Cell(11).GetString(),

                Commodity = long.TryParse(row.Cell(12).GetString(), out var commodity)
                    ? commodity
                    : null,

                CommodityTitle = row.Cell(13).GetString(),
                CommodityDefinition = row.Cell(14).GetString(),

                Synonym = row.Cell(15).GetString(),
                Acronym = row.Cell(16).GetString()
            };

            categories.Add(entity);
        }

        await _repository.Unspsc.CreateRangeAsync(categories);

        await _repository.SaveAsync();

        return categories.Count;
    }
}