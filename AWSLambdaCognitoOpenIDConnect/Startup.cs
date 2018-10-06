using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AWSLambdaCognitoOpenIDConnect
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var clientSecret = Configuration["AmazonCognito:ClientSecret"];
            var clientId = Configuration["AmazonCognito:ClientId"];
            var metadataAddress = Configuration["AmazonCognito:MetaDataAddress"];
            var logOutUrl = Configuration["AmazonCognito:LogOutUrl"];
            var baseUrl = Configuration["AmazonCognito:BaseUrl"];
            
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options => options.Events.OnSigningIn = FilterGroupClaims)
            .AddOpenIdConnect(options =>
            {
                options.ResponseType = "code";
                options.MetadataAddress = metadataAddress;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("email");
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = logOutUrl;
                        logoutUri += $"?client_id={clientId}&logout_uri={baseUrl}";
                        context.Response.Redirect(logoutUri);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes => {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }

        //Remove all the claims that are unrelated to our identity
        private static Task FilterGroupClaims(CookieSigningInContext context)
        {
            var principal = context.Principal;
            if (principal.Identity is ClaimsIdentity identity)
            {
                var unused = identity.FindAll(GroupsToRemove).ToList();
                unused.ForEach(c => identity.TryRemoveClaim(c));
            }
            return Task.FromResult(principal);
        }

        private static bool GroupsToRemove(Claim claim)
        {
            string[] _groupObjectIds = new string[] { "identities" };
            return claim.Type == "groups" && !_groupObjectIds.Contains(claim.Type);
        }
    }
}
