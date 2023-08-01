using System.ComponentModel.DataAnnotations;

namespace CognitoUserManager.Contracts.DTO
{
    public class LoginUsuarioModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Senha { get; set; }
    }
}