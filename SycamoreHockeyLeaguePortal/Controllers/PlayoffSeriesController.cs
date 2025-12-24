using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using NuGet.ProjectModel;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.ConstantGroups;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;
using SycamoreHockeyLeaguePortal.Models.InputForms;
using SycamoreHockeyLeaguePortal.Services;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PlayoffSeriesController : Controller
    {
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;
        private readonly LiveDbSyncService _syncService;
        private readonly DTOConverter _dtoConverter;

        public PlayoffSeriesController(ApplicationDbContext local,
                                       LiveDbContext live,
                                       LiveDbSyncService syncService,
                                       DTOConverter dtoConverter)
        {
            _localContext = local;
            _liveContext = live;
            _syncService = syncService;
            _dtoConverter = dtoConverter;
        }

        // GET: PlayoffSeries
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? season, int? round, string? team1, string? team2)
        {
            var matchups = await _localContext.RankedPlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Matchup)
                .OrderBy(m => m.OverallRanking)
                .ToListAsync();

            var seasons = matchups
                .OrderByDescending(s => s.Season.Year)
                .Select(m => m.Season.Year)
                .Distinct();
            ViewBag.Seasons = new SelectList(seasons);

            var rounds = GetPlayoffRounds(season);
            ViewBag.Rounds = new SelectList(
                rounds.Select(p => new { Key = p.Key, Value = p.Value }),
                "Key", "Value"
            );

            var teams = GetPlayoffTeams(season);
            ViewBag.Team1List = new SelectList(teams, "Code", "FullName");
            ViewBag.Team2List = new SelectList(teams, "Code", "FullName");

            if (season != null)
                matchups = matchups
                    .Where(m => m.Season.Year == season)
                    .ToList();

            if (round != null)
            {
                matchups = matchups
                    .Where(m => m.Round.Index == round)
                    .ToList();

                if (season > 2021 & round == 3)
                {
                    var _2021Matchups = matchups.Where(m => m.Season.Year == 2021);
                    foreach (var matchup in _2021Matchups)
                        matchups.Remove(matchup);
                }
            }

            if (team1 != null)
            {
                matchups = matchups
                    .Where(m => m.Matchup.Team1!.Code == team1 || m.Matchup.Team2!.Code == team1)
                    .ToList();

                var t1 = _localContext.Teams.FirstOrDefault(t => t.Code == team1)!;
                var team2List = teams.ToList();
                team2List.Remove(t1);
                ViewBag.Team2List = new SelectList(team2List, "Code", "FullName");
            }

            if (team2 != null)
            {
                if (team1 == null)
                    return RedirectToAction(nameof(Index), new { season, round, team1 = team2 });
                else
                    matchups = matchups
                    .Where(m => m.Matchup.Team1!.Code == team2 || m.Matchup.Team2!.Code == team2)
                    .ToList();
            }

            return View(matchups);
        }

        private Dictionary<int, string> GetPlayoffRounds(int? season)
        {
            Dictionary<int, string> rounds = new()
            {
                { 1, "First Round" },
                { 2, "Second Round" },
                { 3, "Conference Finals" },
                { 4, "Sycamore Cup Final" }
            };

            if (season == 2021)
            {
                rounds[3] = rounds[4];
                rounds.Remove(4);
            }

            return rounds;
        }

        private IEnumerable<Team> GetPlayoffTeams(int? season = null)
        {
            IQueryable<PlayoffSeries> matchups = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.HasEnded && s.Team1 != null && s.Team2 != null)
                .OrderBy(s => s.Season.Year);

            if (season != null)
                matchups = matchups.Where(m => m.Season.Year == season);

            var higherSeeds = matchups
                .Select(m => m.Team1!)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name)
                .Distinct();

            var lowerSeeds = matchups
                .Select(m => m.Team2!)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name)
                .Distinct();

            return higherSeeds.Union(lowerSeeds);
        }

        // GET: PlayoffSeries/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int season, string team1, string team2)
        {
            if (_localContext.PlayoffSeries == null)
                return NotFound();

            var series = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .FirstOrDefault(s => s.Season.Year == season &&
                                     ((s.Team1!.Code == team1 && s.Team2!.Code == team2) ||
                                      (s.Team1!.Code == team2 && s.Team2!.Code == team1)));

            var games = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Type == "Playoffs" && s.Season.Year == season &&
                            ((s.AwayTeam.Code == team1 && s.HomeTeam.Code == team2) ||
                             (s.AwayTeam.Code == team2 && s.HomeTeam.Code == team1)))
                .OrderBy(s => s.Date)
                .ToList();
            ViewBag.Games = games;

            if (series == null)
                return NotFound();

            return View(series);
        }

        // GET: PlayoffSeries/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _localContext.PlayoffSeries == null)
            {
                return NotFound();
            }

            var playoffSeries = await _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (playoffSeries == null)
                return NotFound();

            if (playoffSeries.IsConfirmed)
                return BadRequest();

            var form = new PlayoffSeries_EditForm
            {
                Id = playoffSeries.Id,
                StartDate = playoffSeries.StartDate,
                Team1Id = playoffSeries.Team1Id,
                Team1 = playoffSeries.Team1,
                Team2Id = playoffSeries.Team2Id,
                Team2 = playoffSeries.Team2
            };

            var teams = _localContext.Standings
                .Include(s => s.Team)
                .Where(s => s.Season.Year == playoffSeries.Season.Year && s.PlayoffRanking <= 8)
                .Select(s => s.Team)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Team1Options = new SelectList(teams, "Id", "FullName");
            ViewBag.Team2Options = new SelectList(teams, "Id", "FullName");

            return View(form);
        }

        // POST: PlayoffSeries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PlayoffSeries_EditForm form)
        {
            form.Team1 = await _localContext.Teams.FindAsync(form.Team1Id);
            form.Team2 = await _localContext.Teams.FindAsync(form.Team2Id);

            var series = _localContext.PlayoffSeries
                .Include(s => s.Round)
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefault(s => s.Id == id)!;

            if (series == null)
                return NotFound();

            if (series.IsConfirmed)
                return BadRequest();

            series.StartDate = form.StartDate;
            series.Team1Id = form.Team1Id;
            series.Team1 = form.Team1;
            series.Team2Id = form.Team2Id;
            series.Team2 = form.Team2;

            if (series.Team1 != series.Team2 || (series.Team1 == null && series.Team2 == null))
            {
                var dto = new DTO_PlayoffSeries
                {
                    Id = series.Id,
                    SeasonId = series.SeasonId,
                    RoundId = series.RoundId,
                    StartDate = series.StartDate,
                    Index = series.Index,
                    Team1Id = series.Team1Id,
                    Team1Wins = series.Team1Wins,
                    Team1Placeholder = series.Team1Placeholder,
                    Team2Id = series.Team2Id,
                    Team2Wins = series.Team2Wins,
                    Team2Placeholder = series.Team2Placeholder,
                    Description = series.Description,
                    IsConfirmed = series.IsConfirmed,
                    HasEnded = series.HasEnded
                };

                _localContext.PlayoffSeries.Update(series);
                await _syncService.EditPlayoffMatchupAsync(dto);
                await _localContext.SaveChangesAsync();

                return RedirectToAction("PlayoffBracket", "Standings", new { season = series.Season.Year });
            }

            var teams = _localContext.Standings
                .Include(s => s.Team)
                .Where(s => s.Season.Year == series.Season.Year && s.PlayoffRanking <= 8)
                .Select(s => s.Team)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Team1Options = new SelectList(teams, "Id", "FullName", series.Team1);
            ViewBag.Team2Options = new SelectList(teams, "Id", "FullName", series.Team2);

            return View(form);
        }

        /*private IEnumerable<Team> FilterTeamsByMatchupIndex(int season, string index)
        {
            var standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season && s.PlayoffRanking <= 8)
                .OrderBy(s => s.Conference!.Code)
                .ThenBy(s => s.PlayoffRanking);

            var conferences = standings
                .Select(s => s.Conference)
                .OrderBy(c => c!.Name)
                .ToDictionary(c => c!.Code);

            if (index == "A" || index == "B" || index == "E" || index == "F")
                return standings
                    .Where(s => s.Conference == conferences["WEST"])
                    .Select(s => s.Team);

            if (index == "C" || index == "D" || index == "G" || index == "H")
                return standings
                    .Where(s => s.Conference == conferences["EAST"])
                    .Select(s => s.Team);
        }*/

        public async Task<IActionResult> ConfirmPlayoffMatchup(int season, string index)
        {
            IQueryable<PlayoffSeries> matchups = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Index);

            var matchup = matchups.FirstOrDefault(s => s.Index == index)!;
            matchups = matchups.Where(m => m.Round == matchup.Round);

            if (matchup.IsConfirmed)
                return BadRequest("This matchup has already been confirmed.");

            matchup.IsConfirmed = true;
            _localContext.PlayoffSeries.Update(matchup);
            await _localContext.SaveChangesAsync();

            var schedule = await GenerateSchedule(matchup);

            if (matchup.Round.Index < 4)
                await ReindexPlayoffGames(matchup.Round);

            await _localContext.SaveChangesAsync();

            var matchupDTO = new DTO_PlayoffSeries
            {
                Id = matchup.Id,
                SeasonId = matchup.SeasonId,
                RoundId = matchup.RoundId,
                StartDate = matchup.StartDate,
                Index = matchup.Index,
                Team1Id = matchup.Team1Id,
                Team1Wins = matchup.Team1Wins,
                Team1Placeholder = matchup.Team1Placeholder,
                Team2Id = matchup.Team2Id,
                Team2Wins = matchup.Team2Wins,
                Team2Placeholder = matchup.Team2Placeholder,
                Description = matchup.Description,
                IsConfirmed = matchup.IsConfirmed,
                HasEnded = matchup.HasEnded
            };
            var scheduleDTO = schedule.Select(g => new DTO_Game
            {
                Id = g.Id,
                SeasonId = g.SeasonId,
                Date = g.Date,
                GameIndex = g.GameIndex,
                Type = g.Type,
                PlayoffRoundId = g.PlayoffRoundId,
                PlayoffSeriesId = g.PlayoffSeriesId,
                PlayoffGameIndex = g.PlayoffGameIndex,
                AwayTeamId = g.AwayTeamId,
                HomeTeamId = g.HomeTeamId,
                IsConfirmed = g.IsConfirmed,
                Notes = g.Notes,
                PlayoffSeriesScore = g.PlayoffSeriesScore
            }).ToList();
            await _syncService.ConfirmPlayoffMatchupAsync(matchupDTO, scheduleDTO);

            return RedirectToAction("PlayoffBracket", "Standings", new { season = season });
        }

        private async Task ReindexPlayoffGames(PlayoffRound round)
        {
            var schedule = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.PlayoffSeries)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == round.Season.Year &&
                            s.Type == GameTypes.PLAYOFFS &&
                            s.PlayoffRound == round)
                .OrderBy(s => s.Date)
                .ThenByDescending(s => s.PlayoffSeries!.StartDate)
                .ThenBy(s => s.PlayoffSeries!.Index);

            int index = schedule.Min(s => s.GameIndex);
            foreach (var game in schedule)
            {
                game.GameIndex = index;
                index++;
            }

            await _localContext.SaveChangesAsync();
        }

        public async Task<IActionResult> SetMatchups(int year, int round)
        {
            var season = await _localContext.Seasons.FirstOrDefaultAsync(s => s.Year == year);
            if (season == null)
                return ErrorMessage($"There is no {year} season in the database.");

            var matchups = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == year && s.Round.Index == round)
                .OrderBy(s => s.Index)
                .ToList()!;
            if (matchups == null)
                return ErrorMessage($"There are no matchups for Round {round} of the {year} Sycamore Cup Playoffs.");

            var form = new PlayoffSeries_MatchupsForm { Matchups = matchups };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMatchups(PlayoffSeries_MatchupsForm form)
        {
            await _localContext.SaveChangesAsync();
            return View(form);
        }

        [AllowAnonymous]
        public IActionResult IncorrectHomeIceAdvantageDisclaimer()
        {
            return View();
        }

        /// <summary>
        ///     Generate a schedule for a playoff series.
        /// </summary>
        /// <param name="series">The series to generate a schedule for.</param>
        /// <returns></returns>
        private async Task<List<Game>> GenerateSchedule(PlayoffSeries series)
        {
            // Store games in this array for the purpose of date tracking
            Game[] games = new Game[7];
            
            // Get the start date for the playoff series
            DateTime gameDate = (DateTime)series.StartDate!;

            // For each possible game...
            for (int index = 0; index < games.Length; index++)
            {
                // Store the game index here (Game 1, Game 2,... Game 7)
                int gameIndex = index + 1;

                // Higher seeds host games 1, 2, 5 and 7
                bool team1IsHome = gameIndex == 1 || gameIndex == 2 || gameIndex == 5 || gameIndex == 7;

                // If we are past scheduling game 1...
                if (gameIndex > 1)
                {
                    // Get the date of the previous game and determine the appropriate amount of rest days before this game
                    // according to SHL conventions
                    DateTime previousGameDate = games[index - 1].Date;
                    int restDaysBeforeGame = DetermineRestDaysBeforeGame(series.Round.Index, gameIndex);
                    
                    // Set the date of the current game to the number of rest days + 1 after the previous game
                    gameDate = previousGameDate.AddDays(1 + restDaysBeforeGame);
                }

                // Create a new Game object
                string gameString = $"Game {gameIndex}";
                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    SeasonId = series.SeasonId,
                    Season = series.Season,
                    Date = gameDate,
                    GameIndex = _localContext.Schedule.Max(s => s.GameIndex) + 1,
                    Type = "Playoffs",
                    PlayoffGameIndex = gameIndex,
                    PlayoffRoundId = series.RoundId,
                    PlayoffRound = series.Round,
                    PlayoffSeriesId = series.Id,
                    PlayoffSeries = series,
                    AwayTeamId = (Guid)(team1IsHome ? series.Team2Id : series.Team1Id)!,
                    AwayTeam = (team1IsHome ? series.Team2 : series.Team1)!,
                    HomeTeamId = (Guid)(team1IsHome ? series.Team1Id : series.Team2Id)!,
                    HomeTeam = (team1IsHome ? series.Team1 : series.Team2)!,
                    IsConfirmed = gameIndex <= 4,  // The first 4 games of a best-of-7 series are always confirmed
                    Notes = gameString,
                    PlayoffSeriesScore = gameString
                };

                // Write the game into the current index of the array and add it to the local schedule table
                games[index] = game;
                _localContext.Schedule.Add(game);
            }

            // Convert the schedule array to a list and send it to the LiveDbSyncService, and save the changes locally
            await _localContext.SaveChangesAsync();

            return games.ToList();
        }

        /// <summary>
        ///     Determine the appropriate amount of rest days before a playoff game.
        /// </summary>
        /// <param name="round">The current round.</param>
        /// <param name="game">The game to determine the amount of rest days before.</param>
        /// <returns>The appropriate amount of rest days before the game.</returns>
        private int DetermineRestDaysBeforeGame(int round, int game)
        {
            // Before Game 5 of the Sycamore Cup Final, there should be 2 rest days
            if (round == 4 && game == 5)
                return 2;
            
            // In all rounds, Games 1 and 2 are played back-to-back, and so are Games 3 and 4.
            // In all other cases, there shall be 1 rest day between games.
            bool isBackToBack = game == 2 || game == 4;
            return isBackToBack ? 0 : 1;
        }

        private IActionResult ErrorMessage(string message)
        {
            var errorMessage = new ErrorViewModel { Description = message };
            return View("Error", errorMessage);
        }
    }
}