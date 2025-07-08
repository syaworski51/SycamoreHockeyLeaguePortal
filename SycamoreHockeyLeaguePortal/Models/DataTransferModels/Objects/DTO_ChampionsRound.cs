namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_ChampionsRound
    {
        public Guid Id { get; set; }
        public Guid ChampionId { get; set; }
        public int RoundIndex { get; set; }
        public Guid OpponentId { get; set; }
        public int SeriesLength { get; set; }
        public int BestOf { get; set; }
    }
}
