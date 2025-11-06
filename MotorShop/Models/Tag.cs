using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public required string Name { get; set; }

        [StringLength(80)]
        public string? Slug { get; set; }

        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
        public string Description { get; internal set; }
    }
}
