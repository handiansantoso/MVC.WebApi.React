using AdCenter.Context;
using AdCenter.Models;
using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AdCenter.Controllers
{
    public class AcceptController : Controller
    {
        private AdCenterDbContext db = new AdCenterDbContext();
        // GET: Accept
        public ActionResult Index(string pid, string p1)
        {
            var data = Convert.FromBase64String(p1);
            string decodedString = Encoding.UTF8.GetString(data);
            string[] parts = decodedString.Split('|');
            int id = int.Parse(parts[0]);
            Ad ad = db.Ads.Include(r => r.AdGroup.Campaign).SingleOrDefault(r => r.Id == id);
            int advertiserId = ad.AdGroup.Campaign.AdvertiserId;
            decimal bid = decimal.Parse(parts[1]);
            string url = parts[2];
            db.ImpressionLog.Add(new ImpressionLog { AdId = id, AdvertiserId = advertiserId, BidPrice = bid, DateCreatedGMT = DateTime.Now.ToUniversalTime() });

            db.SaveChanges();
            return Redirect(url);
        }
    }
}