namespace SycamoreHockeyLeaguePortal.Models.ViewModels
{
    public class ScheduleViewModel
    {
        public int Season { get; set; }
        public DateTime WeekOf { get; set; }
        public DateTime EndOfWeek => WeekOf.AddDays(6);
        public List<Team> Teams { get; set; }
        public List<DateTime> Dates { get; set; }
        public IQueryable<Game> Games { get; set; }
    }
}
