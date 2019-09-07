using AdCenter.Context;
using AdCenter.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AdCenter.Controllers
{
    public class AdminController : Controller
    {
        private string UrlPath
        {
            get
            {
                return Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "") + Request.ApplicationPath;
            }
        }
        private AdCenterDbContext db = new AdCenterDbContext();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Products()
        {
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath);
            return View();
        }
        [HttpGet]
        [ActionName("AllProducts")]
        public ContentResult GetProducts()
        {
            List<Product> products = null;

            products = db.Products.ToList<Product>();

            var json = JsonConvert.SerializeObject(products);
            return Content(json, "application/json");
        }
        [HttpPost]
        public async Task<ActionResult> Product(Product product)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid Data");
            }
            if (product.Id == -1)
            {
                db.Products.Add(product);
                await db.SaveChangesAsync();
            }
            else
            {
                if ((await db.Products.FindAsync(product.Id)) == null)
                {
                    return HttpNotFound();
                }
                using(var context = new AdCenterDbContext())
                {
                    context.Entry(product).State = EntityState.Modified;
                    context.SaveChanges();
                }
                
            }
            

            return new HttpStatusCodeResult(HttpStatusCode.OK);// CreatedAtRoute("DefaultApi", new { id = campaignModels.Id }, campaignModels);
        }

        public ActionResult Tiers()
        {
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath);
            return View();
        }
        [HttpGet]
        [ActionName("AllTiers")]
        public ContentResult GetTiers()
        {
            List<Tier> tiers = null;

            tiers = db.Tiers.ToList<Tier>();

            var json = JsonConvert.SerializeObject(tiers);
            return Content(json, "application/json");
        }
        public async Task<ActionResult> SetDefaultTier(int tierId)
        {
            if(db.Tiers.SingleOrDefault(r => r.Id == tierId) == null)
            {
                return HttpNotFound();
            }
            var tiers = db.Tiers.Where(r => r.Default);
            foreach(Tier t in tiers)
            {
                t.Default = false;
                db.Entry(t).State = EntityState.Modified;
            }
            var tier = db.Tiers.SingleOrDefault(r => r.Id == tierId);
            tier.Default = true;
            db.Entry(tier).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> Tier(Tier tier)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid Data");
            }
            if (tier.Id == -1)
            {
                db.Tiers.Add(tier);
                await db.SaveChangesAsync();
            }
            else
            {
                if ((await db.Tiers.FindAsync(tier.Id)) == null)
                {
                    return HttpNotFound();
                }
                using (var context = new AdCenterDbContext())
                {
                    context.Entry(tier).State = EntityState.Modified;
                    context.SaveChanges();
                }

            }


            return new HttpStatusCodeResult(HttpStatusCode.OK);// CreatedAtRoute("DefaultApi", new { id = campaignModels.Id }, campaignModels);
        }
    }
}