using AdCenter.Context;
using AdCenter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace AdCenter.Controllers
{
    public class SearchController : Controller
    {
        private AdCenterDbContext db = new AdCenterDbContext();
        private string UrlPath
        {
            get
            {
                string url = Request.Url.AbsoluteUri.Substring(0, Request.Url.AbsoluteUri.IndexOf('?'));
                return (Request.Url.AbsolutePath == "/" ? Request.Url.AbsoluteUri : url.Replace(Request.Url.AbsolutePath, "")) + Request.ApplicationPath;
            }
        }
        // GET: Search
        public ActionResult Index(string product, string tier, string keyword, string ref_url, string user_agent, string source_id, string ip_address, string limit, string domain)
        {
            int product_id = 0;
            int tier_id = 0;
            string ip = HttpContext.Request.UserHostAddress;
            string requestId = Guid.NewGuid().ToString();
            List<string> errors = new List<string>();
            XDocument xml;
            if (!int.TryParse(product, out product_id))            
                errors.Add("Invalid product");             
            if (!int.TryParse(tier, out tier_id))
                errors.Add("Invalid tier");
            if(errors.Count > 0)
            {
                xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("Errors",
                errors.Select(i=> new XElement("Error",i))));

                return Content(xml.Declaration.ToString() + "\r\n" + xml.ToString(), "text/xml");
            }
            List<int> activeAdv = db.Database.SqlQuery<int>("SELECT Id FROM USER WHERE Active = TRUE AND ((PaymentTerm = 'PrePay' AND Balance > 0) OR (PaymentTerm = 'Invoice' AND Balance > CreditLimit))").ToList();
            List<AdGroup> result = db.AdGroups.Where(r => r.Campaign.Active && activeAdv.Any(a => a == r.Campaign.AdvertiserId) && r.Active && r.Ads.Count > 0 && r.Products.Any(p => p.Id == product_id) && r.Tiers.Any(t => t.Id == tier_id)).ToList();
            if(!string.IsNullOrWhiteSpace(keyword))
                result = result.Where(r => r.Keywords.Where(k => k.Text.Contains(keyword) && !k.Negative).Count() > 0).ToList();
            string currentDay = DateTime.Now.ToString("ddd").ToLower();
            string currentHour = int.Parse(DateTime.Now.ToString("HH")).ToString();
            string currentDayHour = currentDay + ":" + currentHour + ":";
            List<AdGroup> filtered = new List<AdGroup>();
            foreach (AdGroup ag in result)
            {
                string dayParts = ag.DayParts;
                int dayIndex = dayParts.IndexOf(currentDayHour);
                string searchDayPart = dayParts.Substring(dayIndex, currentDayHour.Length + 1);
                searchDayPart = searchDayPart.Replace(currentDayHour, string.Empty);
                if (searchDayPart.ToLower().Equals("a"))
                    filtered.Add(ag);
            }
            if (filtered.Count == 0)
            {
                xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("ad_offer",string.Empty));
                db.SearchLog.Add(new SearchLog { DateCreatedGMT = DateTime.Now.ToUniversalTime(), Error = xml.Declaration.ToString() + "\r\n" + xml.ToString(), RequestId = requestId, IP = ip });
                db.SaveChanges();
                return Content(xml.Declaration.ToString() + "\r\n" + xml.ToString(), "text/xml");
            }
            result = filtered;
            result = result.OrderByDescending(r => r.Bid).ToList();
            Ad ad = result.FirstOrDefault().Ads.FirstOrDefault();
            string clickurl = ad.Id + "|" + result.FirstOrDefault().Bid + "|" + ad.ClickUrl;
            var bytes = System.Text.Encoding.UTF8.GetBytes(clickurl);
            string convertedClickUrl = Convert.ToBase64String(bytes);
            string ad_url = UrlPath.TrimEnd('/') + "/Accept?pid=1&p1=" + convertedClickUrl;
            int adId = result.FirstOrDefault().Ads.FirstOrDefault().Id;
            xml = new XDocument(
                new XDeclaration("1.0","UTF-8",null),
                new XElement("ad_offer",
                new XElement("cpc", result.FirstOrDefault().Bid), 
                new XElement("request_id", new XCData(requestId)),
                new XElement("request_time", DateTime.Now.ToString("yyyyMMddHHmmssffff")),
                new XElement("accepted_ad_url", new XCData(ad_url))));
            string xmlString = xml.Declaration.ToString() + "\r\n" + xml.ToString();
            db.SearchLog.Add(new SearchLog { DateCreatedGMT = DateTime.Now.ToUniversalTime(), Output = xmlString, RequestId = requestId, IP = ip,AdId=adId });
            db.SaveChanges();
            return Content(xmlString,"text/xml");
        }
    }
}