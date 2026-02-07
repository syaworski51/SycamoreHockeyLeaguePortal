using SycamoreHockeyLeaguePortal.Models.ConstantGroups;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Seasons")]
    public class Season
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Long Name")]
        public string LongName => $"{Year} SHL Season";

        [Display(Name = "Short Name")]
        public string ShortName => $"SHL {Year}";

        [Display(Name = "Games Per Team")]
        public int GamesPerTeam { get; set; }

        public int CurrentPlayoffRound { get; set; }

        public string Status { get; set; }

        public string StandingsFormat { get; set; }

        public int PointsPerRW { get; set; }

        public int PointsPerOTW { get; set; }

        public int PointsPerOTL { get; set; }

        public bool InTestMode => Status == SeasonStatuses.TEST_MODE;

        public bool IsLive => Status == SeasonStatuses.LIVE;

        public bool IsComplete => Status == SeasonStatuses.COMPLETE;
    }
}
