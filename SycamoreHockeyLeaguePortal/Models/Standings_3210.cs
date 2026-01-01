using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class Standings_3210
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        public Season Season { get; set; }

        [ForeignKey(nameof(Conference))]
        public Guid? ConferenceId { get; set; }

        public Conference? Conference { get; set; }

        public int ConferenceRanking { get; set; }

        public int LeagueRanking { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }

        [Display(Name = "GP")]
        public int GamesPlayed { get; set; }

        [Display(Name = "W")]
        public int Wins { get; set; }

        [Display(Name = "OTW")]
        public int OTWins { get; set; }

        [Display(Name = "OTL")]
        public int OTLosses { get; set; }

        [Display(Name = "L")]
        public int Losses { get; set; }

        [Display(Name = "W-OTW-OTL-L")]
        public string Record => $"{Wins}-{OTWins}-{OTLosses}-{Losses}";

        [Display(Name = "Pts.")]
        public int Points { get; set; }

        [Display(Name = "P%")]
        [Column(TypeName = "decimal(4, 3)")]
        public decimal PointsPct { get; set; }

        [Display(Name = "PC")]
        public int PointsCeiling { get; set; }

        [Display(Name = "ROW")]
        public int RegPlusOTWins { get; set; }

        [Display(Name = "Streak")]
        public int Streak { get; set; }

        [Display(Name = "GF")]
        public int GoalsFor { get; set; }

        [Display(Name = "GA")]
        public int GoalsAgainst { get; set; }

        [Display(Name = "GD")]
        public int GoalDifferential { get; set; }

        public int Last10GamesPlayed { get; set; }

        public int Last10Wins { get; set; }

        public int Last10OTWins { get; set; }

        public int Last10OTLosses { get; set; }

        public int Last10Losses { get; set; }

        public int Last10Points { get; set; }

        public string Last10Record => $"{Last10Wins}-{Last10OTWins}-{Last10OTLosses}-{Last10Losses} ({Last10Points})";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal Last10PointsPct { get; set; }
    }
}
