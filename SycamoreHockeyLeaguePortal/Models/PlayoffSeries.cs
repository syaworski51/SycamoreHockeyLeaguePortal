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
        public DateTime? StartDate { get; set; }

        [Display(Name = "Index")]
        public string Index { get; set; }

        public int? Seed1 { get; set; }

        [ForeignKey(nameof(Team1))]
        public Guid? Team1Id { get; set; }

        [Display(Name = "Team 1")]
        public Team? Team1 { get; set; }

        [Display(Name = "Team 1 Wins")]
        public int Team1Wins { get; set; }

        [Display(Name = "Team 1 Placeholder")]
        public string Team1Placeholder { get; set; }

        public int? Seed2 { get; set; }

        [ForeignKey(nameof(Team2))]
        public Guid? Team2Id { get; set; }

        [Display(Name = "Team 2")]
        public Team? Team2 { get; set; }

        [Display(Name = "Team 2 Wins")]
        public int Team2Wins { get; set; }

        [Display(Name = "Team 2 Placeholder")]
        public string Team2Placeholder { get; set; }

        [Display(Name = "Confirmed?")]
        public bool IsConfirmed { get; set; }

        [Display(Name = "Ended?")]
        public bool HasEnded { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Series Score")]
        public string SeriesScoreString
        {
            get
            {
                if (Team1Wins != Team2Wins)
                {
                    string team1Name = Team1!.AlternateName == null ? Team1.City : Team1.AlternateName;
                    string team2Name = Team2!.AlternateName == null ? Team2.City : Team2.AlternateName;
                    string leader = (Team1Wins > Team2Wins) ? team1Name : team2Name;
                    string verb = (Status == "In progress") ? "leads" : "wins";
                    int leadingWins = Math.Max(Team1Wins, Team2Wins);
                    int trailingWins = Math.Min(Team1Wins, Team2Wins);

                    if (leader.StartsWith("NY"))
                        verb = verb.Remove(verb.Length - 1);

                    return $"{leader} {verb} series {leadingWins}-{trailingWins}";
                }

                return $"Series is tied {Team1Wins}-{Team2Wins}";
            }
        }

        [Display(Name = "Series")]
        public string ShortSeriesScoreString
        {
            get
            {
                if (Team1Wins != Team2Wins)
                {
                    string team1Code = Team1!.Code;
                    string team2Code = Team2!.Code;
                    string leader = (Team1Wins > Team2Wins) ? team1Code : team2Code;
                    string verb = (Status == "In progress") ? "leads" : "wins";
                    int leadingWins = Math.Max(Team1Wins, Team2Wins);
                    int trailingWins = Math.Min(Team1Wins, Team2Wins);

                    return $"{leader} {verb} {leadingWins}-{trailingWins}";
                }

                return $"Series tied {Team1Wins}-{Team2Wins}";
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
