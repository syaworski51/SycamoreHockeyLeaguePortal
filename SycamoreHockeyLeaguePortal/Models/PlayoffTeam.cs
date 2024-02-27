using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("PlayoffTeams")]
    public class PlayoffTeam
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

        [Display(Name = "Seed #")]
        public int SeedNumber { get; set; }
    }
}
