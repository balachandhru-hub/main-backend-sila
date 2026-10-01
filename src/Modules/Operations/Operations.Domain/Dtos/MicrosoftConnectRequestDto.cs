using System.Text.Json;

namespace Operations.Domain.Dtos
{
    public class MicrosoftConnectRequestDto
    {
        /// <summary>Relative frontend path the browser returns to after the Microsoft sign-in.</summary>
        public string? ReturnUrl { get; set; }

        public JsonElement Draft { get; set; }
    }
}
