namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_Alignment
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Guid? ConferenceId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid TeamId { get; set; }
    }
}
