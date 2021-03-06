﻿
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserDetailsClient.Core.Constants;
using UserDetailsClient.Core.Models;
using Xamarin.Forms;
using UserDetailsClient.Core.Interfaces;
using UserDetailsClient.Core.Helpers;
using Microsoft.WindowsAzure.MobileServices;
// ReSharper disable PossibleMultipleEnumeration

namespace UserDetailsClient.Core.Services
{
    /// <summary>
    ///  For simplicity, we'll have this as a singleton. 
    /// </summary>
    public class AuthenticationService
    {
        private readonly IPublicClientApplication _pca;

        //Create the mobile service user object for MobileServiceAuthentication
        public static MobileServiceUser User { get; set; }


        private static readonly Lazy<AuthenticationService> lazy = new Lazy<AuthenticationService>
           (() => new AuthenticationService());

        public static AuthenticationService Instance => lazy.Value;


        private AuthenticationService()
        {

            // default redirectURI; each platform specific project will have to override it with its own
            var builder = PublicClientApplicationBuilder.Create(B2CConstants.ClientID)
                .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)
                .WithIosKeychainSecurityGroup(B2CConstants.IOSKeyChainGroup)
                .WithRedirectUri($"msal{B2CConstants.ClientID}://auth");

            // Android implementation is based on https://github.com/jamesmontemagno/CurrentActivityPlugin
            // iOS implementation would require to expose the current ViewController - not currently implemented as it is not required
            // UWP does not require this
            var windowLocatorService = DependencyService.Get<IParentWindowLocatorService>();

            if (windowLocatorService != null)
            {
                builder = builder.WithParentActivityOrWindow(() => windowLocatorService.GetCurrentParentWindow());
            }

            _pca = builder.Build();
        }

        public async Task<UserContext> SignInAsync()
        {
            UserContext newContext;
            try
            {
                // acquire token silent
                newContext = await AcquireTokenSilent();
            }
            catch (MsalUiRequiredException)
            {
                // acquire token interactive
                newContext = await SignInInteractively();
            }
            return newContext;
        }

        private async Task<UserContext> AcquireTokenSilent()
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();
            AuthenticationResult authResult = await _pca.AcquireTokenSilent(B2CConstants.Scopes, GetAccountByPolicy(accounts, B2CConstants.PolicySignUpSignIn))
               .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)
               .ExecuteAsync();

            var newContext = UpdateUserInfo(authResult);

            if (User == null)
            {
                var ar = authResult;
                var payload = new JObject();

                if (ar != null && !string.IsNullOrWhiteSpace(ar.IdToken))
                {
                    payload["access_token"] = ar.IdToken;
                }

                if (AzureMobileServiceClientHelper.DefaultClientHelper == null) return newContext;
                if (AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient == null) return newContext;

                User = await AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient.LoginAsync(
                    MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory,
                    payload).ConfigureAwait(true);

                if (User != null)
                {
                    AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient.CurrentUser =
                        new MobileServiceUser(User.UserId)
                        {
                            MobileServiceAuthenticationToken = User.MobileServiceAuthenticationToken
                        };
                }
            }

            return newContext;
        }

        public async Task<UserContext> ResetPasswordAsync()
        {
            AuthenticationResult authResult = await _pca.AcquireTokenInteractive(B2CConstants.Scopes)
                .WithPrompt(Prompt.NoPrompt)
                .WithAuthority(B2CConstants.AuthorityPasswordReset)
                .ExecuteAsync();

            var userContext = UpdateUserInfo(authResult);

            return userContext;
        }

        public async Task<UserContext> EditProfileAsync()
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();

            AuthenticationResult authResult = await _pca.AcquireTokenInteractive(B2CConstants.Scopes)
                .WithAccount(GetAccountByPolicy(accounts, B2CConstants.PolicyEditProfile))
                .WithPrompt(Prompt.NoPrompt)
                .WithAuthority(B2CConstants.AuthorityEditProfile)
                .ExecuteAsync();

            var userContext = UpdateUserInfo(authResult);

            return userContext;
        }

        private async Task<UserContext> SignInInteractively()
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();

            AuthenticationResult authResult = await _pca.AcquireTokenInteractive(B2CConstants.Scopes)
                .WithAccount(GetAccountByPolicy(accounts, B2CConstants.PolicySignUpSignIn))
                .ExecuteAsync();

            var newContext = UpdateUserInfo(authResult);

            if (User == null)
            {
                var ar = authResult;
                var payload = new JObject();

                if (ar != null && !string.IsNullOrWhiteSpace(ar.IdToken))
                {
                    payload["access_token"] = ar.IdToken;
                }

                if (AzureMobileServiceClientHelper.DefaultClientHelper == null) return newContext;
                if (AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient == null) return newContext;

                User = await AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient.LoginAsync(
                    MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory,
                    payload).ConfigureAwait(true);

                if (User != null)
                {
                    AzureMobileServiceClientHelper.DefaultClientHelper.CurrentClient.CurrentUser =
                        new MobileServiceUser(User.UserId)
                        {
                            MobileServiceAuthenticationToken = User.MobileServiceAuthenticationToken
                        };
                }
            }

            return newContext;
        }

        public async Task<UserContext> SignOutAsync()
        {

            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();
            while (accounts.Any())
            {
                await _pca.RemoveAsync(accounts.FirstOrDefault());
                accounts = await _pca.GetAccountsAsync();
            }

            var signedOutContext = new UserContext {IsLoggedOn = false};
            return signedOutContext;
        }

        private IAccount GetAccountByPolicy(IEnumerable<IAccount> accounts, string policy)
        {
            foreach (var account in accounts)
            {
                string userIdentifier = account.HomeAccountId.ObjectId.Split('.')[0];
                if (userIdentifier.EndsWith(policy.ToLower())) return account;
            }

            return null;
        }

        private string _base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var byteArray = Convert.FromBase64String(s);
            var decoded = Encoding.UTF8.GetString(byteArray, 0, byteArray.Count());
            return decoded;
        }

        public UserContext UpdateUserInfo(AuthenticationResult ar)
        {
	        var newContext = new UserContext {IsLoggedOn = false};
	        var user = _parseIdToken(ar.IdToken);

            newContext.AccessToken = ar.AccessToken;
            newContext.Name = user["name"]?.ToString();
            newContext.UserIdentifier = user["oid"]?.ToString();

            newContext.GivenName = user["given_name"]?.ToString();
            newContext.FamilyName = user["family_name"]?.ToString();

            newContext.StreetAddress = user["streetAddress"]?.ToString();
            newContext.City = user["city"]?.ToString();
            newContext.Province = user["state"]?.ToString();
            newContext.PostalCode = user["postalCode"]?.ToString();
            newContext.Country = user["country"]?.ToString();

            newContext.JobTitle = user["jobTitle"]?.ToString();

            if (user["emails"] is JArray emails)
            {
                newContext.EmailAddress = emails[0].ToString();
            }
            newContext.IsLoggedOn = true;

            return newContext;
        }

        private JObject _parseIdToken(string idToken)
        {
            // Get the piece with actual user info
            idToken = idToken.Split('.')[1];
            idToken = _base64UrlDecode(idToken);
            return JObject.Parse(idToken);
        }
    }
}