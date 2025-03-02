namespace SycamoreHockeyLeaguePortal.Models.InputForms
{
    public class PlayoffSeries_EditForm
    {
        public Guid Id { get; set; }
        public DateTime? StartDate { get; set; }
        public Guid? Team1Id { get; set; }
        public Team? Team1 { get; set; }
        public Guid? Team2Id { get; set; }
        public Team? Team2 { get; set; }
    }
}
