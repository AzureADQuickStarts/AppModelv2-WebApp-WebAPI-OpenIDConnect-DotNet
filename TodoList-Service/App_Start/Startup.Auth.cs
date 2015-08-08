using Microsoft.Owin.Security.ActiveDirectory;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoList_Service
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:Audience"];

        public void ConfigureAuth(IAppBuilder app)
        { 
            var tvps = new TokenValidationParameters
            {
                // The web app and the service are sharing the same clientId
                ValidAudience = clientId,
                ValidateIssuer = false,
            };

            // NOTE: The usual WindowsAzureActiveDirectoryBearerAuthenticaitonMiddleware uses a
            // metadata endpoint which is not supported by the v2.0 endpoint.  Instead, this 
            // OpenIdConenctCachingSecurityTokenProvider can be used to fetch & use the OpenIdConnect
            // metadata document.

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(tvps, new OpenIdConnectCachingSecurityTokenProvider("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration")),
            });
        }
    }
}
