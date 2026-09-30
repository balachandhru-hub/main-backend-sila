namespace Buyer.Domain.Dtos
{
    public class ExcelUploadResultDto
    {
        public int TotalRows { get; set; }

        public int SuccessfulUploads { get; set; }

        public int FailedUploads { get; set; }

        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Id of the created ExcelMaterialMaster batch record. The
        /// uploaded rows are only inserted into ItemBuyerMaster once the
        /// single approval workflow for this batch is fully approved -
        /// SuccessfulUploads/FailedUploads reflect file-validation results
        /// at upload time, not final ItemBuyerMaster creation.
        /// </summary>
        public Guid? ExcelMaterialMasterId { get; set; }
    }
}