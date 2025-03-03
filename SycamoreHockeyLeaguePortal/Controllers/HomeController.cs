using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;
using System.Diagnostics;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentDate = DateTime.Now;
            ViewBag.CurrentDate = currentDate;

            var season = currentDate.Year;
            ViewBag.Season = season;

            var dates = _context.Schedule
                .Include(s => s.Season)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Date.Date)
                .Select(s => s.Date);
            
            var firstDayOfSeason = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Date)
                .Select(s => s.Date.Date)
                .Min();

            bool seasonHasStarted = currentDate.CompareTo(firstDayOfSeason) >= 0;
            var rangeStart = seasonHasStarted ? currentDate : firstDayOfSeason;
            var rangeEnd = rangeStart.AddDays(13);

            var upcomingGames = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(g => g.Date.Date >= rangeStart &&
                            g.Date.Date <= rangeEnd)
                .OrderBy(g => g.Date.Date)
                .ThenBy(g => g.GameIndex);
            ViewBag.UpcomingGames = await upcomingGames.AsNoTracking().ToListAsync();

            var todaysGames = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Date.Date == currentDate)
                .OrderBy(s => s.GameIndex);
            ViewBag.TodaysGames = await todaysGames.AsNoTracking().ToListAsync();

            var standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .OrderByDescending(s => s.WinPct)
                .ThenByDescending(s => s.Wins)
                .ThenBy(s => s.Losses)
                .ThenByDescending(s => s.RegulationWins)
                .ThenByDescending(s => s.RegPlusOTWins)
                .ThenByDescending(s => s.WinPctVsDivision)
                .ThenByDescending(s => s.WinsVsDivision)
                .ThenBy(s => s.LossesVsDivision)
                .ThenByDescending(s => s.WinPctVsConference)
                .ThenByDescending(s => s.WinsVsConference)
                .ThenBy(s => s.LossesVsConference)
                .ThenByDescending(s => s.InterConfWinPct)
                .ThenByDescending(s => s.InterConfWins)
                .ThenBy(s => s.InterConfLosses)
                .ThenByDescending(s => s.GoalDifferential)
                .ThenByDescending(s => s.GoalsFor)
                .ThenByDescending(s => s.WinPctInLast10Games)
                .ThenByDescending(s => s.WinsInLast10Games)
                .ThenBy(s => s.LossesInLast10Games)
                .ThenByDescending(s => s.Streak)
                .ThenBy(s => s.Team.City)
                .ThenBy(s => s.Team.Name);

            ViewBag.AtlanticStandings = standings
                .Where(s => s.Division!.Code == "AT");
            ViewBag.NortheastStandings = standings
                .Where(s => s.Division!.Code == "NE");
            ViewBag.SoutheastStandings = standings
                .Where(s => s.Division!.Code == "SE");

            ViewBag.CentralStandings = standings
                .Where(s => s.Division!.Code == "CE");
            ViewBag.NorthwestStandings = standings
                .Where(s => s.Division!.Code == "NW");
            ViewBag.PacificStandings = standings
                .Where(s => s.Division!.Code == "PA");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
