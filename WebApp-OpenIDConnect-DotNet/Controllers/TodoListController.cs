using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TodoList_WebApp.Utils;

namespace TodoList_WebApp.Controllers
{
    [Authorize]
    public class TodoListController : Controller
    {
        private static string serviceUrl = ConfigurationManager.AppSettings["ida:TodoServiceUrl"];

        // GET: TodoList
        public async Task<ActionResult> Index()
        {
            AuthenticationResult result = null;
            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID, string.Empty);
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);
                
                // Here you ask for a token using the web app's clientId as the scope, since the web app and service share the same clientId.
                AuthenticationContext authContext = new AuthenticationContext(authority, false, new NaiveSessionCache(userObjectID));                
                result = await authContext.AcquireTokenSilentAsync(new string[] { Startup.clientId }, credential, UserIdentifier.AnyUser);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, serviceUrl + "/api/todolist");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    String responseString = await response.Content.ReadAsStringAsync();
                    JArray todoList = JArray.Parse(responseString);
                    ViewBag.TodoList = todoList;
                    return View();
                }
                else
                {
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    // and show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var todoTokens = authContext.TokenCache.ReadItems().Where(a => a.Scope.Contains(Startup.clientId));
                        foreach (TokenCacheItem tci in todoTokens)
                            authContext.TokenCache.DeleteItem(tci);

                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=An Error Occurred Reading To Do List: " + response.StatusCode);
            }
            catch (AdalException ee)
            {
                // If ADAL could not get a token silently, show the user an error indicating they might need to sign in again.
                return new RedirectResult("/Error?message=An Error Occurred Reading To Do List: " + ee.Message + " You might need to log out and log back in.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=An Error Occurred Reading To Do List: " + ex.Message);
            }
        }

        // POST: TodoList/Create
        [HttpPost]
        public async Task<ActionResult> Create(string description)
        {
            AuthenticationResult result = null;

            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID, string.Empty);
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);

                // Here you ask for a token using the web app's clientId as the scope, since the web app and service share the same clientId.                
                AuthenticationContext authContext = new AuthenticationContext(authority, false, new NaiveSessionCache(userObjectID));
                result = await authContext.AcquireTokenSilentAsync(new string[] { Startup.clientId }, credential, UserIdentifier.AnyUser);

                HttpContent content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Description", description) });
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, serviceUrl + "/api/todolist");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                request.Content = content;
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new RedirectResult("/TodoList");
                }
                else
                {
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    // and show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var todoTokens = authContext.TokenCache.ReadItems().Where(a => a.Scope.Contains(Startup.clientId));
                        foreach (TokenCacheItem tci in todoTokens)
                            authContext.TokenCache.DeleteItem(tci);

                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=Error reading your To-Do List.");
            }
            catch (AdalException ex)
            {
                // If ADAL could not get a token silently, show the user an error indicating they might need to sign in again.
                return new RedirectResult("/Error?message=Error: " + ex.Message + " You might need to sign in again.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=Error reading your To-Do List.  " + ex.Message);
            }
        }

        // POST: /TodoList/Delete
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            AuthenticationResult result = null;

            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                string authority = String.Format(CultureInfo.InvariantCulture, Startup.aadInstance, tenantID, string.Empty);
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);

                // Here you ask for a token using the web app's clientId as the scope, since the web app and service share the same clientId.                
                AuthenticationContext authContext = new AuthenticationContext(authority, false, new NaiveSessionCache(userObjectID));
                result = await authContext.AcquireTokenSilentAsync(new string[] { Startup.clientId }, credential, UserIdentifier.AnyUser);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, serviceUrl + "/api/todolist/" + id);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new RedirectResult("/TodoList");
                }
                else
                {
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    // and show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var todoTokens = authContext.TokenCache.ReadItems().Where(a => a.Scope.Contains(Startup.clientId));
                        foreach (TokenCacheItem tci in todoTokens)
                            authContext.TokenCache.DeleteItem(tci);

                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=Error deleting your To-Do Item.");
            }
            catch (AdalException ex)
            {
                // If ADAL could not get a token silently, show the user an error indicating they might need to sign in again.
                return new RedirectResult("/Error?message=Error: " + ex.Message + " You might need to sign in again.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=Error deleting your To-Do Item.  " + ex.Message);
            }
        }
    }
}