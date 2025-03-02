using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("PlayoffRounds")]
    public class PlayoffRound
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [Display(Name = "Index")]
        public int Index { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "All Matchups Confirmed?")]
        public bool MatchupsConfirmed { get; set; }
    }
}
