using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AdCenter.Models
{
    [Table("Ad")]
    public class Ad
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "varchar")]
        [Required]
        public string Name { get; set; }
        public bool Active { get; set; }
        [NotMapped]
        public string Status { get; set; }
        [NotMapped]
        public string Reason { get; set; }
        [Required]
        [Url]
        [Column(TypeName = "varchar")]
        public string ClickUrl { get; set; }
        [Column(TypeName = "varchar")]
        public string ConversionDomain { get; set; }
        [Column(TypeName = "varchar")]
        public string ThirdPartyImpressionTracking { get; set; }
        public AdGroup AdGroup { get; set; }
        [ForeignKey("AdGroup")]
        public int AdGroupId { get; set; }
    }
}