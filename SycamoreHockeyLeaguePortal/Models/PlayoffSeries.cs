using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("PlayoffSeries")]
    public class PlayoffSeries
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [ForeignKey(nameof(Round))]
        public Guid RoundId { get; set; }

        [Display(Name = "Round")]
        public PlayoffRound Round { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [ForeignKey(nameof(Team1))]
        public Guid Team1Id { get; set; }

        [Display(Name = "Team 1")]
        public Team Team1 { get; set; }

        [Display(Name = "Team 1 Wins")]
        public int Team1Wins { get; set; }

        [ForeignKey(nameof(Team2))]
        public Guid Team2Id { get; set; }

        [Display(Name = "Team 2")]
        public Team Team2 { get; set; }

        [Display(Name = "Team 2 Wins")]
        public int Team2Wins { get; set; }

        [Display(Name = "Series Score")]
        public string SeriesScoreString
        {
            get
            {
                if (Team1Wins != Team2Wins)
                {
                    string team1Name = Team1.AlternateName == null ? Team1.City : Team1.AlternateName;
                    string team2Name = Team2.AlternateName == null ? Team2.City : Team2.AlternateName;
                    string leader = (Team1Wins > Team2Wins) ? team1Name : team2Name;
                    int leadingScore = Math.Max(Team1Wins, Team2Wins);
                    int trailingScore = Math.Min(Team1Wins, Team2Wins);

                    return $"{leader} leads series {leadingScore}-{trailingScore}";
                }

                return $"Series is tied {Team1Wins}-{Team2Wins}";
            }
        }
        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                if (Team1Wins == 4 || Team2Wins == 4)
                    return "Series over";

                if (Team1Wins > 0 || Team2Wins > 0)
                    return "In progress";

                return "Not started";
            }
        }
    }
}
