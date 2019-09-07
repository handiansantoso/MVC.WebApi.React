using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace AdCenter.Models
{
    [Table("Tier")]
    public class Tier
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Column(TypeName = "varchar")]
        public string Name { get; set; }
        public bool Default { get; set; }
        [JsonIgnore]
        public virtual ICollection<AdGroup> AdGroups { get; set; }
    }
}