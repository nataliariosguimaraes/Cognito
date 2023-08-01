using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using CognitoUserManager.Contracts;
using CognitoUserManager.Contracts.DTO;
using CognitoUserManager.Contracts.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CognitoUserManager.Core.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppConfig _cloudConfig;
        private readonly AmazonCognitoIdentityProviderClient _provider;
        private readonly CognitoUserPool _userPool;
        private readonly UserContextManager _userManager;
        private readonly HttpContext _httpContext;

        public UsuarioRepository(IOptions<AppConfig> appConfig, UserContextManager userManager, IHttpContextAccessor httpContextAccessor)
        {
            _cloudConfig = appConfig.Value;
            _provider = new AmazonCognitoIdentityProviderClient(
                _cloudConfig.AccessKeyId, _cloudConfig.AccessSecretKey, RegionEndpoint.GetBySystemName(_cloudConfig.Region));
            _userPool = new CognitoUserPool(_cloudConfig.UserPoolId, _cloudConfig.AppClientId, _provider);
            _userManager = userManager;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<CriarUsuarioResponse> CriarUsuarioAsync(NovoUsuario model)
        {
            //// Register the user using Cognito
            var criarUsuario = new SignUpRequest
            {
                ClientId = _cloudConfig.AppClientId,
                Password = model.Senha,
                Username = model.Email
            };

            criarUsuario.UserAttributes.Add(new AttributeType
            {
                Name = "email",
                Value = model.Email
            });
            criarUsuario.UserAttributes.Add(new AttributeType
            {
                Value = model.Nome,
                Name = "given_name"
            });
            criarUsuario.UserAttributes.Add(new AttributeType
            {
                Value = model.Telefone,
                Name = "phone_number"
            });



            //if (model.ProfilePhoto != null)
            //{
            //    // upload the incoming profile photo to user's S3 folder
            //    // and get the s3 url
            //    // add the s3 url to the profile_photo attribute of the userCognito
            //    var picUrl = await _storage.AddItem(model.ProfilePhoto, "profile");

            //    signUpRequest.UserAttributes.Add(new AttributeType
            //    {
            //        Value = picUrl,
            //        Name = "picture"
            //    });
            //}

            SignUpResponse response =await _provider.SignUpAsync(criarUsuario);



            var criarUsuarioGrupo = new AdminAddUserToGroupRequest
            {
                GroupName = "Administrador",
                Username = model.Email,
                UserPoolId = _cloudConfig.UserPoolId
            };

            AdminAddUserToGroupResponse teste = await _provider.AdminAddUserToGroupAsync(criarUsuarioGrupo);



            var signUpResponse = new CriarUsuarioResponse
            {
                UsuarioId = response.UserSub,
                Email = model.Email,
                Message = $"Confirmation Code sent to {response.CodeDeliveryDetails.Destination} via {response.CodeDeliveryDetails.DeliveryMedium.Value}",
                Status = CognitoStatusCodes.USER_UNCONFIRMED,
                IsSuccess = true
            };

            return signUpResponse;
        }

        public async Task<CriarUsuarioResponse> ConfirmUserSignUpAsync(ConfirmaInscricaoUsuarioModel model)
        {
            ConfirmSignUpRequest request = new ConfirmSignUpRequest
            {
                ClientId = _cloudConfig.AppClientId,
                ConfirmationCode = model.CodigoConfirmacao,
                Username = model.Email
            };
            var response = await _provider.ConfirmSignUpAsync(request);

            // add to default users group
            //var addUserToGroupRequest = new AdminAddUserToGroupRequest
            //{
            //    UserPoolId = _cloudConfig.UserPoolId,
            //    Username = model.UserId,
            //    GroupName = "-users-group"
            //};
            //var addUserToGroupResponse = await _provider.AdminAddUserToGroupAsync(addUserToGroupRequest);

            return new CriarUsuarioResponse
            {
                Email = model.Email,
                UsuarioId = model.UserId,
                Message = "User Confirmed",
                IsSuccess = true
            };
        }

        public async Task<BaseResponseModel> TryChangePasswordAsync(AlteraSenhaModel model)
        {
            // FetchTokens for User
            var tokenResponse = await AutenticacaoUsuarioAsync(model.Email, model.SenhaAtual);

            ChangePasswordRequest request = new ChangePasswordRequest
            {
                AccessToken = tokenResponse.Item2.AccessToken,
                PreviousPassword = model.SenhaAtual,
                ProposedPassword = model.NovaSenha
            };
            ChangePasswordResponse response = await _provider.ChangePasswordAsync(request);
            return new AlteraSenhaResponse { UsuarioId = tokenResponse.Item1.Username, Message = "Password Changed", IsSuccess = true };
        }

        public async Task<AuthResponseModel> LoginAsync(LoginUsuarioModel model)
        {
            try
            {
                var result = await AutenticacaoUsuarioAsync(model.Email, model.Senha);

                //if (result.Item1.Username != null)
                //{
                //    await _userManager.SignIn(_httpContext, new Dictionary<string, string>() {
                //        {ClaimTypes.Email, result.Item1.UserID},
                //        {ClaimTypes.NameIdentifier, result.Item1.Username}
                //    });
                //}

                var authResponseModel = new AuthResponseModel();
                authResponseModel.EmailAddress = result.Item1.UserID;
                authResponseModel.UserId = result.Item1.Username;
                authResponseModel.Tokens = new TokenModel
                {
                    IdToken = result.Item2.IdToken,
                    CodigoConfirmacao = result.Item2.AccessToken,
                    Expiracao = result.Item2.ExpiresIn,
                    RefreshToken = result.Item2.RefreshToken
                };
                authResponseModel.IsSuccess = true;
                return authResponseModel;
            }
            catch (UserNotConfirmedException)
            {
                var listUsuariosResponse = await BuscarUsuarioPorEmail(model.Email);

                if (listUsuariosResponse != null && listUsuariosResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var users = listUsuariosResponse.Users;
                    var filtered_user = users.FirstOrDefault(x => x.Attributes.Any(x => x.Name == "email" && x.Value == model.Email));

                    var reenviarCodigoConfirmacaoResponse = await _provider.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
                    {
                        ClientId = _cloudConfig.AppClientId,
                        Username = filtered_user.Username
                    });

                    if (reenviarCodigoConfirmacaoResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return new AuthResponseModel
                        {
                            IsSuccess = false,
                            Message = $"Confirmation Code sent to {reenviarCodigoConfirmacaoResponse.CodeDeliveryDetails.Destination} via {reenviarCodigoConfirmacaoResponse.CodeDeliveryDetails.DeliveryMedium.Value}",
                            Status = CognitoStatusCodes.USER_UNCONFIRMED,
                            UserId = filtered_user.Username
                        };
                    }
                    else
                    {
                        return new AuthResponseModel
                        {
                            IsSuccess = false,
                            Message = $"Resend Confirmation Code Response: {reenviarCodigoConfirmacaoResponse.HttpStatusCode.ToString()}",
                            Status = CognitoStatusCodes.API_ERROR,
                            UserId = filtered_user.Username
                        };
                    }
                }
                else
                {
                    return new AuthResponseModel
                    {
                        IsSuccess = false,
                        Message = "No Users found for the EmailAddress.",
                        Status = CognitoStatusCodes.USER_NOTFOUND
                    };
                }
            }
            catch (UserNotFoundException)
            {
                return new AuthResponseModel
                {
                    IsSuccess = false,
                    Message = "EmailAddress not found.",
                    Status = CognitoStatusCodes.USER_NOTFOUND
                };
            }
            catch (NotAuthorizedException)
            {
                return new AuthResponseModel
                {
                    IsSuccess = false,
                    Message = "Incorrect username or password",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        private async Task<Tuple<CognitoUser, AuthenticationResultType>> AutenticacaoUsuarioAsync(string email, string senha)
        {
            try
            {
                CognitoUser usuario = new CognitoUser(email, _cloudConfig.AppClientId, _userPool, _provider);
                InitiateSrpAuthRequest autenticacaoRequest = new InitiateSrpAuthRequest()
                {
                    Password = senha
                };

                AuthFlowResponse autenticacaoResponse = await usuario.StartWithSrpAuthAsync(autenticacaoRequest);
                var result = autenticacaoResponse.AuthenticationResult;
                // return new Tuple<string, string, AuthenticationResultType>(user.UserID, user.Username, result);
                return new Tuple<CognitoUser, AuthenticationResultType>(usuario, result);
            }
            catch (Exception ex)
            {
                return null;
            }




        }

        public async Task<DesconectarUsuarioResponse> TryLogOutAsync(DesconectarUsuarioModel model)
        {
            var request = new GlobalSignOutRequest { AccessToken = model.Token };
            var response = await _provider.GlobalSignOutAsync(request);

            await _userManager.SignOut(_httpContext);
            return new DesconectarUsuarioResponse { UsuarioId = model.UsuarioId, Message = "User Signed Out" };
        }

        public async Task<AtualizaProfileResponse> UpdateUserAttributesAsync(AtualizaPerfilModel model)
        {
            UpdateUserAttributesRequest userAttributesRequest = new UpdateUserAttributesRequest
            {
                AccessToken = model.Token
            };

            userAttributesRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.GivenName,
                Name = "given_name"
            });

            userAttributesRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.Telefone,
                Name = "phone_number"
            });

            // upload the incoming profile photo to user's S3 folder
            // and get the s3 url
            // add the s3 url to the profile_photo attribute of the userCognito
            // if (model.ProfilePhoto != null)
            // {
            //     var picUrl = await _storage.AddItem(model.ProfilePhoto, "profile");
            //     userAttributesRequest.UserAttributes.Add(new AttributeType
            //     {
            //         Value = picUrl,
            //         Name = "picture"
            //     });
            // }

            if (model.Genero != null)
            {
                userAttributesRequest.UserAttributes.Add(new AttributeType
                {
                    Value = model.Genero,
                    Name = "gender"
                });
            }

            if (!string.IsNullOrEmpty(model.Endereco) ||
                string.IsNullOrEmpty(model.Estado) ||
                string.IsNullOrEmpty(model.Pais) ||
                string.IsNullOrEmpty(model.CodigoPIN))
            {
                var dictionary = new Dictionary<string, string>();

                dictionary.Add("street_address", model.Endereco);
                dictionary.Add("region", model.Estado);
                dictionary.Add("country", model.Pais);
                dictionary.Add("postal_code", model.CodigoPIN);

                userAttributesRequest.UserAttributes.Add(new AttributeType
                {
                    Value = JsonConvert.SerializeObject(dictionary),
                    Name = "address"
                });
            }

            var response = await _provider.UpdateUserAttributesAsync(userAttributesRequest);
            return new AtualizaProfileResponse { UsuarioId = model.UsuarioId, Message = "Profile Updated", IsSuccess = true };
        }

        public async Task<EsqueciSenhaResponse> EsqueciSenhaAsync(EsqueciSenhaModel model)
        {
            var listUsuariosResponse = await BuscarUsuarioPorEmail(model.Email);

            if (listUsuariosResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                var usuarios = listUsuariosResponse.Users;
                var usuarios_filtrados = usuarios.FirstOrDefault(x => x.Attributes.Any(x => x.Name == "email" && x.Value == model.Email));
                if (usuarios_filtrados != null)
                {
                    var esqueciSenhaResponse = await _provider.ForgotPasswordAsync(new ForgotPasswordRequest
                    {
                        ClientId = _cloudConfig.AppClientId,
                        Username = usuarios_filtrados.Username
                    });

                    if (esqueciSenhaResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return new EsqueciSenhaResponse
                        {
                            IsSuccess = true,
                            Message = $"Confirmation Code sent to {esqueciSenhaResponse.CodeDeliveryDetails.Destination} via {esqueciSenhaResponse.CodeDeliveryDetails.DeliveryMedium.Value}",
                            UsuarioId = usuarios_filtrados.Username,
                            Email = model.Email,
                            Status = CognitoStatusCodes.USER_UNCONFIRMED
                        };
                    }
                    else
                    {
                        return new EsqueciSenhaResponse
                        {
                            IsSuccess = false,
                            Message = $"ListUsers Response: {esqueciSenhaResponse.HttpStatusCode.ToString()}",
                            Status = CognitoStatusCodes.API_ERROR
                        };
                    }
                }
                else
                {
                    return new EsqueciSenhaResponse
                    {
                        IsSuccess = false,
                        Message = $"No users with the given emailAddress found.",
                        Status = CognitoStatusCodes.USER_NOTFOUND
                    };
                }
            }
            else
            {
                return new EsqueciSenhaResponse
                {
                    IsSuccess = false,
                    Message = $"ListUsers Response: {listUsuariosResponse.HttpStatusCode.ToString()}",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        public async Task<ResetaSenhaResponse> ResetaSenhaComCodigoConfirmacaoAsync(ResetaSenhaModel model)
        {
            var response = await _provider.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
            {
                ClientId = _cloudConfig.AppClientId,
                Username = model.UsuarioId,
                Password = model.NovaSenha,
                ConfirmationCode = model.CodigoConfirmacao
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return new ResetaSenhaResponse
                {
                    IsSuccess = true,
                    Message = "Password Updated. Please Login."
                };
            }
            else
            {
                return new ResetaSenhaResponse
                {
                    IsSuccess = false,
                    Message = $"ResetPassword Response: {response.HttpStatusCode.ToString()}",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        private async Task<ListUsersResponse> BuscarUsuarioPorEmail(string email)
        {
            ListUsersRequest listUsuariosRequest = new ListUsersRequest
            {
                UserPoolId = _cloudConfig.UserPoolId,
                Filter = $"email=\"{email}\""
            };
            return await _provider.ListUsersAsync(listUsuariosRequest);
        }

        public async Task<PerfilUsuarioResponse> GetUserAsync(string userId)
        {
            var userResponse = await _provider.AdminGetUserAsync(new AdminGetUserRequest
            {
                Username = userId,
                UserPoolId = _cloudConfig.UserPoolId
            });

            // var user = _userPool.GetUser(userId);

            var attributes = userResponse.UserAttributes;
            var response = new PerfilUsuarioResponse
            {
                Email = attributes.GetValueOrDefault("email", string.Empty),
                Nome = attributes.GetValueOrDefault("given_name", string.Empty),
                Telefone = attributes.GetValueOrDefault("phone_number", string.Empty),
                Genero = attributes.GetValueOrDefault("gender", string.Empty),
                UsuarioId = userId
            };

            var address = attributes.GetValueOrDefault("address", string.Empty);
            if (!string.IsNullOrEmpty(address))
            {
                response.Endereco = JsonConvert.DeserializeObject<Dictionary<string, string>>(address);
            }

            return response;
        }
    }

    internal static class AttributeTypeExtension
    {
        public static string GetValueOrDefault(this List<AttributeType> attributeTypes, string propertyName, string defaultValue)
        {
            var prop = attributeTypes.FirstOrDefault(x => x.Name == propertyName);
            if (prop != null) return prop.Value;
            else return defaultValue;
        }
    }
}
