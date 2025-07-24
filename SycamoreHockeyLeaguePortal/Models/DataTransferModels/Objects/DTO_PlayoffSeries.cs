namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_PlayoffSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Guid RoundId { get; set; }
        public DateTime? StartDate { get; set; }
        public Guid? Team1Id { get; set; }
        public int Team1Wins { get; set; }
        public string Team1Placeholder { get; set; }
        public Guid? Team2Id { get; set; }
        public int Team2Wins { get; set; }
        public string Team2Placeholder { get; set; }
        public string Index { get; set; }
        public string Description { get; set; }
        public bool IsConfirmed { get; set; }
        public bool HasEnded { get; set; }
    }
}
