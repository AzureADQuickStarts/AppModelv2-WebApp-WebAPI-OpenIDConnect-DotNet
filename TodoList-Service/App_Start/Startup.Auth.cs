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
                IssuerValidator = new IssuerValidator(ProxyIssuerValidator)
            };

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(tvps, new OpenIdConnectCachingSecurityTokenProvider("https://login.windows-ppe.net/common/v2.0/.well-known/openid-configuration")),
            });
        }

        // In a real multi-tenant app, you would want to validate here that the organization/user has signed up for the app.
        private string ProxyIssuerValidator(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (issuer.Contains("login.microsoftonline.com") || issuer.Contains("login.windows-ppe.net"))
                return issuer;
            throw new SecurityTokenValidationException("Unrecognized issuer.");
        }
    }
}
