namespace Identity.Domain.Dto
{
    public class LoginResponse
    {

        public string Token { get; set; }
        public Guid RefreshToken { get; set; }


    }
}