namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages
{
    public class DTP_NewSeason
    {
        public Season Season { get; set; }
        public List<Alignment> Alignments { get; set; }
        public List<Standings> Standings { get; set; }
        public List<HeadToHeadSeries> HeadToHeadSeries { get; set; }
        public List<PlayoffRound> PlayoffRounds { get; set; }
        public List<PlayoffSeries> PlayoffSeries { get; set; }
    }
}
