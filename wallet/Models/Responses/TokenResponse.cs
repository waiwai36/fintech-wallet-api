namespace wallet.Models.Responses
{
    public class LoginResponse
    {
        public string UserName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenResponse
    {       
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
