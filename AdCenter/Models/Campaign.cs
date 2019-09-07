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
    public enum DeliveryType : int
    {
        [Description("Standard")]
        Standard = 1,
        [Description("Accelerated")]
        Accelerated = 2
    }
    [Table("Campaign")]
    public class Campaign
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName="varchar")]
        [Required]
        public string Name { get; set; }
        public decimal OverallBudget { get; set; }
        public decimal DailyBudget { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public bool Active { get; set; }
        [NotMapped]
        public string Status { get; set; }
        [NotMapped]
        public string Reason { get; set; }
        public string DeliveryTypeName
        {
            get
            {
                return Enum.GetName(typeof(DeliveryType), DeliveryType);
            }
        }
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual ICollection<AdGroup> AdGroups { get; set; }
        public int CreatedBy { get; set; }
        public int AdvertiserId { get; set; }

        public Campaign()
        {
            this.AdGroups = new List<AdGroup>();
        }
    }
}