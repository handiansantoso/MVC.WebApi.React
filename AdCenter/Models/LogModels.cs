using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AdCenter.Models
{
    public class SearchLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateCreatedGMT { get; set; }
        [Column(TypeName = "varchar")]
        public string IP { get; set; }
        [Column(TypeName = "varchar")]
        public string Output { get; set; }
        [Column(TypeName = "varchar")]
        public string Error { get; set; }
        [Column(TypeName = "varchar")]
        public string RequestId { get; set; }
        public int? AdId { get; set; }
    }
    public class ImpressionLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateCreatedGMT { get; set; }
        public int AdId { get; set; }
        public int AdvertiserId { get; set; }
        public decimal BidPrice { get; set; }
    }
}