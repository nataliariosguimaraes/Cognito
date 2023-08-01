using CognitoUserManager.Contracts.DTO;
using System.Threading.Tasks;

namespace CognitoUserManager.Contracts.Repositories
{
    public interface IUsuarioRepository
    {
        Task<CriarUsuarioResponse> ConfirmUserSignUpAsync(ConfirmaInscricaoUsuarioModel model);
        Task<CriarUsuarioResponse> CriarUsuarioAsync(NovoUsuario model);
        Task<PerfilUsuarioResponse> GetUserAsync(string userId);
        Task<BaseResponseModel> TryChangePasswordAsync(AlteraSenhaModel model);
        Task<EsqueciSenhaResponse> EsqueciSenhaAsync(EsqueciSenhaModel model);
        Task<AuthResponseModel> LoginAsync(LoginUsuarioModel model);
        Task<DesconectarUsuarioResponse> TryLogOutAsync(DesconectarUsuarioModel model);
        Task<ResetaSenhaResponse> ResetaSenhaComCodigoConfirmacaoAsync(ResetaSenhaModel model);
        Task<AtualizaProfileResponse> UpdateUserAttributesAsync(AtualizaPerfilModel model);
    }
}