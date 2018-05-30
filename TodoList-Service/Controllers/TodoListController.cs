using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Mvc;
using TodoList_Service.Models;

namespace TodoList_Service.Controllers
{
    [System.Web.Http.Authorize]
    public class TodoListController : ApiController
    {
        private TodoList_ServiceContext db = new TodoList_ServiceContext();

        private ClaimsIdentity userClaims;

        public TodoListController()
        {
            userClaims = User.Identity as ClaimsIdentity;
        }

        /// <summary>
        /// Assure the presence of a scope claim containing a specific scope (i.e. access_as_user)
        /// </summary>
        /// <param name="scopeName">The name of the scope</param>
        private void CheckAccessTokenScope(string scopeName)
        {
            // Make sure access_as_user scope is present
            string scopeClaimValue = userClaims.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
            if (!string.Equals(scopeClaimValue, scopeName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    ReasonPhrase = @"Please request an access token to scope '{scopeName}'"
                });
            }
        }

        // GET: api/TodoList
        public IQueryable<Todo> GetTodoes()
        {
            CheckAccessTokenScope("access_as_user");
            string userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            return db.Todoes.Where(t => t.Owner.Equals(userId));
        }

        // POST: api/TodoList
        [ResponseType(typeof(Todo))]
        public IHttpActionResult PostTodo(Todo todo)
        {
            CheckAccessTokenScope("access_as_user");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            todo.Owner = userClaims.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value; 
            db.Todoes.Add(todo);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = todo.ID }, todo);
        }

        // DELETE: api/TodoList/5
        [ResponseType(typeof(Todo))]
        public IHttpActionResult DeleteTodo(int id)
        {
            CheckAccessTokenScope("access_as_user");
            Todo todo = db.Todoes.Find(id);
            if (todo == null)
            {
                return NotFound();
            }

            string userId = userClaims.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            if (todo.Owner != userId)
            {
                return Unauthorized();
            }

            db.Todoes.Remove(todo);
            db.SaveChanges();

            return Ok(todo);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TodoExists(int id)
        {
            return db.Todoes.Count(e => e.ID == id) > 0;
        }
    }
}