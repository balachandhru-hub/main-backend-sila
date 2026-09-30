namespace Buyer.Domain.Dto
{
    public class MessageAttachmentFileDto
    {
        public string FileName { get; set; }

        public string? ContentType { get; set; }

        public byte[] FileBytes { get; set; }
    }
}
