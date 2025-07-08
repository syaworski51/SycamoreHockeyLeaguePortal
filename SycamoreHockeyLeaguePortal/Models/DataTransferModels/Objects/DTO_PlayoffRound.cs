namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_PlayoffRound
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public bool MatchupsConfirmed { get; set; }
    }
}
