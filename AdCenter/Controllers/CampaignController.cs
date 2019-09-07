using AdCenter.Context;
using AdCenter.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;
using Microsoft.AspNet.Identity;

namespace AdCenter.Controllers
{
    [Authorize]
    public class CampaignController : Controller
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
                return (Request.Url.AbsolutePath == "/" ? Request.Url.AbsoluteUri : Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "")) + Request.ApplicationPath;
            }
        }
        // GET: Campaign
        public ActionResult Index()
        {
            ViewBag.DeliveryTypes = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<DeliveryType>("--select delivery type--"));
            if (User.IsInRole("Admin"))
            {
                var c = db.Campaigns.ToList();
                for (int i = 0; i < c.Count(); i++)
                {
                    c[i] = CampaignStatus(c[i]);
                }
                ViewBag.Campaigns = JsonConvert.SerializeObject(c);
                ViewBag.Users = JsonConvert.SerializeObject(context.Users.Select(r => new { Id = r.Id, UserName = r.UserName, Email = r.Email }));
            }
            else
            {
                var c = db.Campaigns.Where(r => r.AdvertiserId == UserId).ToList();
                for (int i = 0; i < c.Count(); i++)
                {
                    c[i] = CampaignStatus(c[i]);
                }
                ViewBag.Campaigns = JsonConvert.SerializeObject(c);
            }
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath.TrimEnd('/'));
            return View();
        }
        public ActionResult Wizard()
        {
            ViewBag.DeliveryTypes = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<DeliveryType>("--select delivery type--"));
            ViewBag.DayPartingTypes = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<DayPartingType>());
            ViewBag.KeywordTargetingType = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<KeywordTargetingType>());
            ViewBag.Products = JsonConvert.SerializeObject(db.Products.ToList());
            ViewBag.Tiers = JsonConvert.SerializeObject(db.Tiers.ToList());
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath);
            if (User.IsInRole("Admin"))
            {
                ViewBag.Users = JsonConvert.SerializeObject(context.Users.Select(r => new { Id = r.Id, UserName = r.UserName, Email = r.Email }));
            }
            return View();
        }
        [HttpGet]
        [ActionName("Campaigns")]
        public ContentResult GetCampaigns()
        {
            List<Campaign> campaigns = null;
            if (User.IsInRole("Admin"))
                campaigns = db.Campaigns.ToList<Campaign>();
            else
                campaigns = db.Campaigns.Where(r => r.AdvertiserId == UserId).ToList<Campaign>();
            for (int i = 0; i < campaigns.Count(); i++)
            {
                campaigns[i] = CampaignStatus(campaigns[i]);
            }
            var json = JsonConvert.SerializeObject(campaigns);
            return Content(json, "application/json");
        }


        [HttpGet]
        [ActionName("Campaign")]
        public async Task<ContentResult> GetCampaignModels(int id)
        {
            Campaign campaignModels = await db.Campaigns.FindAsync(id);
            if (!User.IsInRole("Admin"))
                campaignModels = campaignModels.AdvertiserId == UserId ? campaignModels : null;
            if (campaignModels == null)
            {
                return Content("", "application/json");
            }

            var json = JsonConvert.SerializeObject(campaignModels);
            return Content(json, "application/json");
        }

        [HttpPost]
        [ActionName("Campaign")]
        public async Task<ActionResult> PostCampaign(Campaign campaignModels)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid Data");
            }
            if (campaignModels.Id == -1)
            {
                campaignModels.CreatedBy = UserId;
                if (!User.IsInRole("Admin"))
                    campaignModels.AdvertiserId = UserId;
                db.Campaigns.Add(campaignModels);
            }
            else
            {
                if (!CampaignModelsExists(campaignModels.Id))
                {
                    return HttpNotFound();
                }
                db.Entry(campaignModels).State = EntityState.Modified;
            }
            await db.SaveChangesAsync();

            return new HttpStatusCodeResult(HttpStatusCode.OK);// CreatedAtRoute("DefaultApi", new { id = campaignModels.Id }, campaignModels);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateStatus(int id, bool value)
        {

            if (!CampaignModelsExists(id))
                return HttpNotFound();
            Campaign c = await db.Campaigns.FindAsync(id);
            c.Active = value;
            db.Entry(c).State = EntityState.Modified;
            await db.SaveChangesAsync();
            c = CampaignStatus(c);
            //return Json(c);
            return Content(JsonConvert.SerializeObject(c), "application/json");
            //return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> DeleteCampaign(int id)
        {
            if (!CampaignModelsExists(id))
                return HttpNotFound();
            Campaign c = await db.Campaigns.FindAsync(id);
            db.Campaigns.Remove(c);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [ActionName("Clone")]
        public async Task<ActionResult> CloneCampaign(int id, string name, bool fullCopy)
        {
            if (!CampaignModelsExists(id))
                return new HttpNotFoundResult();
            Campaign campaignModels = await db.Campaigns.FindAsync(id);
            if (!User.IsInRole("Admin"))
                campaignModels = campaignModels.AdvertiserId == UserId ? campaignModels : null;
            if (campaignModels == null)
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Not authorized to clone campaign");

            campaignModels.Name = name;
            campaignModels.CreatedBy = UserId;
            campaignModels.AdvertiserId = UserId;
            db.Campaigns.Add(campaignModels);
            await db.SaveChangesAsync();

            if (fullCopy)
            {
                var ADs = db.AdGroups.Where(r => r.Campaign.Id == id);
                ADs.Include(r => r.Keywords).Include(r => r.Domains).Include(r => r.Products).Include(r => r.Tiers);
                var adGroups = ADs.ToList();
                campaignModels.AdGroups = adGroups;
                foreach (AdGroup ag in adGroups)
                {
                    int originalId = ag.Id;
                    ag.CampaignId = campaignModels.Id;
                    List<Keyword> keywords = new List<Keyword>();
                    List<Domain> domains = new List<Domain>();
                    foreach (var p in ag.Products)
                    {
                        db.Entry(p).State = System.Data.Entity.EntityState.Unchanged;
                    }
                    foreach (var t in ag.Tiers)
                    {
                        db.Entry(t).State = System.Data.Entity.EntityState.Unchanged;
                    }
                    foreach (var k in ag.Keywords)
                    {
                        keywords.Add(new Keyword { Active = k.Active, BidPrice = k.BidPrice, MatchType = k.MatchType, Negative = k.Negative, OverrideUrl = k.OverrideUrl, Text = k.Text });
                    }
                    foreach (var d in ag.Domains)
                    {
                        domains.Add(new Domain { Active = d.Active, Banned = d.Banned, BidPrice = d.BidPrice, Text = d.Text });
                    }
                    ag.Keywords = keywords;
                    ag.Domains = domains;
                    db.AdGroups.Add(ag);
                    await db.SaveChangesAsync();
                    int newId = ag.Id;
                    var ads = db.Ads.Where(r => r.AdGroup.Id == originalId);
                    if (ads.Count() > 0)
                    {
                        foreach (var a in ads)
                        {
                            a.AdGroupId = newId;
                            db.Ads.Add(a);
                        }
                        await db.SaveChangesAsync();
                    }

                }

            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        [ActionName("Wizard")]
        public async Task<ActionResult> PostCampaignWizard(Campaign campaign, AdGroup adGroup, Ad ad)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(" | ", ModelState.Values
                                            .SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage)));
            }
            List<Keyword> keywordList = new List<Keyword>();
            List<Domain> domainList = new List<Domain>();
            if (!string.IsNullOrWhiteSpace(adGroup.VMKeywords))
            {
                string[] lines = adGroup.VMKeywords.Split(Environment.NewLine.ToCharArray());
                foreach (string line in lines)
                {
                    string[] parts = line.Split("|".ToCharArray());
                    //ignore line if invalid format
                    if (parts.Length == 1 || parts.Length == 3)
                    {
                        string matchType = string.Empty;
                        string word = parts[0].Trim();
                        string url = string.Empty;
                        string w = RemoveSpecialCharactersFromText(word);
                        if (word.StartsWith("\"") && word.EndsWith("\""))
                        {
                            matchType = "Exact";
                        }
                        if (word.StartsWith("[") && word.EndsWith("]"))
                        {
                            matchType = "Phrase";
                        }
                        if (string.IsNullOrWhiteSpace(matchType))
                            matchType = "Broad";
                        if (parts.Length > 1)
                        {
                            url = parts[2].Trim();
                            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                                url = string.Empty;
                            decimal bidPrice = 0;
                            if (decimal.TryParse(parts[1].Trim(), out bidPrice))
                            {
                                if (bidPrice <= 0)
                                    keywordList.Add(new Keyword { Id = -1, MatchType = matchType, Negative = false, Text = w, OverrideUrl = url, Active = true });
                                else
                                    keywordList.Add(new Keyword { Id = -1, MatchType = matchType, Negative = false, Text = w, OverrideUrl = url, BidPrice = bidPrice, Active = true });
                            }
                            else
                                keywordList.Add(new Keyword { Id = -1, MatchType = matchType, Negative = false, Text = w, OverrideUrl = url, Active = true });
                        }
                        else
                            keywordList.Add(new Keyword { Id = -1, MatchType = matchType, Negative = false, Text = w, Active = true });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(adGroup.VMNegativeKeywords))
            {
                string[] keywords = adGroup.VMNegativeKeywords.Split(Environment.NewLine.ToCharArray());

                foreach (string word in keywords)
                {
                    string matchType = string.Empty;
                    string w = RemoveSpecialCharactersFromText(word);
                    if (word.StartsWith("\"") && word.EndsWith("\""))
                    {
                        matchType = "Exact";
                    }
                    if (word.StartsWith("[") && word.EndsWith("]"))
                    {
                        matchType = "Phrase";
                    }
                    if (string.IsNullOrWhiteSpace(matchType))
                        matchType = "Broad";
                    keywordList.Add(new Keyword { MatchType = matchType, Negative = true, Text = w, Active = true });
                }
            }
            if (!string.IsNullOrWhiteSpace(adGroup.VMDomains))
            {
                string[] lines = adGroup.VMDomains.Split(Environment.NewLine.ToCharArray());
                foreach (string line in lines)
                {
                    string[] parts = line.Split("|".ToCharArray());
                    if (parts.Length == 1 || parts.Length == 2)
                    {
                        string domain = parts[0].Trim();
                        if (Uri.CheckHostName(parts[0].Trim()) != UriHostNameType.Unknown)
                        {
                            if (parts.Length == 1)
                                domainList.Add(new Domain { Text = domain, Banned = false, Active = true });
                            else
                            {
                                decimal bidPrice = 0;
                                if (decimal.TryParse(parts[1].Trim(), out bidPrice))
                                {
                                    if (bidPrice <= 0)
                                        domainList.Add(new Domain { Text = domain, Banned = false, Active = true });
                                    else
                                        domainList.Add(new Domain { Text = domain, Banned = false, BidPrice = bidPrice, Active = true });
                                }
                                else
                                    domainList.Add(new Domain { Text = domain, Banned = false, Active = true });
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(adGroup.VMBannedDomains))
            {
                string[] domains = adGroup.VMBannedDomains.Split(Environment.NewLine.ToCharArray());
                foreach (string d in domains)
                {
                    if (Uri.CheckHostName(d.Trim()) != UriHostNameType.Unknown)
                        domainList.Add(new Domain { Text = d.Trim(), Banned = true, Active = true });
                }
            }
            if (keywordList.Count > 0)
                adGroup.Keywords = keywordList;
            if (domainList.Count > 0)
                adGroup.Domains = domainList;
            adGroup.Ads.Add(ad);
            if (!User.IsInRole("Admin"))
            {
                Tier tier = db.Tiers.SingleOrDefault(r => r.Default);
                adGroup.Tiers.Add(tier);
            }                
            campaign.AdGroups.Add(adGroup);
            foreach (var p in adGroup.Products)
            {
                db.Entry(p).State = System.Data.Entity.EntityState.Unchanged;
            }
            foreach (var t in adGroup.Tiers)
            {
                db.Entry(t).State = System.Data.Entity.EntityState.Unchanged;
            }
            campaign.CreatedBy = UserId;
            if (!User.IsInRole("Admin"))
                campaign.AdvertiserId = UserId;
            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        private Campaign CampaignStatus(Campaign campaign)
        {
            var advertiser = context.Users.SingleOrDefault(r => r.Id == campaign.AdvertiserId);
            if (advertiser == null)
            {
                campaign.Status = "Inactive";
                campaign.Reason = "Advertiser not found";
                return campaign;
            }

            if (!advertiser.Active)
            {
                campaign.Status = "Inactive";
                campaign.Reason = "Advertiser account is inactive";
                return campaign;
            }
            if (advertiser.PaymentTerm == "PrePay" && advertiser.Balance <= 0)
            {
                campaign.Status = "Inactive";
                campaign.Reason = "Advertiser has not enough fund";
                return campaign;
            }
            if (advertiser.PaymentTerm == "Invoice" && advertiser.Balance <= advertiser.CreditLimit)
            {
                campaign.Status = "Inactive";
                campaign.Reason = "Advertiser's credit limit is reached";
                return campaign;
            }
            if (!campaign.Active)
            {
                campaign.Status = "Inactive";
                campaign.Reason = "Campaign is inactive";
                return campaign;
            }
            var adgroups = db.AdGroups.Where(r => r.CampaignId == campaign.Id);
            if (adgroups.Count() == 0)
            {
                campaign.Status = "Not delivering";
                campaign.Reason = "Campaign does not have any ad group";
                return campaign;
            }
            if (adgroups.Where(r => !r.Active).Count() == adgroups.Count())
            {
                campaign.Status = "Not delivering";
                campaign.Reason = "All ad groups in this campaign are inactive";
                return campaign;
            }
            var ads = db.Ads.Where(r => r.AdGroup.CampaignId == campaign.Id);
            if (ads.Count() == 0)
            {
                campaign.Status = "Not delivering";
                campaign.Reason = "Campaign does not have any ad group";
                return campaign;
            }
            if (ads.Where(r => !r.Active).Count() == ads.Count())
            {
                campaign.Status = "Not delivering";
                campaign.Reason = "All ads in this campaign are inactive";
                return campaign;
            }
            if (string.IsNullOrWhiteSpace(campaign.Status))
            {
                campaign.Status = "Active";
                campaign.Reason = string.Empty;
                return campaign;
            }
            return campaign;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CampaignModelsExists(int id)
        {
            return db.Campaigns.Count(e => e.Id == id) > 0;
        }
        private string RemoveSpecialCharactersFromText(string text)
        {
            return text.Replace("\"", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Trim();
        }
    }
}