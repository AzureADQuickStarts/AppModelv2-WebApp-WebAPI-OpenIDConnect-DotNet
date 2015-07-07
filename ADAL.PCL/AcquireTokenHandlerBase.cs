﻿//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected static readonly Task CompletedTask = Task.FromResult(false);
        private readonly TokenCache tokenCache;

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string[] scope,
            ClientKey clientKey, TokenSubjectType subjectType)
        {
            this.Authenticator = authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId);
            PlatformPlugin.Logger.Information(this.CallState,
                string.Format(
                    "=== accessToken Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                    authenticator.Authority, scope, clientKey.ClientId,
                    (tokenCache != null)
                        ? tokenCache.GetType().FullName + string.Format(" ({0} items)", tokenCache.Count)
                        : "null",
                    subjectType));

            this.tokenCache = tokenCache;
            this.ClientKey = clientKey;
            this.TokenSubjectType = subjectType;

            this.LoadFromCache = (tokenCache != null);
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;
            if (ADALScopeHelper.IsNullOrEmpty(scope))
            {
                throw new ArgumentNullException("scope");
            }

            this.Scope = scope;
            ValidateScopeInput(scope);
        }

        internal CallState CallState { get; set; }
        protected bool SupportADFS { get; set; }
        protected Authenticator Authenticator { get; private set; }
        protected string[] Scope { get; set; }
        protected ClientKey ClientKey { get; private set; }
        protected TokenSubjectType TokenSubjectType { get; private set; }
        protected string UniqueId { get; set; }
        protected string DisplayableId { get; set; }
        protected UserIdentifierType UserIdentifierType { get; set; }
        protected bool LoadFromCache { get; set; }
        protected bool StoreToCache { get; set; }

        protected string[] GetDecoratedScope(string[] inputScope)
        {
            ISet<string> set = ADALScopeHelper.CreateSetFromArray(inputScope);
            set.Remove(ClientKey.ClientId); //remove client id if it exists
            set.Add("openid");
            //set.Add("offline_access");
            return set.ToArray();
        }

        protected void ValidateScopeInput(string[] scopeInput)
        {
            ISet<string> set = ADALScopeHelper.CreateSetFromArray(scopeInput);
            //make sure developer does not pass openid scope.
            if (set.Contains("openid"))
            {
                throw new ArgumentException("API does not accept openid as a user-provided scope");
            }

            //make sure developer does not pass offline_access scope.
            if (set.Contains("offline_access"))
            {
                throw new ArgumentException("API does not accept offline_access as a user-provided scope");
            }

            //check if scope or additional scope contains client ID.
            if (set.Contains(this.ClientKey.ClientId))
            {
                if (this.Scope.Length > 1)
                {
                    throw new ArgumentException("Client Id can only be provided as a single scope");
                }
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;

            try
            {
                await this.PreRunAsync();

                AuthenticationResultEx resultEx = null;

                if (this.LoadFromCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Scope,
                        this.ClientKey.ClientId, this.TokenSubjectType, this.UniqueId, this.DisplayableId,
                        this.CallState);
                    if (resultEx != null && resultEx.Result.Token == null && resultEx.RefreshToken != null)
                    {
                        List<AuthenticationResultEx> resultList = await this.RefreshAccessTokenAsync(resultEx);

                        if (resultList != null)
                        {
                            foreach (var resultItem in resultList)
                            {
                                this.tokenCache.StoreToCache(resultItem, this.Authenticator.Authority,
                                    resultItem.ScopeInResponse, this.ClientKey.ClientId, this.TokenSubjectType,
                                    this.CallState);
                            }

                            resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Scope,
                                this.ClientKey.ClientId, this.TokenSubjectType, this.UniqueId, this.DisplayableId,
                                this.CallState);
                        }
                    }
                }

                if (resultEx == null)
                {
                    await this.PreTokenRequest();
                    List<AuthenticationResultEx> resultList = await this.SendTokenRequestAsync();
                    foreach (var resultItem in resultList)
                    {
                        this.PostTokenRequest(resultItem);

                        if (this.StoreToCache)
                        {
                            if (!notifiedBeforeAccessCache)
                            {
                                this.NotifyBeforeAccessCache();
                                notifiedBeforeAccessCache = true;
                            }

                            this.tokenCache.StoreToCache(resultItem, this.Authenticator.Authority,
                                resultItem.ScopeInResponse, this.ClientKey.ClientId, this.TokenSubjectType,
                                this.CallState);
                        }
                        else
                        {
                            resultEx = resultItem;
                        }
                    }
                    resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Scope,
                        this.ClientKey.ClientId, this.TokenSubjectType, this.UniqueId, this.DisplayableId,
                        this.CallState);
                }

                await this.PostRunAsync(resultEx.Result);
                return resultEx.Result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(this.CallState, ex);
                throw;
            }
            finally
            {
                if (notifiedBeforeAccessCache)
                {
                    this.NotifyAfterAccessCache();
                }
            }
        }

        public static CallState CreateCallState(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new CallState(correlationId);
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);

            return CompletedTask;
        }

        protected virtual async Task PreRunAsync()
        {
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState);
            this.ValidateAuthorityType();
        }

        protected virtual Task PreTokenRequest()
        {
            return CompletedTask;
        }

        protected virtual void PostTokenRequest(AuthenticationResultEx result)
        {
            this.Authenticator.UpdateTenantId(result.Result.TenantId);
        }

        protected abstract void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters);

        protected virtual async Task<List<AuthenticationResultEx>> SendTokenRequestAsync()
        {
            var requestParameters = new DictionaryRequestParameters(this.GetDecoratedScope(this.Scope), this.ClientKey);
            var extraQueryParameters = this.AddAdditionalQueryStringParameters();
            this.AddAditionalRequestParameters(requestParameters);
            return await this.SendHttpMessageAsync(requestParameters, extraQueryParameters);
        }

        protected virtual string AddAdditionalQueryStringParameters()
        {
            return null;
        }

        protected async Task<List<AuthenticationResultEx>> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(this.GetDecoratedScope(this.Scope), this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            List<AuthenticationResultEx> results = await this.SendHttpMessageAsync(requestParameters);

            foreach (var result in results)
            {
                if (result.RefreshToken == null)
                {
                    result.RefreshToken = refreshToken;
                    PlatformPlugin.Logger.Verbose(this.CallState,
                        "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
                }
            }

            return results;
        }

        private async Task<List<AuthenticationResultEx>> RefreshAccessTokenAsync(AuthenticationResultEx result)
        {
            List<AuthenticationResultEx> newResultExList = null;

            if (!ADALScopeHelper.IsNullOrEmpty(this.Scope))
            {
                PlatformPlugin.Logger.Verbose(this.CallState, "Refreshing access token...");

                try
                {
                    newResultExList = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken);
                    this.Authenticator.UpdateTenantId(result.Result.TenantId);
                    foreach (var newResultEx in newResultExList)
                    {
                        if (newResultEx.Result.ProfileInfo == null)
                        {
                            // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                            newResultEx.Result.UpdateTenantAndUserInfo(result.Result.TenantId, result.Result.ProfileInfo,
                                result.Result.UserInfo);
                        }
                    }
                }
                catch (AdalException ex)
                {
                    AdalServiceException serviceException = ex as AdalServiceException;
                    if (serviceException != null && serviceException.ErrorCode == "invalid_request")
                    {
                        throw new AdalServiceException(
                            AdalError.FailedToRefreshToken,
                            AdalErrorMessage.FailedToRefreshToken + ". " + serviceException.Message,
                            serviceException.ServiceErrorCodes,
                            serviceException.InnerException);
                    }

                    newResultExList = null;
                }
            }

            return newResultExList;
        }

        private async Task<List<AuthenticationResultEx>> SendHttpMessageAsync(IRequestParameters requestParameters, string extraQueryParameters = null)
        {
            string uri = this.Authenticator.TokenUri;
            if (!string.IsNullOrEmpty(extraQueryParameters))
            {
                uri = uri + "?" + extraQueryParameters;
            }

            var client = new AdalHttpClient(uri, this.CallState)
            {
                Client = {BodyParameters = requestParameters}
            };
            TokenResponse tokenResponse = await client.GetResponseAsync<TokenResponse>(ClientMetricsEndpointType.Token);

            return tokenResponse.GetResults();
        }

        private void NotifyBeforeAccessCache()
        {
            this.tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope,
                ClientId = this.ClientKey.ClientId,
                UniqueId = this.UniqueId,
                DisplayableId = this.DisplayableId
            });
        }

        private void NotifyAfterAccessCache()
        {
            this.tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope,
                ClientId = this.ClientKey.ClientId,
                UniqueId = this.UniqueId,
                DisplayableId = this.DisplayableId
            });
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.Token != null)
            {
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.Token);

                PlatformPlugin.Logger.Information(this.CallState,
                    string.Format(
                        "=== token Acquisition finished successfully. An access token was retuned:\n\tToken Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
                        accessTokenHash,
                        result.ExpiresOn,
                        result.UserInfo != null
                            ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.UserInfo.UniqueId)
                            : "null"));
            }
        }

        private void ValidateAuthorityType()
        {
            if (!this.SupportADFS && this.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate,
                        this.Authenticator.Authority));
            }
        }
    }
}