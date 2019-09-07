using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AdCenter.Models;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;

namespace AdCenter.Controllers
{
    [Authorize]
    public class UserAccessController : Controller
    {

        private ApplicationUserManager _userManager;
        private ApplicationRoleManager _roleManager;
        public UserAccessController() { }
        public UserAccessController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        // GET: Admin
        public async Task<ActionResult> Roles()
        {
            //_roleManager.Roles;
            return View(await RoleManager.Roles.ToListAsync());
        }
        [ActionName("UserList")]
        [HttpGet]
        public ContentResult GetUsers()
        {
            var json = JsonConvert.SerializeObject(UserManager.Users.ToList());
            return Content(json, "application/json");
        }
        private string UrlPath
        {
            get
            {
                return Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "") + Request.ApplicationPath;
            }
        }
        public async Task<ActionResult> Users()
        {
            ViewBag.Roles = JsonConvert.SerializeObject(await RoleManager.Roles.ToListAsync());
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath);
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> UpdateStatus(int id, bool value)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return new HttpNotFoundResult();
            user.Active = value;
            var result = await UserManager.UpdateAsync(user);
            if (result.Succeeded)
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            else
            {
                string errors = string.Empty;
                foreach (string e in result.Errors)
                    errors += e + " ";
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, errors);
            }

        }
        [ActionName("User")]
        [HttpPost]
        public async Task<ActionResult> PostUser(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == -1)
                {
                    var user = new AdUser { UserName = model.UserName, Email = model.Email, Address = model.Address, PhoneNumber = model.PhoneNumber,Active = true, PaymentTerm = model.PaymentTerm, Balance = model.Balance, CreditLimit = model.CreditLimit };

                    var result = await UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        if (model.Roles != null)
                        {
                            string[] roles = new string[model.Roles.Length];
                            for (int i = 0; i < user.Roles.Count; i++)
                            {
                                roles[i] = (await RoleManager.FindByIdAsync(model.Roles[i])).Name;
                            }
                            await UserManager.AddToRolesAsync(user.Id, roles);
                        }
                    }
                    else
                    {
                        string errors = string.Empty;
                        foreach (string e in result.Errors)
                            errors += e + " ";
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, errors);
                    }
                }
                else
                {
                    var user = await UserManager.FindByIdAsync(model.Id);
                    if (user == null)
                        return new HttpNotFoundResult();
                    user.UserName = model.UserName;
                    user.Email = model.Email;
                    user.Address = model.Address;
                    user.PhoneNumber = model.PhoneNumber;
                    user.CreditLimit = model.CreditLimit;
                    user.Balance = model.Balance;
                    user.PaymentTerm = model.PaymentTerm;
                    var result = await UserManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        if (model.Roles != null)
                        {
                            foreach (var role in user.Roles)
                            {
                                if (!model.Roles.Any(t => t == role.RoleId))
                                {
                                    var r = await RoleManager.FindByIdAsync(role.RoleId);
                                    await UserManager.RemoveFromRoleAsync(user.Id, r.Name);
                                }
                            }
                            foreach (var roleId in model.Roles)
                                if (!user.Roles.Any(t => t.RoleId == roleId))
                                {
                                    var r = await RoleManager.FindByIdAsync(roleId);
                                    await UserManager.AddToRoleAsync(user.Id, r.Name);
                                }
                        }
                        else
                        {
                            var userRoles = user.Roles;
                            string[] strRoles = new string[userRoles.Count];
                            for (int i = 0; i < userRoles.Count; i++)
                            {

                                var r = await RoleManager.FindByIdAsync(userRoles.ElementAt(i).RoleId);
                                strRoles[i] = r.Name;
                            }
                            await UserManager.RemoveFromRolesAsync(user.Id, strRoles);
                        }
                        if (!string.IsNullOrWhiteSpace(model.Password))
                        {
                            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                            var passResult = await UserManager.ResetPasswordAsync(user.Id, resetToken, model.Password);
                            if (!passResult.Succeeded)
                            {
                                string errors = string.Empty;
                                foreach (string e in passResult.Errors)
                                    errors += e + " ";
                                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, errors);
                            }

                        }
                    }
                    else
                    {
                        string errors = string.Empty;
                        foreach (string e in result.Errors)
                            errors += e + " ";
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, errors);
                    }
                }
                return new HttpStatusCodeResult(200);
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Invalid input");

        }
        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }
        //// GET: Admin/Details/5
        //public async Task<ActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    AdRole adRole = await db.AdRoles.FindAsync(id);
        //    if (adRole == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(adRole);
        //}

        //// GET: Admin/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Admin/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Create([Bind(Include = "Id,Description,Name")] AdRole adRole)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.AdRoles.Add(adRole);
        //        await db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }

        //    return View(adRole);
        //}

        //// GET: Admin/Edit/5
        //public async Task<ActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    AdRole adRole = await db.AdRoles.FindAsync(id);
        //    if (adRole == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(adRole);
        //}

        //// POST: Admin/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit([Bind(Include = "Id,Description,Name")] AdRole adRole)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(adRole).State = EntityState.Modified;
        //        await db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    return View(adRole);
        //}

        //// GET: Admin/Delete/5
        //public async Task<ActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    AdRole adRole = await db.AdRoles.FindAsync(id);
        //    if (adRole == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(adRole);
        //}

        //// POST: Admin/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteConfirmed(int id)
        //{
        //    AdRole adRole = await db.AdRoles.FindAsync(id);
        //    db.AdRoles.Remove(adRole);
        //    await db.SaveChangesAsync();
        //    return RedirectToAction("Index");
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
