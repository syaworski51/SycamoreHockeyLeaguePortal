namespace SycamoreHockeyLeaguePortal.Models.ViewModels
{
    public class HomePageViewModel
    {
        public DateTime Date { get; set; }
        public int Season { get; set; }
        public int Round { get; set; }
        public List<Game>? UpcomingGames { get; set; }
        public List<Game>? TodaysGames { get; set; }
        public Dictionary<string, List<Standings>> Standings { get; set; }
    }
}
