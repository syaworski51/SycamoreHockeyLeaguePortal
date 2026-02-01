using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;
using SycamoreHockeyLeaguePortal.Models.ViewModels;
using SycamoreHockeyLeaguePortal.Services;
using System.Diagnostics;
using ZstdSharp.Unsafe;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;
        private readonly LiveDbSyncService _syncService;
        private readonly DTOConverter _dtoConverter;

        public HomeController(ILogger<HomeController> logger,
                              ApplicationDbContext localContext,
                              LiveDbContext liveContext,
                              LiveDbSyncService syncService,
                              DTOConverter dtoConverter)
        {
            _logger = logger;
            _localContext = localContext;
            _liveContext = liveContext;
            _syncService = syncService;
            _dtoConverter = dtoConverter;
        }

        public async Task<IActionResult> Index()
        {
            var currentDate = DateTime.Now;
            ViewBag.CurrentDate = currentDate;

            var season = currentDate.Year;
            ViewBag.Season = season;

            int round = _localContext.Seasons
                .FirstOrDefault(s => s.Year == season)!
                .CurrentPlayoffRound;
            ViewBag.CurrentRound = round;

            var dates = _localContext.Schedule
                .Include(s => s.Season)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Date.Date)
                .Select(s => s.Date)
                .Distinct();

            var firstDayOfSeason = dates.Min();

            bool seasonHasStarted = currentDate.CompareTo(firstDayOfSeason) >= 0;
            var rangeStart = seasonHasStarted ? currentDate : firstDayOfSeason;
            var rangeEnd = rangeStart.AddDays(13);

            var upcomingGames = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(g => g.Date.Date >= rangeStart.Date &&
                            g.Date.Date <= rangeEnd.Date)
                .OrderBy(g => g.Date.Date)
                .ThenBy(g => g.GameIndex)
                .ToList();
            ViewBag.UpcomingGames = upcomingGames;

            var todaysGames = upcomingGames
                .Where(s => s.Date.Date == currentDate.Date)
                .ToList();
            ViewBag.TodaysGames = todaysGames;

            var standings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.LeagueRanking);

            if (standings.All(s => s.ConferenceRanking == 0 && s.LeagueRanking == 0))
            {
                standings = standings
                    .OrderBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
                
                int east = 1, west = 1, league = 1;
                foreach (var team in standings)
                {
                    team.LeagueRanking = league;
                    league++;

                    if (team.Conference!.Code == "EAST")
                    {
                        team.ConferenceRanking = east;
                        east++;
                    }
                    else
                    {
                        team.ConferenceRanking = west;
                        west++;
                    }
                }

                await _localContext.SaveChangesAsync();

                List<DTO_Standings> DTOs = _dtoConverter.ConvertBatchToDTO(standings.ToList());
                await _syncService.UpdateStandingsAsync(season, DTOs);
            }

            Dictionary<string, List<Standings>> standingsDict = new()
            {
                { "EAST", standings.Where(s => s.Conference!.Code == "EAST").ToList() },
                { "WEST", standings.Where(s => s.Conference!.Code == "WEST").ToList() }
            };
            ViewBag.Standings = standingsDict;

            HomePageViewModel viewModel = new()
            {
                Date = currentDate,
                Season = currentDate.Year,
                Round = round,
                UpcomingGames = upcomingGames,
                TodaysGames = todaysGames,
                Standings = standingsDict
            };

            return View(viewModel);
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
