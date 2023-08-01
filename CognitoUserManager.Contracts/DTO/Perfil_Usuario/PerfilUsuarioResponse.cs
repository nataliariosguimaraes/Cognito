using System.Collections.Generic;

namespace CognitoUserManager.Contracts.DTO
{
    public class PerfilUsuarioResponse : BaseResponseModel
    {
        public PerfilUsuarioResponse()
        {
            Endereco = new Dictionary<string, string>();
        }

        public string Email { get; set; }
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public string UsuarioId { get; set; }
        public Dictionary<string, string> Endereco { get; set; }
        public string Genero { get; set; }
    }
}