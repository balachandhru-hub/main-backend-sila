namespace Buyer.Domain.Dtos
{
    public class ExcelUploadResultDto
    {
        public int TotalRows { get; set; }

        public int SuccessfulUploads { get; set; }

        public int FailedUploads { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}