namespace CognitoUserManager.Contracts.DTO
{
    public class TokenModel
    {
        public string IdToken { get; set; }
        public string CodigoConfirmacao { get; set; }
        public int Expiracao { get; set; }
        public string RefreshToken { get; set; }
    }
}