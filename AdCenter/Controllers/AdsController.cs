using AdCenter.Context;
using AdCenter.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AdCenter.Controllers
{
    [Authorize]
    public class AdsController : Controller
    {
        private AdCenterDbContext db = new AdCenterDbContext();
        private ApplicationDbContext context = new ApplicationDbContext();
        private int UserId
        {
            get
            {
                return int.Parse(User.Identity.GetUserId());
            }
        }
        private string UrlPath
        {
            get
            {
                return Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "") + Request.ApplicationPath;
            }
        }
        // GET: Ads
        public ActionResult Index(int? id)
        {
            List<AdGroup> adGroup = null;
            if (User.IsInRole("Admin"))
                adGroup = db.AdGroups.ToList();
            else
                adGroup = db.AdGroups.Where(r => r.Campaign.AdvertiserId == UserId).ToList();
            var adGroupList = adGroup.Select(r => new { r.Id, r.Name, r.Bid, r.DailyBudget, r.StartDate, r.EndDate }).ToList();
            adGroupList.Insert(0, new { Id = -1, Name = "- Select Ad Group -", Bid = decimal.Parse("0.00"), DailyBudget = decimal.Parse("0.00"), StartDate = new Nullable<DateTime>(), EndDate = new Nullable<DateTime>() });
            ViewBag.AdGroups = JsonConvert.SerializeObject(adGroupList);
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath.TrimEnd('/'));
            ViewBag.AdGroupId = id == null ? -1 : id;
            return View();
        }
        [ActionName("Ads")]
        [HttpGet]
        public ContentResult GetAds()
        {
            List<Ad> ads = null;
            if (User.IsInRole("Admin"))
                ads = db.Ads.Include(r => r.AdGroup).ToList();
            else
                ads = db.Ads.Where(r => r.AdGroup.Campaign.AdvertiserId == UserId).Include(r => r.AdGroup).ToList();
            for (int i = 0; i < ads.Count(); i++)
                ads[i] = AdStatus(ads[i]);

            var searchCount = db.SearchLog.GroupBy(r => r.AdId, (key, g) => new { AdsId = key, Search = g.Count() });
            var impressionCount = db.ImpressionLog.GroupBy(r => r.AdId, (key, g) => new { AdsId = key, Impression = g.Count(), Spend = g.Sum(r => r.BidPrice) });
            var merged = from ad in ads
                         join s in searchCount on ad.Id equals s.AdsId into gj
                         from subsearch in gj.DefaultIfEmpty()
                         join i in impressionCount on ad.Id equals i.AdsId into bj
                         from subimpression in bj.DefaultIfEmpty()
                         select new
                         {
                             AdGroupName = ad.AdGroup.Name,
                             ad.Id,
                             ad.Name,
                             ad.Reason,
                             ad.Status,
                             ad.Active,
                             ad.AdGroupId,
                             ad.ClickUrl,
                             ad.ConversionDomain,
                             Search = subsearch?.Search ?? 0,
                             Impression = subimpression?.Impression ?? 0,
                             Spend = subimpression?.Spend??0
                         };
            var json = JsonConvert.SerializeObject(merged, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            return Content(json, "application/json");
        }
        [ActionName("Ad")]
        [HttpPost]
        public async Task<ActionResult> PostAdGroup(Ad ad)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid model.");
            }
            if (ad.Id == -1)
            {
                db.Ads.Add(ad);
            }
            else
            {
                if (!AdExists(ad.Id))
                {
                    return HttpNotFound();
                }
                db.Entry(ad).State = EntityState.Modified;
            }
            await db.SaveChangesAsync();

            return new HttpStatusCodeResult(200);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateStatus(int id, bool value)
        {

            if (!AdExists(id))
                return HttpNotFound();
            Ad a = await db.Ads.FindAsync(id);
            a.Active = value;
            db.Entry(a).State = EntityState.Modified;
            await db.SaveChangesAsync();
            a = AdStatus(a);
            return Content(JsonConvert.SerializeObject(a), "application/json");
        }
        [HttpPost]
        [ActionName("Clone")]
        public async Task<ActionResult> CloneAd(int id, string name, int adGroupId)
        {
            if (!AdExists(id))
                return HttpNotFound();
            Ad a = await db.Ads.FindAsync(id);
            a.Name = name;
            a.AdGroupId = adGroupId;
            db.Ads.Add(a);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(200);
        }
        [HttpPost]
        public async Task<ActionResult> DeleteAd(int id)
        {
            if (!AdExists(id))
                return HttpNotFound();
            Ad c = await db.Ads.FindAsync(id);
            db.Ads.Remove(c);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        private Ad AdStatus(Ad ad)
        {
            var adGroup = db.AdGroups.SingleOrDefault(r => r.Id == ad.AdGroupId);
            var campaign = db.Campaigns.SingleOrDefault(r => r.Id == adGroup.CampaignId);
            var advertiser = context.Users.SingleOrDefault(r => r.Id == campaign.AdvertiserId);
            if (advertiser == null)
            {
                ad.Status = "Inactive";
                ad.Reason = "Advertiser not found";
                return ad;
            }

            if (!advertiser.Active)
            {
                ad.Status = "Inactive";
                ad.Reason = "Advertiser account is inactive";
                return ad;
            }
            if (advertiser.PaymentTerm == "PrePay" && advertiser.Balance <= 0)
            {
                ad.Status = "Inactive";
                ad.Reason = "Advertiser has not enough fund";
                return ad;
            }
            if (advertiser.PaymentTerm == "Invoice" && advertiser.Balance <= advertiser.CreditLimit)
            {
                ad.Status = "Inactive";
                ad.Reason = "Advertiser's credit limit is reached";
                return ad;
            }
            if (!campaign.Active)
            {
                ad.Status = "Inactive";
                ad.Reason = "Campaign is inactive";
                return ad;
            }
            //var adGroup = db.AdGroups.SingleOrDefault(r => r.Id == ad.AdGroupId);
            if (!adGroup.Active)
            {
                ad.Status = "Inactive";
                ad.Reason = "Ad Group is inactive";
                return ad;
            }
            if (!ad.Active)
            {
                ad.Status = "Inactive";
                ad.Reason = "Ad is inactive";
                return ad;
            }
            if (string.IsNullOrWhiteSpace(ad.Status))
            {
                ad.Status = "Active";
                ad.Reason = string.Empty;
                return ad;
            }
            return ad;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AdExists(int id)
        {
            return db.Ads.Count(e => e.Id == id) > 0;
        }
    }
}