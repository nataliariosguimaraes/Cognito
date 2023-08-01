using CognitoUserManager.Contracts.DTO;
using CognitoUserManager.Contracts.Repositories;
using CognitoUserManager.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cognito.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        public const string Session_TokenKey = "_Tokens";
        private readonly IUsuarioRepository _usuarioService;
        private readonly IPersistService _cache;

        public UsuarioController(IUsuarioRepository userService, IPersistService cache)
        {
            _usuarioService = userService;
            _cache = cache;
        }

        #region Landing-TokensPage

        //[Authorize]
        //public async Task<IActionResult> IndexAsync()
        //{
        //    var id = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();
        //    var response = await _userService.GetUserAsync(id.Value);

        //    var model = new UpdateProfileModel
        //    {
        //        UserId = id.Value,
        //        GivenName = response.GivenName,
        //        PhoneNumber = response.PhoneNumber,
        //        Pincode = response.Address.GetOrDefaultValue("postal_code"),
        //        Country = response.Address.GetOrDefaultValue("country"),
        //        State = response.Address.GetOrDefaultValue("region"),
        //        Address = response.Address.GetOrDefaultValue("street_address"),
        //        Gender = response.Gender
        //    };

        //    return View(model);
        //}

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> IndexAsync(UpdateProfileModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View();
        //    }

        //    var userId = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();

        //    var token = _cache.Get<TokenModel>($"{userId.Value}_{Session_TokenKey}");

        //    model.AccessToken = token.AccessToken;

        //    var response = await _userService.UpdateUserAttributesAsync(model);

        //    if (response.IsSuccess)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    return View();
        //}

        #endregion

        #region ExistingUser-Login

        //public IActionResult Login()
        //{
        //    return View();
        //}

      
        [HttpPost]
        public async Task<AuthResponseModel> LoginAsync(LoginUsuarioModel model)
        {


            var response = await _usuarioService.LoginAsync(model);

            if (response.IsSuccess)
            {
                //_cache.Set<TokenModel>($"{response.UserId}_{Session_TokenKey}", response.Tokens);
                //_cache.Set<TokenModel>($"{response.UserId}_{Session_TokenKey}", response.Tokens);

            }

            return response;
        }

        #endregion

        #region NewUser-Signup

        //public IActionResult Signup()
        //{
        //    return View();
        //}

        [Route("CadastrarUsuario")]
        [HttpPost]
        public async Task<bool> CriarUsuario(NovoUsuario model)
        {
            if (!ModelState.IsValid)
            {
                //  return View();
            }

            var response = await _usuarioService.CriarUsuarioAsync(model);

            if (response.IsSuccess)
            {
                //TempData["UserId"] = response.UserId;
                //TempData["EmailAddress"] = response.EmailAddress;
                return true;
            }

            return false;
        }

        //public IActionResult ConfirmSignup()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> ConfirmSignupAsync(UserConfirmSignUpModel model)
        //{
        //    var response = await _userService.ConfirmUserSignUpAsync(model);

        //    if (response.IsSuccess)
        //    {
        //        return RedirectToAction("Login");
        //    }

        //    return View();
        //}

        #endregion

        #region Change-Password

        //[Authorize]
        //public IActionResult ChangePassword()
        //{
        //    var email = User.Claims.Where(x => x.Type == ClaimTypes.Email).First();
        //    var model = new ChangePwdModel
        //    {
        //        EmailAddress = email.Value
        //    };
        //    return View(model);
        //}

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> ChangePassword(ChangePwdModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var response = await _userService.TryChangePasswordAsync(model);

        //    if (response.IsSuccess)
        //    {
        //        return RedirectToAction("Logout");
        //    }

        //    return View(model);
        //}

        #endregion

        //[Authorize]
        //public async Task<IActionResult> LogOutAsync()
        //{
        //    var userId = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();

        //    var tokens = _cache.Get<TokenModel>($"{userId.Value}_{Session_TokenKey}");

        //    var user = new UserSignOutModel
        //    {
        //        AccessToken = tokens.AccessToken,
        //        UserId = userId.Value
        //    };

        //    _cache.Remove($"{userId.Value}_{Session_TokenKey}");

        //    await _userService.TryLogOutAsync(user);

        //    return RedirectToAction("Index");
        //}

        //#region Forgot-Password

        //public IActionResult ForgotPassword()
        //{
        //    return View();
        //}

        [Route("EsqueciSenha")]
        [HttpPost]
        public async Task<bool> EsqueciSenhaAsync(EsqueciSenhaModel model)
        {
            if (!ModelState.IsValid)
            {
               // return View(model);
            }

            var response = await _usuarioService.EsqueciSenhaAsync(model);

            if (response.IsSuccess)
            {
                //TempData["EmailAddress"] = response.EmailAddress;
                //TempData["UserId"] = response.UserId;

                return response.IsSuccess;
            }

            return false;
        }

        //public IActionResult ResetPasswordWithConfirmationCode()
        //{
        //    return View();
        //}


        [Route("ResetaSenhaComCodigoConfirmacao")]
        [HttpPost]
        public async Task<bool> ResetaSenhaComCodigoConfirmacaoAsync(ResetaSenhaModel model)
        {
            if (!ModelState.IsValid)
            {
              //  return View(model);
            }

            var response = await _usuarioService.ResetaSenhaComCodigoConfirmacaoAsync(model);

            if (response.IsSuccess)
            {
                return true;
            }

            return false;
        }

        //#endregion
    }

    public static class SessionExtensions
    {
        public static K GetOrDefaultValue<T, K>(this Dictionary<T, K> dictionary, T key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default;
        }
    }
}