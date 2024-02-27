using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("StandingsSortOptions")]
    public class StandingsSortOption
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Index")]
        public int Index { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Parameter")]
        public string Parameter { get; set; }

        [Display(Name = "First Year")]
        public int FirstYear { get; set; }

        [Display(Name = "Last Year")]
        public int? LastYear { get; set; }
    }
}
