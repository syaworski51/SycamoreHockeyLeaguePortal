namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_HeadToHeadSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Guid Team1Id { get; set; }
        public int Team1Wins { get; set; }
        public int Team1GoalsFor { get; set; }
        public Guid Team2Id { get; set; }
        public int Team2Wins { get; set; }
        public int Team2GoalsFor { get; set; }
    }
}
