using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages
{
    public class DTP_NewSeason
    {
        public Season Season { get; set; }
        public List<DTO_Alignment> Alignments { get; set; }
        public List<DTO_Standings> Standings { get; set; }
        public List<DTO_HeadToHeadSeries> HeadToHeadSeries { get; set; }
        public List<DTO_PlayoffRound> PlayoffRounds { get; set; }
        public List<DTO_PlayoffSeries> PlayoffSeries { get; set; }

        public DTP_NewSeason(Season season)
        {
            Season = season;
            Alignments = new();
            Standings = new();
            HeadToHeadSeries = new();
            PlayoffRounds = new();
            PlayoffSeries = new();
        }
    }
}
