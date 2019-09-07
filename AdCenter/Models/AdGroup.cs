using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace AdCenter.Models
{
    public enum DurationType
    {
        [Description("Start as soon as approved")]
        StartAsApproved = 1,
        [Description("Custom date range")]
        Custom = 2
    }
    public enum DayPartingType
    {
        [Description("Full Coverage")]
        FullCoverage = 1,
        [Description("Afternoons (Noon to 5pm)")]
        Afternoons = 2,
        [Description("Evenings (6pm to Midnight)")]
        Evenings = 3,
        [Description("Mornings (6am to Noon)")]
        Mornings = 4,
        [Description("Weekdays")]
        Weekdays = 5,
        [Description("Weekends")]
        Weekends = 6,
        [Description("Weekends including Friday")]
        WeekendsIncludingFriday = 7,
        [Description("Custom")]
        Custom = 0
    }
    public enum KeywordTargetingType
    {
        [Description("Run of Network")]
        RunOfNetwork = 1,
        [Description("Targeted")]
        Targeted = 2,
        [Description("Targeted Run of Network")]
        TargetedRunOfNetwork = 3
    }
    [Table("AdGroup")]
    public class AdGroup
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "varchar")]
        [Required]
        public string Name { get; set; }
        public bool Active { get; set; }
        public decimal DailyBudget { get; set; }
        [NotMapped]
        public string Status { get; set; }
        [NotMapped]
        public string Reason { get; set; }
        public decimal Bid { get; set; }
        //public DurationType Duration { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string AdGroupType { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Tier> Tiers { get; set; }
        public int Impressions { get; set; }
        public DayPartingType Dayparting { get; set; }
        public string DaypartingTypeName
        {
            get
            {
                return Enum.GetName(typeof(DayPartingType), Dayparting);
            }
        }
        public string DayParts { get; set; }
        public KeywordTargetingType KeywordTargeting { get; set; }
        public string KeywordTargetingTypeName
        {
            get
            {
                return Enum.GetName(typeof(KeywordTargetingType), KeywordTargeting);
            }
        }
        public virtual ICollection<Keyword> Keywords { get; set; }       
        public virtual ICollection<Domain> Domains { get; set; }
        [NotMapped]
        public string VMKeywords { get; set; }
        [NotMapped]
        public string VMNegativeKeywords { get; set; }
        [NotMapped]
        public string VMDomains { get; set; }
        [NotMapped]
        public string VMBannedDomains { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual ICollection<Ad> Ads { get; set; }
        public Campaign Campaign { get; set; }
        [ForeignKey("Campaign")]
        public int CampaignId { get; set; }

        public AdGroup()
        {
            this.Ads = new List<Ad>();
            this.Tiers = new List<Tier>();
        }
    }

    [Table("Keyword")]
    public class Keyword
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "varchar")]
        [Required]
        public string Text { get; set; }
        [Required]
        public bool Negative { get; set; }
        [Required]
        [Column(TypeName = "varchar")]
        public string MatchType { get; set; }
        public decimal? BidPrice { get; set; }
        [Column(TypeName = "varchar")]
        public string OverrideUrl { get; set; }
        public bool Active { get; set; }
    }

    [Table("Domain")]
    public class Domain
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "varchar")]
        [Required]
        public string Text { get; set; }
        [Required]
        public bool Banned { get; set; }
        public decimal? BidPrice { get; set; }
        public bool Active { get; set; }
    }
}