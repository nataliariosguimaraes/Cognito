namespace CognitoUserManager.Contracts.DTO
{
    public class ResetaSenhaModel
    {
        public string UsuarioId { get; set; }
        public string NovaSenha { get; set; }
        public string CodigoConfirmacao { get; set; }
        public string Email { get; set; }
    }
}