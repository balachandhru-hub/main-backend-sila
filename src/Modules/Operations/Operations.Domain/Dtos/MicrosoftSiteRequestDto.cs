using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class MicrosoftSiteRequestDto
    {
        [Required, Url]
        public string SiteUrl { get; set; } = string.Empty;
    }
}
