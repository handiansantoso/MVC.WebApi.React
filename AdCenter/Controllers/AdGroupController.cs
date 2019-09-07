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
    public class AdGroupController : Controller
    {
        private AdCenterDbContext db = new AdCenterDbContext();
        private ApplicationDbContext context = new ApplicationDbContext();
        private string UrlPath
        {
            get
            {
                return Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "") + Request.ApplicationPath;
            }
        }
        private int UserId
        {
            get
            {
                return int.Parse(User.Identity.GetUserId());
            }
        }
        // GET: AdGroup
        public ActionResult Index(int? id)
        {

            if (User.IsInRole("Admin"))
            {
                var campaignList = db.Campaigns.Select(r => new { r.Id, r.Name }).ToList();
                campaignList.Insert(0, new { Id = -1, Name = "- Select Campaign -" });
                ViewBag.Campaigns = JsonConvert.SerializeObject(campaignList);
            }
            else
            {
                var campaignList = db.Campaigns.Where(r => r.AdvertiserId == UserId).Select(r => new { r.Id, r.Name }).ToList();
                campaignList.Insert(0, new { Id = -1, Name = "- Select Campaign -" });
                ViewBag.Campaigns = JsonConvert.SerializeObject(campaignList);
            }

            //ViewBag.AdGroups = JsonConvert.SerializeObject(db.AdGroups.Include(r => r.Campaign).ToList());
            ViewBag.DayPartingTypes = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<DayPartingType>());
            ViewBag.KeywordTargetingType = JsonConvert.SerializeObject(Helpers.EnumHelper.EnumToDropdownValues<KeywordTargetingType>());
            ViewBag.Products = JsonConvert.SerializeObject(db.Products.ToList());
            ViewBag.Tiers = JsonConvert.SerializeObject(db.Tiers.ToList());
            ViewBag.UriPath = JsonConvert.SerializeObject(UrlPath.TrimEnd('/'));
            ViewBag.CampaignId = id == null ? -1 : id;
            return View();
        }
        [ActionName("AdGroup")]
        [HttpGet]
        public ContentResult GetAdGroups()
        {
            List<AdGroup> adGroups = null;
            if (User.IsInRole("Admin"))
                adGroups = db.AdGroups.Include(r => r.Campaign).Include(r => r.Keywords).Include(r => r.Domains).ToList();
            else
                adGroups = db.AdGroups.Where(r => r.Campaign.AdvertiserId == UserId).Include(r => r.Campaign).Include(r => r.Keywords).Include(r => r.Domains).ToList();
            for (int i = 0; i < adGroups.Count(); i++)
                adGroups[i] = AdGroupStatus(adGroups[i]);
            
            var json = JsonConvert.SerializeObject(adGroups, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            return Content(json, "application/json");
        }
        [ActionName("AdGroup")]
        [HttpPost]
        public async Task<ActionResult> PostAdGroup(AdGroup adGroup)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid model.");
            }
            if (adGroup.Id == -1)
            {
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
                if (!User.IsInRole("Admin"))
                {
                    Tier tier = db.Tiers.SingleOrDefault(r => r.Default);
                    adGroup.Tiers.Add(tier);
                }
                db.AdGroups.Add(adGroup);
                foreach (var p in adGroup.Products)
                {
                    db.Entry(p).State = System.Data.Entity.EntityState.Unchanged;
                }
                foreach (var t in adGroup.Tiers)
                {
                    db.Entry(t).State = System.Data.Entity.EntityState.Unchanged;
                }
            }

            else
            {
                if (!AdGroupExists(adGroup.Id))
                {
                    return new HttpNotFoundResult();
                }
                var adGroupDb = db.AdGroups.Include(c => c.Products).Include(c => c.Tiers).Single(c => c.Id == adGroup.Id);

                db.Entry(adGroupDb).CurrentValues.SetValues(adGroup);

                foreach (var prodDb in adGroupDb.Products.ToList())
                    if (!adGroup.Products.Any(t => t.Id == prodDb.Id))
                        adGroupDb.Products.Remove(prodDb);
                foreach (var prod in adGroup.Products)
                    if (!adGroupDb.Products.Any(t => t.Id == prod.Id))
                    {
                        db.Products.Attach(prod);
                        adGroupDb.Products.Add(prod);
                    }
                foreach (var tierDb in adGroupDb.Tiers.ToList())
                    if (!adGroup.Tiers.Any(t => t.Id == tierDb.Id))
                        adGroupDb.Tiers.Remove(tierDb);
                foreach (var tier in adGroup.Tiers)
                    if (!adGroupDb.Tiers.Any(t => t.Id == tier.Id))
                    {
                        db.Tiers.Attach(tier);
                        adGroupDb.Tiers.Add(tier);
                    }
            }


            await db.SaveChangesAsync();

            return new HttpStatusCodeResult(200); //CreatedAtRoute("DefaultApi", new { id = adGroup.Id }, adGroup);
        }
        [ActionName("Keywords")]
        [HttpPost]
        public async Task<ActionResult> PostAdGroupKeywords(int id, string keywords, string negativeKeywords)
        {
            AdGroup adGroup = await db.AdGroups.FindAsync(id);
            if (adGroup == null)
            {
                return HttpNotFound();
            }
            List<Keyword> keywordList = new List<Keyword>();

            if (!string.IsNullOrWhiteSpace(keywords))
            {
                string[] lines = keywords.Split(Environment.NewLine.ToCharArray());
                foreach (string line in lines)
                {
                    string[] parts = line.Split("|".ToCharArray());
                    //ignore line if invalid format
                    if (parts.Length == 1 || parts.Length == 3)
                    {
                        string matchType = string.Empty;
                        string word = parts[0];
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

            if (!string.IsNullOrWhiteSpace(negativeKeywords))
            {
                string[] keys = negativeKeywords.Split(Environment.NewLine.ToCharArray());

                foreach (string word in keys)
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
            foreach (Keyword word in keywordList)
            {
                if (adGroup.Keywords.Count(r => r.Negative == word.Negative && r.Text.Equals(word.Text) && r.MatchType.Equals(word.MatchType)) == 0)
                {
                    adGroup.Keywords.Add(new Keyword { MatchType = word.MatchType, Text = word.Text, Negative = word.Negative, BidPrice = word.BidPrice, OverrideUrl = word.OverrideUrl, Active = word.Active });
                }
            }
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [ActionName("Domains")]
        [HttpPost]
        public async Task<ActionResult> PostAdGroupDomains(int id, string domains, string bannedDomains)
        {
            AdGroup adGroup = await db.AdGroups.FindAsync(id);
            if (adGroup == null)
            {
                return HttpNotFound();
            }
            List<Domain> domainList = new List<Domain>();

            if (!string.IsNullOrWhiteSpace(domains))
            {
                string[] lines = domains.Split(Environment.NewLine.ToCharArray());
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

            if (!string.IsNullOrWhiteSpace(bannedDomains))
            {
                string[] domainArr = bannedDomains.Split(Environment.NewLine.ToCharArray());

                foreach (string d in domainArr)
                {
                    if (Uri.CheckHostName(d.Trim()) != UriHostNameType.Unknown)
                        domainList.Add(new Domain { Text = d.Trim(), Banned = true, Active = true });
                }
            }
            foreach (Domain domain in domainList)
            {
                if (adGroup.Domains.Count(r => r.Banned == domain.Banned && r.Text.Equals(domain.Text)) == 0)
                {
                    adGroup.Domains.Add(new Domain { Text = domain.Text, Banned = domain.Banned, BidPrice = domain.BidPrice, Active = domain.Active });
                }
            }
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [ActionName("AdGroupById")]
        [HttpGet]
        public ContentResult GetAdGroupById(int id)
        {
            var json = JsonConvert.SerializeObject(db.AdGroups.Where(r => r.Id == id).Include(r => r.Campaign).Include(r => r.Keywords).Include(r => r.Domains).SingleOrDefault(), new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<ActionResult> UpdateKeyword(Keyword keyword)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid model.");
            }
            int id = keyword.Id;
            db.Entry(keyword).State = EntityState.Modified;
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KeywordExists(id))
                {
                    return HttpNotFound();
                }
                else
                {
                    throw;
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateDomain(Domain domain)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid model.");
            }
            int id = domain.Id;
            db.Entry(domain).State = EntityState.Modified;
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KeywordExists(id))
                {
                    return HttpNotFound();
                }
                else
                {
                    throw;
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteAdGroup(int id)
        {
            if (!AdGroupExists(id))
            {
                return new HttpNotFoundResult();
            }
            AdGroup ag = await db.AdGroups.FindAsync(id);
            db.AdGroups.Remove(ag);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> DeleteKeyword(int id)
        {
            Keyword keyword = await db.Keywords.FindAsync(id);
            if (keyword == null)
            {
                return HttpNotFound();
            }
            db.Keywords.Remove(keyword);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> DeleteDomain(int id)
        {
            Domain domain = await db.Domains.FindAsync(id);
            if (domain == null)
            {
                return HttpNotFound();
            }
            db.Domains.Remove(domain);
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateStatus(string type, int id, bool value)
        {
            switch (type)
            {
                case "adGroup":
                    if (!AdGroupExists(id))
                        return HttpNotFound();
                    AdGroup ag = await db.AdGroups.FindAsync(id);
                    ag.Active = value;
                    db.Entry(ag).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    ag = AdGroupStatus(ag);
                    return Content(JsonConvert.SerializeObject(ag), "application/json");
                case "keyword":
                    Keyword keyword = await db.Keywords.FindAsync(id);
                    if (keyword == null)
                        return HttpNotFound();
                    keyword.Active = value;
                    db.Entry(keyword).State = EntityState.Modified;
                    break;
                case "domain":
                    Domain domain = await db.Domains.FindAsync(id);
                    if (domain == null)
                        return HttpNotFound();
                    domain.Active = value;
                    db.Entry(domain).State = EntityState.Modified;
                    break;
            }
            await db.SaveChangesAsync();
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        [HttpPost]
        [ActionName("Clone")]
        public async Task<ActionResult> CloneAdGroup(int id, string name, int campaignId, bool fullCopy)
        {
            if (!AdGroupExists(id))
                return HttpNotFound();
            AdGroup ag = db.AdGroups.Where(r=>r.Id == id).Include(r=>r.Keywords).Include(r=>r.Domains).Include(r=>r.Products).Include(r=>r.Tiers).SingleOrDefault();
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
            ag.Name = name;
            ag.CampaignId = campaignId;
            db.AdGroups.Add(ag);
            await db.SaveChangesAsync();
            if (fullCopy)
            {
                int newId = ag.Id;
                var ads = db.Ads.Where(r => r.AdGroup.Id == id);
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
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        private AdGroup AdGroupStatus(AdGroup ag)
        {
            var campaign = db.Campaigns.SingleOrDefault(r => r.Id == ag.CampaignId);
            var advertiser = context.Users.SingleOrDefault(r => r.Id == campaign.AdvertiserId);
            if (advertiser == null)
            {
                ag.Status = "Inactive";
                ag.Reason = "Advertiser not found";
                return ag;
            }

            if (!advertiser.Active)
            {
                ag.Status = "Inactive";
                ag.Reason = "Advertiser account is inactive";
                return ag;
            }
            if (advertiser.PaymentTerm == "PrePay" && advertiser.Balance <= 0)
            {
                ag.Status = "Inactive";
                ag.Reason = "Advertiser has not enough fund";
                return ag;
            }
            if (advertiser.PaymentTerm == "Invoice" && advertiser.Balance <= advertiser.CreditLimit)
            {
                ag.Status = "Inactive";
                ag.Reason = "Advertiser's credit limit is reached";
                return ag;
            }
            if (!campaign.Active)
            {
                ag.Status = "Inactive";
                ag.Reason = "Campaign is inactive";
                return ag;
            }
            var ads = db.Ads.Where(r => r.AdGroupId == ag.Id);
            if (ads.Count() == 0)
            {
                ag.Status = "Not delivering";
                ag.Reason = "Ad Group does not have any ads";
                return ag;
            }
            if (ads.Where(r => !r.Active).Count() == ads.Count())
            {
                ag.Status = "Not delivering";
                ag.Reason = "All ads in this ad group are inactive";
                return ag;
            }
            if (!ag.Active)
            {
                ag.Status = "Inactive";
                ag.Reason = "Ad Group is inactive";
                return ag;
            }
            if (string.IsNullOrWhiteSpace(ag.Status))
            {
                ag.Status = "Active";
                ag.Reason = string.Empty;
                return ag;
            }
            return ag;
        }
        private string RemoveSpecialCharactersFromText(string text)
        {
            return text.Replace("\"", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Trim();
        }
        private bool AdGroupExists(int id)
        {
            return db.AdGroups.Count(e => e.Id == id) > 0;
        }
        private bool KeywordExists(int id)
        {
            return db.Keywords.Count(e => e.Id == id) > 0;
        }
        private bool DomainExists(int id)
        {
            return db.Domains.Count(e => e.Id == id) > 0;
        }
    }
}