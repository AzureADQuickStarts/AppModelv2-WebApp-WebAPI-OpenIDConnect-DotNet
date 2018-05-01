using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin.Security.Notifications;
using System.IdentityModel.Tokens;
using System.Net.Http;
using TodoList_WebApp.Utils;
using System.Security.Claims;

using Microsoft.Identity.Client;
using System.Threading;

namespace TodoList_WebApp
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        
        public static string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string tenant = ConfigurationManager.AppSettings["Tenant"];

        public static string MyWebApiScope = ConfigurationManager.AppSettings["MyWebAPIScope"];

        private ConfidentialClientApplication app = null;

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    // The Client ID is used by the application to uniquely identify itself to Azure AD.
                    ClientId = clientId,

                    // The `Authority` represents the v2.0 endpoint - https://login.microsoftonline.com/{tenantname-or-common}/v2.0
                    Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant),

                    // Below is the list of scopes required:
                    //    MyWebApiScope is the Web API access token scope using the format
                    //       api://{Audience-or-AppId}/{ScopeName}
                    //    for other scopes please see https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-scopes
                    Scope = $"openid email profile offline_access {MyWebApiScope}",

                    RedirectUri = redirectUri,
                    PostLogoutRedirectUri = redirectUri,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                        // To only allow users from a single organizations, set ValidateIssuer to true and 'tenant' setting in web.config to the tenant name

                        ValidateIssuer = false,
                    },

                    // The `AuthorizationCodeReceived` notification is used to capture and redeem the authorization_code that the v2.0 endpoint returns to your app.

                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = OnAuthenticationFailed,
                        AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                    }
                });
    }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification)
        {
            string userObjectId = notification.AuthenticationTicket.Identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string tenantID = notification.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            ClientCredential cred = new ClientCredential(clientSecret);
            
            // Here you ask for an access token for your service's Web API scope
            app = new ConfidentialClientApplication(Startup.clientId, redirectUri, cred, new NaiveSessionCache(userObjectId, notification.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase)) {};
            var authResult = await app.AcquireTokenByAuthorizationCodeAsync(new string[] { MyWebApiScope }, notification.Code);
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            notification.Response.Redirect("/Error?message=" + notification.Exception.Message);
            return Task.FromResult(0);
        }
    }
}
