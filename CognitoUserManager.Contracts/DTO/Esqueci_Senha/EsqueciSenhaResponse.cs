namespace CognitoUserManager.Contracts.DTO
{
    public class EsqueciSenhaResponse : BaseResponseModel
    {
        public string UsuarioId { get; set; }
        public string Email { get; set; }
    }
}