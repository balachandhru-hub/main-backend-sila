namespace Operations.Domain.Dtos
{
    /// <summary>
    /// File returned by a download endpoint (document content, template, export, correction report).
    /// </summary>
    public class FileDownloadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    }
}
