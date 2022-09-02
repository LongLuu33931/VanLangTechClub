using System;
using System.Configuration;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.VanLang;
using Owin;
using System.Linq;
using Team15_SEP2022.Models;
using System.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Team15_SEP2022
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ClientId"];
        private static string tenant = ConfigurationManager.AppSettings["Tenant"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["PostLogoutRedirectUri"];
        string authority = ConfigurationManager.AppSettings["Authority"].ToString() + tenant + "/v2.0";
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Home/Index"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(0.001),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                },
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            app.UseOpenIdConnectAuthentication(
               new OpenIdConnectAuthenticationOptions
               {
                   ClientId = clientId,
                   Authority = authority,
                   PostLogoutRedirectUri = postLogoutRedirectUri,
                   Scope = OpenIdConnectScope.OpenIdProfile,
                   ResponseType = OpenIdConnectResponseType.CodeIdToken,

                   TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = false
                   },

                   Notifications = new OpenIdConnectAuthenticationNotifications()
                   {
                       SecurityTokenValidated = (context) =>
                       {
                           string name = context.AuthenticationTicket.Identity.FindFirst("preferred_username").Value;
                           context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Name, name, string.Empty));
                           return System.Threading.Tasks.Task.FromResult(0);
                       },
                   },
                   
               });

        }


        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}