using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class Standings_NHL
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        public Season Season { get; set; }

        [ForeignKey(nameof(Conference))]
        public Guid? ConferenceId { get; set; }

        public Conference? Conference { get; set; }

        [ForeignKey(nameof(Division))]
        public Guid? DivisionId { get; set; }

        public Division? Division { get; set; }

        public int DivisionRanking { get; set; }

        public int ConferenceRanking { get; set; }

        public int PlayoffRanking { get; set; }

        public int LeagueRanking { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }

        [Display(Name = "GP")]
        public int GamesPlayed { get; set; }

        [Display(Name = "W")]
        public int Wins { get; set; }

        [Display(Name = "L")]
        public int Losses { get; set; }

        [Display(Name = "OTL")]
        public int OTLosses { get; set; }

        [Display(Name = "W-L-OTL")]
        public string Record => $"{Wins}-{Losses}-{OTLosses}";

        [Display(Name = "Pts.")]
        public int Points { get; set; }

        [Display(Name = "P%")]
        [Column(TypeName = "decimal(4, 3)")]
        public decimal PointsPct { get; set; }

        [Display(Name = "PC")]
        public int PointsCeiling { get; set; }

        [Display(Name = "RW")]
        public int RegulationWins { get; set; }

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

        public int DivisionGamesPlayed { get; set; }

        public int DivisionWins { get; set; }

        public int DivisionLosses { get; set; }

        public int DivisionOTLosses { get; set; }

        [Display(Name = "vs. Div.")]
        public string DivisionRecord => $"{DivisionWins}-{DivisionLosses}-{DivisionOTLosses}";

        public int DivisionPoints { get; set; }

        [Column(TypeName = "decimal(4, 3)")]
        public decimal DivisionPointsPct { get; set; }

        public int ConferenceGamesPlayed { get; set; }

        public int ConferenceWins { get; set; }

        public int ConferenceLosses { get; set; }

        public int ConferenceOTLosses { get; set; }

        [Display(Name = "vs. Conf.")]
        public string ConferenceRecord => $"{ConferenceWins}-{ConferenceLosses}-{ConferenceOTLosses}";

        public int ConferencePoints { get; set; }

        [Column(TypeName = "decimal(4, 3)")]
        public decimal ConferencePointsPct { get; set; }

        public int InterConfGamesPlayed { get; set; }

        public int InterConfWins { get; set; }

        public int InterConfLosses { get; set; }

        public int InterConfOTLosses { get; set; }

        [Display(Name = "East/West")]
        public string InterConfRecord => $"{InterConfWins}-{InterConfLosses}-{InterConfOTLosses}";

        public int InterConfPoints { get; set; }

        [Column(TypeName = "decimal(4, 3)")]
        public decimal InterConfPointsPct { get; set; }

        public int Last10GamesPlayed { get; set; }

        public int Last10Wins { get; set; }

        public int Last10Losses { get; set; }

        public int Last10OTLosses { get; set; }

        public string Last10Record => $"{Last10Wins}-{Last10Losses}-{Last10OTLosses}";

        public int Last10Points { get; set; }

        [Column(TypeName = "decimal(4, 3)")]
        public decimal Last10PointsPct { get; set; }
    }
}
