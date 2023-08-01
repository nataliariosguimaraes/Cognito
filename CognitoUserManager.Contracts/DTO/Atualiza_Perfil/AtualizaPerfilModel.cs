using Microsoft.AspNetCore.Http;

namespace CognitoUserManager.Contracts.DTO
{
    public class AtualizaPerfilModel
    {
        public string GivenName { get; set; }
        public string Telefone { get; set; }
        public IFormFile FotoPerfil { get; set; }
        public string Genero { get; set; }
        public string Endereco { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; }
        public string CodigoPIN { get; set; }
        public string UsuarioId { get; set; }
        public string Token { get; set; }
    }
}