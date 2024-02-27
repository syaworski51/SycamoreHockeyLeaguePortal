using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Champions")]
    public class Champion
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }
    }
}
