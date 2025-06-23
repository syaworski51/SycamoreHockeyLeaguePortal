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
                .Select(s => s.Date)
                .Distinct();

            var firstDayOfSeason = dates.Min();

            bool seasonHasStarted = currentDate.CompareTo(firstDayOfSeason) >= 0;
            var rangeStart = seasonHasStarted ? currentDate : firstDayOfSeason;
            var rangeEnd = rangeStart.AddDays(13);

            var upcomingGames = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(g => g.Date.Date >= rangeStart.Date &&
                            g.Date.Date <= rangeEnd.Date)
                .OrderBy(g => g.Date.Date)
                .ThenBy(g => g.GameIndex);
            ViewBag.UpcomingGames = await upcomingGames.AsNoTracking().ToListAsync();

            var todaysGames = upcomingGames.Where(s => s.Date.Date == currentDate.Date);
            ViewBag.TodaysGames = await todaysGames.AsNoTracking().ToListAsync();

            var standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.DivisionRanking);

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
