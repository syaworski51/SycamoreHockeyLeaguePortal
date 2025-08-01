﻿using AspNetCoreGeneratedDocument;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.ConstantGroups;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages;
using SycamoreHockeyLeaguePortal.Models.Exceptions;
using SycamoreHockeyLeaguePortal.Models.InputForms;
using SycamoreHockeyLeaguePortal.Models.ViewModels;
using SycamoreHockeyLeaguePortal.Services;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;
        private readonly LiveDbSyncService _syncService;
        private readonly DTOConverter _dtoConverter;
        Random random = new Random();

        private const string DIVISION = "division";
        private const string CONFERENCE = "conference";
        private const string INTER_CONFERENCE = "inter-conference";

        private int SEASON;
        private const int TEAM1 = 0, TEAM2 = 1;

        private readonly IConfigurationSection _secrets;

        public ScheduleController(ApplicationDbContext local, 
                                  LiveDbContext live, 
                                  LiveDbSyncService syncService,
                                  DTOConverter dtoConverter,
                                  IConfiguration config)
        {
            _localContext = local;
            _liveContext = live;
            _syncService = syncService;
            _dtoConverter = dtoConverter;
            _secrets = config.GetSection("Secrets");
        }

        // GET: Schedule
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime weekOf, string? team)
        {
            ViewBag.Team = team;

            ViewBag.WeekOf = weekOf;
            var endOfWeek = weekOf.AddDays(6);
            ViewBag.EndOfWeek = endOfWeek;

            var seasons = await _localContext.Seasons
                .OrderByDescending(s => s.Year)
                .ToListAsync();
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var season = weekOf.Year;

            var teams = await _localContext.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Team.City)
                .ThenBy(a => a.Team.Name)
                .Select(a => a.Team)
                .ToListAsync();
            ViewBag.Teams = new SelectList(teams, "Code", "FullName");

            IQueryable<Game> schedule = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            (s.Date.Date.CompareTo(weekOf) >= 0 &&
                             s.Date.Date.CompareTo(endOfWeek) <= 0))
                .OrderBy(s => s.Date.Date)
                .ThenBy(s => s.GameIndex);

            if (team != null)
            {
                schedule = schedule
                    .Where(s => s.AwayTeam.Code == team ||
                                s.HomeTeam.Code == team);
            }

            var dates = await schedule
                .Select(s => s.Date.Date)
                .Distinct()
                .ToListAsync();

            ScheduleViewModel scheduleViewModel = new ScheduleViewModel
            {
                Season = season,
                WeekOf = weekOf,
                Teams = teams,
                Dates = dates,
                Games = schedule
            };

            return View(scheduleViewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Playoffs(int season, int round, string? team)
        {
            if (season < 2021)
                return ErrorMessage($"Invalid request. There is no {season} season in the database.");

            if (round < 1)
                return ErrorMessage("Invalid request. Round indexes cannot be less than 1.");
            
            if (season == 2021 && round >= 4)
                return RedirectToAction(nameof(Playoffs), new { season = season, round = 3, team = team });

            if (season >= 2022 && round > 4)
                return RedirectToAction(nameof(Playoffs), new { season = season, round = 4, team = team });

            ViewBag.Season = season;
            ViewBag.Team = team;
            ViewBag.Round = round;

            var seasons = _localContext.Seasons
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var rounds = _localContext.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season)
                .OrderBy(r => r.Index);
            ViewBag.Rounds = new SelectList(rounds, "Index", "Name");

            string roundName = rounds
                .Where(r => r.Index == round)
                .Select(r => r.Name)
                .First();
            ViewBag.RoundName = roundName;

            var teams = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.PlayoffRound!.Index == round)
                .Select(s => s.HomeTeam)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Teams = new SelectList(teams, "Code", "FullName");

            IQueryable<Game> playoffs = _localContext.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.PlayoffRound)
                    .Include(s => s.PlayoffSeries)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == GameTypes.PLAYOFFS &&
                                s.PlayoffRound!.Index == round)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);

            if (team != null)
                playoffs = playoffs.Where(s => s.AwayTeam.Code == team || s.HomeTeam.Code == team);

            List<DateTime> dates = await playoffs
                .Select(p => p.Date.Date)
                .Distinct()
                .ToListAsync();
            ViewBag.Dates = dates;

            return View(await playoffs.ToListAsync());
        }

        private async Task LoadFinalScores()
        {
            var matchups = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.HasEnded && s.Team1 != null && s.Team2 != null)
                .OrderBy(s => s.Season.Year)
                .ThenBy(s => s.Index);

            var games = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.PlayoffSeries)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Type == GameTypes.PLAYOFFS && s.IsFinalized)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);

            foreach (var matchup in matchups)
            {
                var schedule = games.Where(s => s.PlayoffSeriesId == matchup.Id).ToList();
                await CalculateFinalScoreForSeriesAsync(matchup, schedule);
            }

            await _localContext.SaveChangesAsync();
        }

        [AllowAnonymous]
        public IActionResult ReturnToPlayoffsSchedule(int season, int round, DateTime date)
        {
            string dateSection = "#" + date.ToString("MMMdd");
            var url = Url.Action("Playoffs", "Schedule", new { season = season, round = round });
            return Redirect(url + dateSection);
        }

        public IActionResult UploadSchedule(int year)
        {
            bool seasonExists = DoesSeasonExist(year);
            if (!seasonExists)
                return ErrorMessage($"There is no {year} season in the database.");

            bool seasonHasSchedule = DoesSeasonHaveSchedule(year);
            if (seasonHasSchedule)
                return ErrorMessage($"The {year} season already has a schedule. You cannot upload another schedule for this season.");

            var form = new Schedule_UploadForm
            {
                Season = year
            };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSchedule(Schedule_UploadForm form)
        {
            bool seasonExists = DoesSeasonExist(form.Season);
            if (!seasonExists)
                return ErrorMessage($"There is no {form.Season} season in the database.");
            
            var season = _localContext.Seasons.FirstOrDefault(s => s.Year == form.Season)!;
            bool seasonHasSchedule = DoesSeasonHaveSchedule(season.Year);
            if (seasonHasSchedule)
                return ErrorMessage($"There is already a schedule for the {form.Season} season.");

            if (form.File != null && form.File.Length > 0)
            {
                DateTime firstDay = DateTime.Now;
                var extension = Path.GetExtension(form.File.FileName).ToLower();
                
                if (extension == ".csv") 
                {
                    List<Game> schedule = new();
                    
                    using (var streamReader = new StreamReader(form.File.OpenReadStream()))
                    using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                    {
                        var games = csvReader.GetRecords<ScheduleCSV>().ToList();
                        firstDay = games.First().Date;

                        int gameIndex = _localContext.Schedule.Max(s => s.GameIndex);
                        int index = 1;
                        foreach (var _game in games)
                        {
                            var awayTeam = _localContext.Teams.FirstOrDefault(t => t.Code == _game.AwayTeam)!;
                            var homeTeam = _localContext.Teams.FirstOrDefault(t => t.Code == _game.HomeTeam)!;

                            var game = new Game
                            {
                                Id = Guid.NewGuid(),
                                SeasonId = season.Id,
                                Season = season,
                                Date = _game.Date,
                                GameIndex = gameIndex + index,
                                Type = GameTypes.REGULAR_SEASON,
                                AwayTeamId = awayTeam.Id,
                                AwayTeam = awayTeam,
                                HomeTeamId = homeTeam.Id,
                                HomeTeam = homeTeam,
                                IsConfirmed = true
                            };
                            _localContext.Schedule.Add(game);
                            schedule.Add(game);

                            index++;
                        }
                    }

                    var scheduleDTO = _dtoConverter.ConvertBatchToDTO(schedule);
                    await _syncService.UploadScheduleAsync(scheduleDTO);

                    await _localContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { weekOf = firstDay.ToString("yyyy-MM-dd") });
                }
            }

            return RedirectToAction(nameof(UploadSchedule), new { year = form.Season });
        }

        private bool DoesSeasonExist(int year)
        {
            return _localContext.Seasons.Any(s => s.Year == year);
        }

        private bool DoesSeasonHaveSchedule(int year)
        {
            return _localContext.Schedule
                .Include(s => s.Season)
                .Any(s => s.Season.Year == year);
        }

        [Route("Schedule/PlayoffSeries/{season}/{team1}/{team2}")]
        [AllowAnonymous]
        public async Task<IActionResult> PlayoffSeries(int season, string team1, string team2)
        {
            var playoffSeries = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season &&
                            ((s.Team1.Code == team1 && s.Team2.Code == team2) ||
                             (s.Team1.Code == team2 && s.Team2.Code == team1)))
                .FirstOrDefault()!;

            Conference conference = _localContext.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Team == playoffSeries.Team1)
                .Select(a => a.Conference)
                .FirstOrDefault()!;

            string conferenceName = conference.Name.Replace(" Conference", "");

            string roundString;
            if ((season == 2021 && playoffSeries.Round.Index == 3) ||
                (season >= 2022 && playoffSeries.Round.Index == 4))
                roundString = $"{season} {playoffSeries.Round.Name}";
            else
            {
                string name = playoffSeries.Round.Name.Replace(" Finals", " Final");
                roundString = $"{season} {conferenceName} {name}";
            }
            ViewBag.RoundString = roundString;

            var schedule = await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == "Playoffs" &&
                            ((s.AwayTeam == playoffSeries.Team2 && s.HomeTeam == playoffSeries.Team1) ||
                             (s.AwayTeam == playoffSeries.Team1 && s.HomeTeam == playoffSeries.Team2)))
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex)
                .ToListAsync();
            ViewBag.SeriesSchedule = schedule;

            ViewBag.Season = season;


            return View(playoffSeries);
        }

        // GET: Schedule/Details/5
        [Route("GameCenter/{date}/{awayTeam}/{homeTeam}")]
        [AllowAnonymous]
        public async Task<IActionResult> GameCenter(DateTime date, string awayTeam, string homeTeam)
        {
            if (_localContext.Schedule == null)
            {
                return NotFound();
            }

            var game = await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(m => m.Date.Date == date &&
                                          m.AwayTeam.Code == awayTeam &&
                                          m.HomeTeam.Code == homeTeam);

            if (game == null)
            {
                return NotFound();
            }

            int season = game.Season.Year;

            var matchup = _localContext.HeadToHeadSeries
                .Include(h => h.Season)
                .Include(h => h.Team1)
                .Include(h => h.Team2)
                .FirstOrDefault(h => h.Season.Year == season &&
                                     ((h.Team1.Code == awayTeam && h.Team2.Code == homeTeam) ||
                                      (h.Team1.Code == homeTeam && h.Team2.Code == awayTeam)));
            ViewBag.Matchup = matchup;

            var teamStats = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            (s.Team == game.AwayTeam || s.Team == game.HomeTeam))
                .OrderBy(s => s.LeagueRanking)
                .ToList();
            ViewBag.TeamStats = teamStats;

            IQueryable<Game> results = _localContext.Schedule
                .Where(s => (s.AwayTeam.Code == awayTeam && s.HomeTeam.Code == homeTeam) ||
                            (s.AwayTeam.Code == homeTeam && s.HomeTeam.Code == awayTeam));

            if (game.Type == GameTypes.PLAYOFFS)
            {
                var series = _localContext.PlayoffSeries
                    .Include(s => s.Season)
                    .Include(s => s.Round)
                    .Include(s => s.Team1)
                    .Include(s => s.Team2)
                    .FirstOrDefault(s => s.Season.Year == season &&
                                         ((s.Team1!.Code == awayTeam && s.Team2!.Code == homeTeam) ||
                                          (s.Team1!.Code == homeTeam && s.Team2!.Code == awayTeam)));
                ViewBag.PlayoffSeries = series;
                
                results = results
                    .Where(r => r.Type == GameTypes.PLAYOFFS &&
                                r.Season.Year == season)
                    .OrderBy(r => r.Date);
            }
            else
            {
                results = results.Where(r => r.Type != GameTypes.PLAYOFFS);

                if (teamStats[0].Conference != teamStats[1].Conference)
                    results = results
                        .Where(r => r.IsFinalized)
                        .OrderByDescending(r => r.Date)
                        .Take(5);
                else
                    results = results
                        .Where(r => r.Season.Year == season)
                        .OrderBy(r => r.Date);
            }

            ViewBag.Results = results.ToList();

            return View(game);
        }

        // GET: Schedule/Edit/5
        [Route("GameControls/{date}/{awayTeam}/{homeTeam}")]
        public async Task<IActionResult> GameControls(DateTime date, string awayTeam, string homeTeam)
        {
            if (_localContext.Schedule == null)
            {
                return NotFound();
            }

            var game = await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Date.Date == date &&
                                          s.AwayTeam.Code == awayTeam &&
                                          s.HomeTeam.Code == homeTeam)!;

            if (game == null)
            {
                return NotFound();
            }

            int season = game.Season.Year;

            var matchup = _localContext.HeadToHeadSeries
                .Include(h => h.Season)
                .Include(h => h.Team1)
                .Include(h => h.Team2)
                .FirstOrDefault(h => h.Season.Year == season &&
                                     ((h.Team1.Code == awayTeam && h.Team2.Code == homeTeam) ||
                                      (h.Team1.Code == homeTeam && h.Team2.Code == awayTeam)));
            ViewBag.Matchup = matchup;

            var teamStats = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            (s.Team == game.AwayTeam || s.Team == game.HomeTeam))
                .OrderBy(s => s.LeagueRanking)
                .ToList();
            ViewBag.TeamStats = teamStats;

            IQueryable<Game> results = _localContext.Schedule
                .Where(s => (s.AwayTeam.Code == awayTeam && s.HomeTeam.Code == homeTeam) ||
                            (s.AwayTeam.Code == homeTeam && s.HomeTeam.Code == awayTeam));

            if (game.Type == GameTypes.PLAYOFFS)
            {
                var series = _localContext.PlayoffSeries
                    .Include(s => s.Season)
                    .Include(s => s.Round)
                    .Include(s => s.Team1)
                    .Include(s => s.Team2)
                    .FirstOrDefault(s => s.Season.Year == season &&
                                         ((s.Team1!.Code == awayTeam && s.Team2!.Code == homeTeam) ||
                                          (s.Team1!.Code == homeTeam && s.Team2!.Code == awayTeam)));
                ViewBag.PlayoffSeries = series;

                results = results
                    .Where(r => r.Type == GameTypes.PLAYOFFS &&
                                r.Season.Year == season)
                    .OrderBy(r => r.Date);
            }
            else
            {
                results = results.Where(r => r.Type != GameTypes.PLAYOFFS);

                if (teamStats[0].Conference != teamStats[1].Conference)
                    results = results
                        .Where(r => r.IsFinalized)
                        .OrderByDescending(r => r.Date)
                        .Take(5);
                else
                    results = results
                        .Where(r => r.Season.Year == season)
                        .OrderBy(r => r.Date);
            }
                
            ViewBag.Results = results.ToList();

            TempData["APIDomain"] = _secrets.GetValue<string>("API:LocalURL");
            TempData["MVCDomain"] = _secrets.GetValue<string>("MVC:LocalURL");
            TempData["Endpoints"] = _secrets.GetSection("API:Endpoints:Schedule");

            return View(game);
        }

        public async Task<Game> GetGameAsync(Guid? id)
        {
            var game = await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Id == id);

            return game!;
        }

        public async Task UpdateGameAsync(Game game)
        {
            _localContext.Schedule.Update(game);
            await _localContext.SaveChangesAsync();
        }

        public async Task<IActionResult> SaveGame(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive)
            {
                game.IsLive = false;
                await UpdateGameAsync(game);
            }

            if (game.Type == GameTypes.PLAYOFFS)
                return RedirectToAction(nameof(Playoffs), new { season = game.Date.Year, round = game.PlayoffRound!.Index });

            return RedirectToAction(nameof(Index), new { weekOf = game.Date.ToString("yyyy-MM-dd") });
        }

        /// <summary>
        ///     Finalize a game.
        /// </summary>
        /// <param name="id">The GUID of the game to finalize.</param>
        /// <returns></returns>
        public async Task<IActionResult> FinalizeGame(Guid id)
        {
            // Get the game by its ID
            var game = await GetGameAsync(id);

            // Get the season and date of this game
            int season = game.Season.Year;
            DateTime date = game.Date;

            // If the game is currently live, in the 3rd period or later, and the score is NOT tied
            if (IsGameLive(game) && game.Period >= 3 && game.AwayScore != game.HomeScore)
            {
                // Disable the Live flag, and enable the Finalized flag
                game.IsLive = false;
                game.IsFinalized = true;
                await UpdateGameAsync(game);

                // If the game is a playoff game, update the appropriate playoff series
                if (game.Type == GameTypes.PLAYOFFS)
                    await UpdatePlayoffSeriesAsync(season, game);
                // If it is not a playoff game...
                else
                {
                    // Update the head-to-head matchup between the teams of this game
                    await UpdateH2HSeriesAsync(game);

                    // If the game is a regular season game...
                    if (game.Type == GameTypes.REGULAR_SEASON)
                    {
                        // Update the standings
                        await UpdateStandingsAsync(season, game.AwayTeam, game.HomeTeam);

                        // After all the games in a day have been completed, take a snapshot of the standings
                        if (IsSnapshotNeeded(date))
                            TakeSnapshot(date);
                    }
                }
                await _localContext.SaveChangesAsync();

                // Convert the game to DTO format and send it to the sync service
                var dto = new DTO_Game
                {
                    Id = game.Id,
                    SeasonId = game.SeasonId,
                    Date = game.Date,
                    GameIndex = game.GameIndex,
                    Type = game.Type,
                    PlayoffRoundId = game.PlayoffRoundId,
                    PlayoffSeriesId = game.PlayoffSeriesId,
                    PlayoffGameIndex = game.PlayoffGameIndex,
                    PlayoffSeriesScore = game.PlayoffSeriesScore,
                    AwayTeamId = game.AwayTeamId,
                    AwayScore = game.AwayScore,
                    HomeTeamId = game.HomeTeamId,
                    HomeScore = game.HomeScore,
                    Period = game.Period,
                    IsConfirmed = game.IsConfirmed,
                    IsLive = game.IsLive,
                    IsFinalized = game.IsFinalized,
                    Notes = game.Notes
                };
                await _syncService.WriteOneResultAsync(dto);

                return RedirectToAction(nameof(GameCenter), new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    awayTeam = game.AwayTeam.Code,
                    homeTeam = game.HomeTeam.Code
                });
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToString("yyyy-MM-dd"),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        /// <summary>
        ///     Takes a snapshot of the current standings if all the games on a particular day have been played.
        /// </summary>
        /// <param name="date">The date to get the games of.</param>
        /// <returns>True if all the games on the particular day have been played, otherwise False.</returns>
        private bool IsSnapshotNeeded(DateTime date)
        {
            // Get all the games scheduled on the requested date
            var games = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Date.Date == date.Date)
                .OrderBy(s => s.GameIndex);

            // Return True if there are any games on the requested date AND they are all finalized
            return games.Any() && games.Count(s => s.IsFinalized) == games.Count();
        }

        private void TakeSnapshot(DateTime date)
        {
            if (IsSnapshotNeeded(date))
            {
                int season = date.Year;

                var standings = _localContext.Standings
                    .Include(s => s.Season)
                    .Include(s => s.Conference)
                    .Include(s => s.Division)
                    .Include(s => s.Team)
                    .Where(s => s.Season.Year == season)
                    .OrderBy(s => s.LeagueRanking)
                    .ToList();

                var snapshot = new StandingsSnapshot
                {
                    Id = new ObjectId(),
                    Season = season,
                    Date = date,
                    Standings = standings
                };
            }
            else
                throw new SnapshotNotNeededException("Snapshot is not needed.", date);
        }

        private async Task UpdateH2HSeriesAsync(Game game)
        {
            var matchup = await _localContext.HeadToHeadSeries
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefaultAsync(s => s.Season.Year == game.Season.Year &&
                                          ((s.Team1 == game.AwayTeam && s.Team2 == game.HomeTeam) ||
                                           (s.Team1 == game.HomeTeam && s.Team2 == game.AwayTeam)));

            bool team1IsHomeTeam = matchup!.Team1 == game.HomeTeam;
            if (team1IsHomeTeam)
            {
                if (game.HomeScore > game.AwayScore)
                    matchup.Team1Wins++;
                else
                    matchup.Team2Wins++;
            }
            else
            {
                if (game.HomeScore > game.AwayScore)
                    matchup.Team2Wins++;
                else
                    matchup.Team1Wins++;
            }

            matchup.Team1GoalsFor += team1IsHomeTeam ? game.HomeScore : game.AwayScore;
            matchup.Team2GoalsFor += team1IsHomeTeam ? game.AwayScore : game.HomeScore;

            _localContext.HeadToHeadSeries.Update(matchup);
            await _localContext.SaveChangesAsync();
        }

        private bool IsGameLive(Game game)
        {
            return game.IsLive && !game.IsFinalized;
        }

        private bool ScheduleExists(Guid id)
        {
            return (_localContext.Schedule?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [AllowAnonymous]
        public IActionResult Formula()
        {
            return View();
        }

        private async Task UpdatePlayoffSeriesAsync(int season, Game game)
        {
            var series = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season &&
                            ((s.Team1 == game.HomeTeam && s.Team2 == game.AwayTeam) ||
                             (s.Team1 == game.AwayTeam && s.Team2 == game.HomeTeam)))
                .FirstOrDefault()!;

            if (series.HasEnded)
                throw new Exception("This series has already ended.");

            var schedule = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.PlayoffSeries)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == GameTypes.PLAYOFFS &&
                            ((s.AwayTeam == game.AwayTeam && s.HomeTeam == game.HomeTeam) ||
                             (s.AwayTeam == game.HomeTeam && s.HomeTeam == game.AwayTeam)))
                .OrderBy(s => s.Date)
                .ToList();

            var gamesPlayed = schedule.Where(g => g.IsFinalized);

            series.Team1Wins = gamesPlayed
                .Count(g => (g.AwayTeam == series.Team1 && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == series.Team1 && g.HomeScore > g.AwayScore));
            series.Team2Wins = gamesPlayed
                .Count(g => (g.AwayTeam == series.Team2 && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == series.Team2 && g.HomeScore > g.AwayScore));

            _localContext.PlayoffSeries.Update(series);

            game.Notes = series.SeriesScoreString;
            game.PlayoffSeriesScore = series.ShortSeriesScoreString;
            _localContext.Schedule.Update(game);

            var remainingGames = schedule.Where(g => !g.IsFinalized);
            if (remainingGames.Any())
            {
                var nextGame = remainingGames.First();
                bool manyGamesRemaining = remainingGames.Count() > 1;

                nextGame.Notes = manyGamesRemaining ? series.SeriesScoreString : "Game 7";
                nextGame.PlayoffSeriesScore = manyGamesRemaining ? series.ShortSeriesScoreString : "Game 7";
                _localContext.Schedule.Update(nextGame);

                var nextUnconfirmedGame = schedule.FirstOrDefault(g => !g.IsConfirmed)!;
                int minimumWinsNeeded = (int)nextUnconfirmedGame.PlayoffGameIndex! - 4;
                nextUnconfirmedGame.IsConfirmed = Math.Min(series.Team1Wins, series.Team2Wins) == minimumWinsNeeded;
                _localContext.Schedule.Update(nextUnconfirmedGame);
            }

            if (Math.Max(series.Team1Wins, series.Team2Wins) == 4)
            {
                series.HasEnded = true;
                
                if (remainingGames.Any())
                {
                    foreach (var _game in remainingGames)
                        _localContext.Schedule.Remove(_game);
                }

                if ((series.Season.Year == 2021 && series.Round.Index == 3) ||
                    (series.Season.Year >= 2022 && series.Round.Index == 4))
                {
                    Team _champion = (series.Team1Wins == 4) ? series.Team1! : series.Team2!;

                    var champion = new Champion
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = series.SeasonId,
                        Season = series.Season,
                        TeamId = _champion.Id,
                        Team = _champion
                    };
                    var championDTO = new DTO_Champion
                    {
                        Id = champion.Id,
                        SeasonId = champion.SeasonId,
                        TeamId = champion.TeamId
                    };
                    _localContext.Champions.Add(champion);
                    var package = new DTP_NewChampion(championDTO);

                    var roundsWon = _localContext.PlayoffSeries
                        .Include(s => s.Season)
                        .Include(s => s.Round)
                        .Include(s => s.Team1)
                        .Include(s => s.Team2)
                        .Where(s => s.Season.Year == champion.Season.Year &&
                                    (s.Team1 == champion.Team ||
                                     s.Team2 == champion.Team))
                        .OrderBy(s => s.Round.Index);

                    foreach (var round in roundsWon)
                    {
                        Team opponent = (champion.Team == round.Team1) ? round.Team2! : round.Team1!;

                        var roundWon = new ChampionsRound
                        {
                            Id = Guid.NewGuid(),
                            ChampionId = champion.Id,
                            Champion = champion,
                            RoundIndex = round.Round.Index,
                            OpponentId = opponent.Id,
                            Opponent = opponent,
                            SeriesLength = round.Team1Wins + round.Team2Wins,
                            BestOf = 7
                        };
                        var roundWonDTO = new DTO_ChampionsRound
                        {
                            Id = roundWon.Id,
                            ChampionId = roundWon.ChampionId,
                            RoundIndex = roundWon.RoundIndex,
                            OpponentId = roundWon.OpponentId,
                            SeriesLength = roundWon.SeriesLength,
                            BestOf = roundWon.BestOf
                        };
                        _localContext.ChampionsRounds.Add(roundWon);
                        package.Rounds.Add(roundWonDTO);
                    }

                    series.Season.IsLive = false;
                    series.Season.IsComplete = true;
                    
                    _localContext.Seasons.Update(series.Season);
                    await _syncService.NewChampionAsync(package);
                }
            }

            await _localContext.SaveChangesAsync();
        }

        private async Task<RankedPlayoffSeries> CalculateFinalScoreForSeriesAsync(PlayoffSeries series, 
                                                                                  List<Game> schedule)
        {
            if (!series.HasEnded)
                throw new InvalidOperationException("This series has not ended yet.");

            var details = new RankedPlayoffSeries
            {
                Id = Guid.NewGuid(),
                SeasonId = series.SeasonId,
                Season = series.Season,
                RoundId = series.RoundId,
                Round = series.Round,
                MatchupId = series.Id,
                Matchup = series
            };

            Team seriesWinner = series.Team1Wins > series.Team2Wins ? series.Team1! : series.Team2!;

            const int TEAM1 = 0, TEAM2 = 1;
            int LENGTH = schedule.Count;
            int[] margins = new int[LENGTH];
            int[] overtimes = new int[LENGTH];
            int goalDifferential = 0;
            int goalTotal = 0;
            int seriesTies = 0;
            for (int index = 0; index < LENGTH; index++)
            {
                Game game = schedule[index];
                
                int goals = game.HomeScore + game.AwayScore;
                int margin = CalculateGameMargin(game, seriesWinner);

                margins[index] = Math.Abs(margin);
                overtimes[index] = game.Period - 3;
                goalTotal += goals;
                goalDifferential += margin;

                if (game.PlayoffSeriesScore!.StartsWith("Series tied"))
                    seriesTies++;
            }
            goalDifferential = Math.Abs(goalDifferential);

            details.SeriesCompetitivenessScore = CalculateSeriesCompetitivenessScore(margins);
            details.OvertimeImpactScore = CalculateOTImpactScore(overtimes);
            details.OverallGoalDiffScore = 15 - (15 * ((decimal)goalDifferential / goalTotal));
            details.SeriesTiesScore = 5 * (decimal)seriesTies;

            details.FinalScore =
                details.SeriesCompetitivenessScore +
                details.OvertimeImpactScore +
                details.OverallGoalDiffScore +
                details.SeriesTiesScore;

            bool seriesExists = _localContext.RankedPlayoffSeries
                .Any(s => s.Season.Year == details.Season.Year &&
                          s.Round.Index == details.Round.Index &&
                          s.MatchupId == details.MatchupId);

            if (!seriesExists)
                _localContext.RankedPlayoffSeries.Add(details);

            await ReorderPlayoffSeriesRankingsAsync();
            await _localContext.SaveChangesAsync();

            return details;
        }

        private int CalculateGameMargin(Game game, Team seriesWinner)
        {
            return seriesWinner == game.HomeTeam ?
                game.HomeScore - game.AwayScore :
                game.AwayScore - game.HomeScore;
        }

        private async Task ReorderPlayoffSeriesRankingsAsync()
        {
            var matchups = _localContext.RankedPlayoffSeries
                .Include(m => m.Season)
                .Include(m => m.Round)
                .Include(m => m.Matchup)
                .OrderByDescending(m => m.FinalScore)
                .ThenByDescending(m => m.SeriesCompetitivenessScore)
                .ThenByDescending(m => m.OvertimeImpactScore)
                .ThenByDescending(m => m.OverallGoalDiffScore)
                .ThenByDescending(m => m.SeriesTiesScore)
                .ThenBy(m => m.Season.Year);

            Dictionary<int, int> currentSeasonRankings = new();
            int ranking = 1;
            foreach (var matchup in matchups)
            {
                int season = matchup.Season.Year;
                
                if (!currentSeasonRankings.ContainsKey(season))
                    currentSeasonRankings.Add(season, 1);

                matchup.SeasonRanking = currentSeasonRankings[season];
                matchup.OverallRanking = ranking;

                currentSeasonRankings[season]++;
                ranking++;
            }

            await _localContext.SaveChangesAsync();
        }

        private int DetermineBaseScore(int length)
        {
            return length switch
            {
                4 => 10,
                5 => 12,
                6 => 15,
                7 => 18,
                _ => 0
            };
        }

        private static decimal DetermineBonusOrPenalty(decimal rate)
        {
            if (rate == 1)
                return 10;

            if (rate >= 0.75m)
                return 8;

            if (rate > 0.5m)
                return 5;

            return 0;
        }

        private int DetermineOTWeight(int ot)
        {
            if (ot == 0)
                return 0;

            return 3 + (2 * (ot - 1));
        }

        private decimal CalculateSeriesCompetitivenessScore(int[] margins)
        {
            int LENGTH = margins.Length;
            decimal score = DetermineBaseScore(LENGTH);
            int blowouts = 0;
            int closeGames = 0;

            foreach (var margin in margins)
            {
                if (margin == 1)
                {
                    score += 3;
                    closeGames++;
                }
                else if (margin == 2)
                    score += 2;
                else
                {
                    score -= margin - 3;
                    blowouts++;
                }
            }

            decimal blowoutRate = (decimal)blowouts / LENGTH;
            decimal closeGameRate = (decimal)closeGames / LENGTH;

            if (blowoutRate > 0.5m)
                score -= DetermineBonusOrPenalty(blowoutRate);

            else if (closeGameRate > 0.5m)
                score += DetermineBonusOrPenalty(closeGameRate);

            return Math.Min(40, score);
        }

        private decimal CalculateOTImpactScore(int[] overtimes)
        {
            int LENGTH = overtimes.Length;
            decimal score = DetermineBaseScore(LENGTH);
            int otPeriods = 0;

            for (int index = 0; index < LENGTH; index++)
            {
                int game = index + 1;
                int ot = overtimes[index];
                
                if (ot > 0)
                {
                    otPeriods += ot;
                    int weight = DetermineOTWeight(ot);
                    score += weight * ((decimal)game / LENGTH);
                }
            }

            if (otPeriods == 0)
                score /= 2;

            return Math.Min(30, score);
        }

        private async Task UpdateStandingsAsync(int season, Team awayTeam, Team homeTeam)
        {
            await UpdateTeamStats(season, awayTeam);
            await UpdateTeamStats(season, homeTeam);
            await UpdateRankings(season);

            await StandingsUpdateNowAvailable();
        }

        private async Task UpdateRankings(int season)
        {
            if (season >= 2025)
            {
                List<Standings> standings = await GetStandingsAsync(season, true);
                List<Standings> teams;

                int ranking = 1;

                if (season == 2025)
                {
                    var divisions = standings
                    .Select(s => s.Division)
                    .Distinct()
                    .OrderBy(s => s!.Name);
                    foreach (var division in divisions)
                    {
                        ranking = 1;

                        teams = standings.Where(s => s.Division == division).ToList();
                        teams = ApplyHeadToHeadTiebreakers(teams, "division");

                        foreach (var team in teams)
                        {
                            team.DivisionRanking = ranking;
                            _localContext.Standings.Update(team);

                            ranking++;
                        }
                    }
                }

                var conferences = standings
                    .Select(s => s.Conference)
                    .Distinct()
                    .OrderBy(s => s!.Name);
                foreach (var conference in conferences)
                {
                    ranking = 1;
                    teams = standings.Where(s => s.Conference == conference).ToList();

                    var leaders = GetLeaders(teams);
                    leaders = ApplyHeadToHeadTiebreakers(leaders, "playoffs");
                    foreach (var team in leaders)
                    {
                        team.PlayoffRanking = ranking;
                        _localContext.Standings.Update(team);

                        ranking++;
                    }

                    var wildCards = GetWildCards(teams);
                    wildCards = ApplyHeadToHeadTiebreakers(wildCards, "playoffs");
                    foreach (var team in wildCards)
                    {
                        team.PlayoffRanking = ranking;
                        _localContext.Standings.Update(team);

                        ranking++;
                    }

                    ranking = 1;
                    teams = ApplyHeadToHeadTiebreakers(teams, "conference");
                    foreach (var team in teams)
                    {
                        team.ConferenceRanking = ranking;
                        _localContext.Standings.Update(team);

                        ranking++;
                    }
                }

                ranking = 1;
                standings = ApplyHeadToHeadTiebreakers(standings, "league");
                foreach (var team in standings)
                {
                    team.LeagueRanking = ranking;
                    _localContext.Standings.Update(team);

                    ranking++;
                }

                await _localContext.SaveChangesAsync();
            }
        }

        private int GetDivisionOrConferenceRanking(List<Standings> standings, Standings team, string searchType)
        {
            if (!(searchType == "division" || searchType == "conference"))
                throw new Exception("Invalid search type.");
            
            standings = searchType == "division" ?
                standings.Where(s => s.Division == team.Division).ToList() :
                standings.Where(s => s.Conference == team.Conference).ToList();

            return standings.IndexOf(team) + 1;
        }

        private int GetPlayoffRanking(List<Standings> standings, Standings team)
        {
            standings = standings.Where(s => s.Conference == team.Conference).ToList();
            
            var leaders = GetLeaders(standings);
            if (leaders.Contains(team))
                return leaders.IndexOf(team) + 1;

            var wildCards = GetWildCards(standings);
            return wildCards.IndexOf(team) + (leaders.Count + 1);
        }

        private List<Standings> GetWildCards(List<Standings> standings)
        {
            return standings
                .Where(s => s.DivisionRanking > 1)
                .ToList();
        }

        private List<Standings> GetLeaders(List<Standings> standings)
        {
            return standings
                .Where(s => s.DivisionRanking == 1)
                .ToList();
        }

        private async Task<List<Standings>> GetStandingsAsync(int season, bool updatingRankings)
        {
            SEASON = season;

            IQueryable<Standings> standings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            if (!updatingRankings)
                return standings.OrderBy(s => s.LeagueRanking).ToList();

            int mostRecentSeason = _localContext.Standings
                .Include(s => s.Season)
                .Max(s => s.Season.Year);

            if (season < 2021 || season > mostRecentSeason)
                throw new Exception($"There is no {season} season.");
            else if (season <= 2022)
            {
                standings = standings
                    .OrderByDescending(s => s.Points)
                    .ThenBy(s => s.GamesPlayed)
                    .ThenByDescending(s => s.Wins)
                    .ThenBy(s => s.Losses)
                    .ThenByDescending(s => s.GoalDifferential)
                    .ThenByDescending(s => s.GoalsFor)
                    .ThenBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            }
            else if (season == 2023)
            {
                standings = standings
                    .OrderByDescending(s => s.Points)
                    .ThenBy(s => s.GamesPlayed)
                    .ThenByDescending(s => s.RegulationWins)
                    .ThenByDescending(s => s.RegPlusOTWins)
                    .ThenByDescending(s => s.Wins)
                    .ThenBy(s => s.Losses)
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
                    .ThenByDescending(s => s.Streak)
                    .ThenBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            }
            else if (season == 2024)
            {
                standings = standings
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
            }
            else if (season == 2025)
            {
                bool in2ndHalfOfSeason = standings.Any(s => s.GamesPlayed > (s.Season.GamesPerTeam / 2));

                standings = in2ndHalfOfSeason ?
                    standings
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
                        .ThenByDescending(s => s.GoalDifferential)
                        .ThenByDescending(s => s.GoalsFor)
                        .ThenByDescending(s => s.WinPctInLast10Games)
                        .ThenByDescending(s => s.WinsInLast10Games)
                        .ThenBy(s => s.LossesInLast10Games)
                        .ThenByDescending(s => s.Streak)
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name) :
                    standings
                        .OrderByDescending(s => s.WinPct)
                        .ThenByDescending(s => s.Wins)
                        .ThenBy(s => s.Losses)
                        .ThenByDescending(s => s.RegulationWins)
                        .ThenByDescending(s => s.RegPlusOTWins)
                        .ThenByDescending(s => s.GoalDifferential)
                        .ThenByDescending(s => s.GoalsFor)
                        .ThenByDescending(s => s.WinPctInLast10Games)
                        .ThenByDescending(s => s.WinsInLast10Games)
                        .ThenBy(s => s.LossesInLast10Games)
                        .ThenByDescending(s => s.Streak)
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name);
            }
            else
            {
                standings = standings
                    .OrderByDescending(s => s.WinPct)
                    .ThenByDescending(s => s.Wins)
                    .ThenBy(s => s.Losses)
                    .ThenByDescending(s => s.WinPctVsConference)
                    .ThenByDescending(s => s.WinsVsConference)
                    .ThenBy(s => s.LossesVsConference)
                    .ThenByDescending(s => s.GoalDifferential)
                    .ThenByDescending(s => s.GoalsFor)
                    .ThenByDescending(s => s.WinPctInLast10Games)
                    .ThenByDescending(s => s.WinsInLast10Games)
                    .ThenBy(s => s.LossesInLast10Games)
                    .ThenByDescending(s => s.Streak)
                    .ThenBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            }

            return standings.ToList();
        }

        private List<Standings> ApplyDivisionLeaderTiebreaker(List<Standings> standings)
        {
            return standings
                .OrderByDescending(s => s.DivisionRanking == 1)
                .ToList();
        }

        private List<Standings> ApplyPlayoffStatusTiebreaker(List<Standings> standings)
        {
            return standings
                .OrderByDescending(s => s.PlayoffRanking <= 8)
                .ToList();
        }

        private List<Standings> ApplyHeadToHeadTiebreakers(List<Standings> standings, string level)
        {
            List<Standings> tiedTeams = new List<Standings>();
            List<int> indexes;
            for (int index = 0; index < standings.Count - 1; index++)
            {
                Standings currentTeam = standings[index];
                Standings nextTeam = standings[index + 1];

                if (currentTeam.Wins == nextTeam.Wins &&
                    currentTeam.Losses == nextTeam.Losses &&
                    currentTeam.RegulationWins == nextTeam.RegulationWins &&
                    currentTeam.RegPlusOTWins == nextTeam.RegPlusOTWins)
                {
                    if (tiedTeams.IsNullOrEmpty())
                        tiedTeams.Add(currentTeam);

                    tiedTeams.Add(nextTeam);

                    if (index == standings.Count - 2)
                    {
                        if (tiedTeams.Count == standings.Count)
                            return standings;

                        indexes = GetIndexesInLeagueStandings(standings, tiedTeams);

                        standings = tiedTeams.Count == 2 ?
                            ApplyTwoWayH2HTiebreaker(standings, indexes[0], tiedTeams[0], indexes[1], tiedTeams[1]) :
                            ApplyMultiWayH2HTiebreaker(standings, indexes, tiedTeams);
                    }
                }
                else
                {
                    if (tiedTeams.Any())
                    {
                        if (tiedTeams.First().GamesPlayed > 0)
                        {
                            indexes = GetIndexesInLeagueStandings(standings, tiedTeams);
                            standings = tiedTeams.Count == 2 ?
                                ApplyTwoWayH2HTiebreaker(standings, indexes[0], tiedTeams[0], indexes[1], tiedTeams[1]) :
                                ApplyMultiWayH2HTiebreaker(standings, indexes, tiedTeams);
                        }

                        tiedTeams.Clear();
                    }
                }
            }

            return standings;
        }

        private int GetSeason(List<Standings> standings)
        {
            return standings.First().Season.Year;
        }

        private List<int> GetIndexesInLeagueStandings(List<Standings> standings, List<Standings> tiedTeams)
        {
            List<int> indexes = new List<int>();

            foreach (var team in tiedTeams)
            {
                int index = standings.IndexOf(team);
                indexes.Add(index);
            }

            return indexes;
        }

        private List<Standings> ApplyTwoWayH2HTiebreaker(List<Standings> standings, 
                                                         int index1, Standings team1, 
                                                         int index2, Standings team2)
        {
            var matchup = _localContext.HeadToHeadSeries
                .Include(m => m.Season)
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .FirstOrDefault(m => m.Season.Year == SEASON &&
                                     ((m.Team1 == team1.Team && m.Team2 == team2.Team) ||
                                      (m.Team1 == team2.Team && m.Team2 == team1.Team)))!;

            int gamesPlayed = matchup.Team1Wins + matchup.Team2Wins;
            if (gamesPlayed > 0)
            {
                if (matchup.Team1Wins == matchup.Team2Wins)
                    return ApplyTwoWayH2HAggregateTiebreaker(standings, matchup, index1, team1, index2, team2);

                if (team1.Team == matchup.Team1)
                {
                    if (matchup.Team1Wins < matchup.Team2Wins)
                        return SwapTeams(standings, index1, team1, index2, team2);
                }
                else
                {
                    if (matchup.Team1Wins > matchup.Team2Wins)
                        return SwapTeams(standings, index1, team1, index2, team2);
                }
            }

            return standings;
        }
        
        private List<Standings> ApplyTwoWayH2HAggregateTiebreaker(List<Standings> standings,
                                                                  HeadToHeadSeries matchup,
                                                                  int index1, Standings team1,
                                                                  int index2, Standings team2)
        {
            if (team1.Team == matchup.Team1)
            {
                if (matchup.Team1GoalsFor < matchup.Team2GoalsFor)
                    return SwapTeams(standings, index1, team1, index2, team2);
            }
            else
            {
                if (matchup.Team1GoalsFor > matchup.Team2GoalsFor)
                    return SwapTeams(standings, index1, team1, index2, team2);
            }

            return standings;
        }

        private List<Standings> ApplyMultiWayH2HTiebreaker(List<Standings> standings, List<int> indexes, List<Standings> tiedTeams)
        {
            const int GAMES_PLAYED = 0, WINS = 1, LOSSES = 2;

            Dictionary<Team, int[]> records = new();

            foreach (var team in tiedTeams)
                records.Add(team.Team, new int[5]);

            for (int index = 0; index < tiedTeams.Count - 1; index++)
            {
                for (int nextIndex = index + 1; nextIndex < tiedTeams.Count; nextIndex++)
                {
                    Team currentTeam = tiedTeams[index].Team;
                    Team nextTeam = tiedTeams[nextIndex].Team;

                    var matchup = _localContext.HeadToHeadSeries
                        .Include(s => s.Season)
                        .Include(s => s.Team1)
                        .Include(s => s.Team2)
                        .FirstOrDefault(s => s.Season.Year == SEASON &&
                                             ((s.Team1 == currentTeam && s.Team2 == nextTeam) ||
                                              (s.Team1 == nextTeam && s.Team2 == currentTeam)))!;

                    var games = _localContext.Schedule
                        .Include(s => s.Season)
                        .Include(s => s.AwayTeam)
                        .Include(s => s.HomeTeam)
                        .Where(s => s.Season.Year == SEASON &&
                                    s.Type == GameTypes.REGULAR_SEASON &&
                                    !s.IsLive && s.IsFinalized &&
                                    ((s.AwayTeam == nextTeam && s.HomeTeam == currentTeam) ||
                                     (s.AwayTeam == currentTeam && s.HomeTeam == nextTeam)))
                        .OrderBy(s => s.Date);

                    if (games.Any())
                    {
                        records[currentTeam] = UpdateMultiWayH2HTeamStats(matchup, currentTeam, records[currentTeam]);
                        records[nextTeam] = UpdateMultiWayH2HTeamStats(matchup, nextTeam, records[nextTeam]);
                    }
                }
            }

            int teamsNotPlayedOthers = records.Count(r => r.Value[GAMES_PLAYED] == 0);
            if (teamsNotPlayedOthers < records.Count)
            {
                const int TEAM1 = 0, TEAM2 = 1;
                
                var tiebreaker = records
                    .OrderByDescending(r => (r.Value[GAMES_PLAYED] > 0) ?
                                                (decimal)r.Value[WINS] / r.Value[GAMES_PLAYED] : 0)
                    .ThenByDescending(r => r.Value[WINS])
                    .ThenBy(r => r.Value[LOSSES])
                    .ToList();
                var teamStatLines = ExtractStatLines(SEASON, tiebreaker);
                standings = ReorderTeams(standings, indexes, teamStatLines);

                List<Standings> teamsStillTied = new();
                for (int index = 0; index < tiebreaker.Count - 1; index++)
                {
                    KeyValuePair<Team, int[]> currentTeam = tiebreaker[index];
                    KeyValuePair<Team, int[]> nextTeam = tiebreaker[index + 1];

                    if (currentTeam.Value[WINS] == nextTeam.Value[WINS] &&
                        currentTeam.Value[LOSSES] == nextTeam.Value[LOSSES])
                    {
                        if (teamsStillTied.IsNullOrEmpty())
                            teamsStillTied.Add(GetTeamStatLine(SEASON, currentTeam.Key));

                        teamsStillTied.Add(GetTeamStatLine(SEASON, nextTeam.Key));

                        if (index == tiebreaker.Count - 2)
                        {
                            if (teamsStillTied.Count == tiebreaker.Count)
                                return standings;

                            indexes = GetIndexesInLeagueStandings(standings, teamsStillTied);

                            standings = teamsStillTied.Count == 2 ?
                                ApplyTwoWayH2HTiebreaker(standings,
                                                         indexes[TEAM1], teamsStillTied[TEAM1],
                                                         indexes[TEAM2], teamsStillTied[TEAM2]) :
                                ApplyMultiWayH2HTiebreaker(standings, indexes, teamsStillTied);
                        }
                    }
                    else
                    {
                        if (teamsStillTied.Any())
                        {
                            Team firstTeamInTie = teamsStillTied.First().Team;
                            int gamesPlayed = records[firstTeamInTie][GAMES_PLAYED];

                            if (gamesPlayed > 0)
                            {
                                indexes = GetIndexesInLeagueStandings(standings, teamsStillTied);

                                standings = teamsStillTied.Count == 2 ?
                                    ApplyTwoWayH2HTiebreaker(standings,
                                                             indexes[TEAM1], teamsStillTied[TEAM1],
                                                             indexes[TEAM2], teamsStillTied[TEAM2]) :
                                    ApplyMultiWayH2HTiebreaker(standings, indexes, teamsStillTied);
                            }

                            teamsStillTied.Clear();
                        }
                    }
                }
            }

            return standings;
        }

        private bool AreRecordsIdentical(int[] team1, int[] team2)
        {
            const int WINS = 0, LOSSES = 1;
            return team1[WINS] == team2[WINS] && team1[LOSSES] == team2[LOSSES];
        }

        private int[] UpdateMultiWayH2HTeamStats(HeadToHeadSeries matchup, Team team, int[] stats)
        {
            const int GAMES_PLAYED = 0, WINS = 1, LOSSES = 2;
            bool isTeam1 = team == matchup.Team1;
            
            stats[GAMES_PLAYED] += matchup.Team1Wins + matchup.Team2Wins;
            stats[WINS] += isTeam1 ? matchup.Team1Wins : matchup.Team2Wins;
            stats[LOSSES] += isTeam1 ? matchup.Team2Wins : matchup.Team1Wins;

            return stats;
        }

        private List<Standings> SwapRankings(List<Standings> standings, Standings team1, Standings team2, string level)
        {
            int hold;
            
            if (level == "division")
            {
                hold = team1.DivisionRanking;
                team1.DivisionRanking = team2.DivisionRanking;
                team2.DivisionRanking = hold;
            }
            else if (level == "conference")
            {
                hold = team1.ConferenceRanking;
                team1.ConferenceRanking = team2.ConferenceRanking;
                team2.ConferenceRanking = hold;
            }
            else if (level == "playoffs")
            {
                hold = team1.PlayoffRanking;
                team1.PlayoffRanking = team2.PlayoffRanking;
                team2.PlayoffRanking = hold;
            }
            else
            {
                hold = team1.LeagueRanking;
                team1.LeagueRanking = team2.LeagueRanking;
                team2.LeagueRanking = hold;
            }

            _localContext.Standings.Update(team1);
            _localContext.Standings.Update(team2);
            _localContext.SaveChanges();

            return standings;
        }

        private List<Standings> SwapTeams(List<Standings> standings, 
                                          int index1, Standings team1, 
                                          int index2, Standings team2)
        {
            standings[index1] = team2;
            standings[index2] = team1;

            return standings;
        }

        private List<Standings> ReorderTeams(List<Standings> standings, List<int> indexes, List<Standings> teams)
        {
            for (int loopIndex = 0; loopIndex < teams.Count; loopIndex++)
            {
                int index = indexes[loopIndex];
                Standings team = teams[loopIndex];

                standings[index] = team;
            }

            return standings;
        }

        private List<Team> ExtractTeams(List<Standings> teamList)
        {
            List<Team> teams = new List<Team>();

            foreach (var team in teamList)
                teams.Add(team.Team);

            return teams;
        }

        private Standings GetTeamStatLine(int season, Team team)
        {
            return _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Season.Year == season &&
                                     s.Team == team)!;
        }

        private List<Standings> ExtractStatLines(int season, List<KeyValuePair<Team, int[]>> teamList)
        {
            List<Standings> teams = new List<Standings>();

            foreach (var team in teamList)
            {
                var statLine = GetTeamStatLine(season, team.Key);
                teams.Add(statLine);
            }

            return teams;
        }

        private async Task UpdateTeamStats(int season, Team team)
        {
            var teamStats = await _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.Team == team)
                .FirstOrDefaultAsync();

            teamStats.Wins = await GetWins(season, team);
            teamStats.Losses = await GetLosses(season, team);
            teamStats.GamesPlayed = teamStats.Wins + teamStats.Losses;
            teamStats.WinPct = (teamStats.GamesPlayed > 0) ?
                100 * ((decimal)teamStats.Wins / teamStats.GamesPlayed) :
                0;

            int[] goalRatio = await GetGoalRatio(season, team);
            teamStats.GoalsFor = goalRatio[0];
            teamStats.GoalsAgainst = goalRatio[1];
            teamStats.GoalDifferential = teamStats.GoalsFor - teamStats.GoalsAgainst;

            int[] winsByType = await GetWinsByType(season, team);
            teamStats.Streak = await GetStreak(season, team);
            teamStats.RegulationWins = winsByType[0];
            teamStats.RegPlusOTWins = winsByType[1];

            teamStats.WinsVsDivision = await GetWins(season, team, DIVISION);
            teamStats.LossesVsDivision = await GetLosses(season, team, DIVISION);
            teamStats.GamesPlayedVsDivision = teamStats.WinsVsDivision + teamStats.LossesVsDivision;
            teamStats.WinPctVsDivision = (teamStats.GamesPlayedVsDivision > 0) ?
                100 * ((decimal)teamStats.WinsVsDivision / teamStats.GamesPlayedVsDivision) :
                0;

            teamStats.WinsVsConference = await GetWins(season, team, CONFERENCE);
            teamStats.LossesVsConference = await GetLosses(season, team, CONFERENCE);
            teamStats.GamesPlayedVsConference = teamStats.WinsVsConference + teamStats.LossesVsConference;
            teamStats.WinPctVsConference = (teamStats.GamesPlayedVsConference > 0) ?
                100 * ((decimal)teamStats.WinsVsConference / teamStats.GamesPlayedVsConference) :
                0;

            teamStats.InterConfWins = await GetWins(season, team, INTER_CONFERENCE);
            teamStats.InterConfLosses = await GetLosses(season, team, INTER_CONFERENCE);
            teamStats.InterConfGamesPlayed = teamStats.InterConfWins + teamStats.InterConfLosses;
            teamStats.InterConfWinPct = (teamStats.InterConfGamesPlayed > 0) ?
                100 * ((decimal)teamStats.InterConfWins / teamStats.InterConfGamesPlayed) :
                0;

            int[] recordInLast10Games = await GetRecordInLast10Games(season, team);
            teamStats.WinsInLast10Games = recordInLast10Games[0];
            teamStats.LossesInLast10Games = recordInLast10Games[1];
            teamStats.GamesPlayedInLast10Games = teamStats.WinsInLast10Games + teamStats.LossesInLast10Games;
            teamStats.WinPctInLast10Games = teamStats.GamesPlayedInLast10Games > 0 ?
                100 * ((decimal)teamStats.WinsInLast10Games / teamStats.GamesPlayedInLast10Games) :
                0;

            Game? nextGame = await GetNextGame(season, team);
            //teamStats.NextGame = nextGame;

            _localContext.Standings.Update(teamStats);
            await _localContext.SaveChangesAsync();

            await UpdateGamesBehind(season);
        }

        private async Task<int[]> GetRecordInLast10Games(int season, Team team)
        {
            const int WINS = 0, LOSSES = 1;
            int[] record = { 0, 0 };

            var last10Games = await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == GameTypes.REGULAR_SEASON &&
                            (s.AwayTeam == team || s.HomeTeam == team) &&
                            s.IsFinalized && s.Period >= 3)
                .OrderByDescending(s => s.Date.Date)
                .Take(10)
                .ToListAsync();

            record[WINS] = last10Games.Count(g => (g.HomeTeam == team && g.HomeScore > g.AwayScore) ||
                                                  (g.AwayTeam == team && g.AwayScore > g.HomeScore));
            record[LOSSES] = last10Games.Count - record[WINS];

            return record;
        }

        private async Task<Game?> GetNextGame(int season, Team team)
        {
            return await _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .OrderBy(s => s.Date.Date)
                .FirstOrDefaultAsync(s => s.Season.Year == season &&
                                          s.Type == GameTypes.REGULAR_SEASON &&
                                          s.Period == 0 &&
                                          (s.AwayTeam == team || s.HomeTeam == team))!;
        }

        private IQueryable<Alignment> GetTeams(int season)
        {
            return _localContext.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);
        }

        private Alignment GetAlignment(int season, Team team)
        {
            var teams = GetTeams(season);
            return teams.First(t => t.Team == team);
        }

        private Conference GetConference(int season, Team team)
        {
            var alignment = GetAlignment(season, team);
            return alignment.Conference!;
        }

        private IQueryable<Game> GetGamesPlayed(int season, Team team)
        {
            return _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == GameTypes.REGULAR_SEASON &&
                            (s.AwayTeam == team || s.HomeTeam == team) &&
                            s.IsFinalized &&
                            s.Period >= 3)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);
        }

        private IQueryable<Game> GetGamesWon(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

            return gamesPlayed
                .Where(g => (g.AwayTeam == team && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == team && g.HomeScore > g.AwayScore));
        }

        private IQueryable<Game> GetGamesLost(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

            return gamesPlayed
                .Where(g => (g.AwayTeam == team && g.AwayScore < g.HomeScore) ||
                            (g.HomeTeam == team && g.HomeScore < g.AwayScore));
        }

        private async Task<int> GetWins(int season, Team team, string vsGroup = "overall")
        {
            var teams = _localContext.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);

            var gamesPlayed = await GetGamesPlayed(season, team).ToListAsync();
            var gamesWon = await GetGamesWon(season, team).ToListAsync();

            Conference conference = teams
                .Where(t => t.Team == team)
                .Select(t => t.Conference)
                .First()!;

            Division division = teams
                .Where(t => t.Team == team)
                .Select(t => t.Division)
                .First()!;

            if (gamesPlayed.Any())
            {
                int wins = 0;

                foreach (var game in gamesWon)
                {
                    var opponentsAlignment = teams
                        .Where(t => t.Team == (game.HomeTeam == team ? game.AwayTeam : game.HomeTeam));

                    Conference opponentsConference = opponentsAlignment
                        .Select(a => a.Conference)
                        .First()!;

                    Division opponentsDivision = opponentsAlignment
                        .Select(t => t.Division)
                        .First()!;

                    switch (vsGroup)
                    {
                        case DIVISION:
                            if (opponentsDivision == division)
                                wins++;

                            break;

                        case CONFERENCE:
                            if (opponentsConference == conference && opponentsDivision != division)
                                wins++;

                            break;

                        case INTER_CONFERENCE:
                            if (opponentsConference != conference)
                                wins++;

                            break;

                        default:
                            return gamesWon.Count();
                    }
                }

                return wins;
            }
            else
                return 0;
        }

        private async Task<int[]> GetWinsByType(int season, Team team)
        {
            var gamesWon = await GetGamesWon(season, team).ToListAsync();
            int[] winsByType = { 0, 0 };
            const int RW = 0, ROW = 1;

            if (gamesWon.Any())
            {
                foreach (var game in gamesWon)
                {
                    if (game.Period < 5)
                    {
                        if (game.Period == 3)
                            winsByType[RW]++;

                        winsByType[ROW]++;
                    }
                }
            }

            return winsByType;
        }

        private async Task<int> GetLosses(int season, Team team, string vsGroup = "overall")
        {
            var teams = _localContext.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);

            var gamesPlayed = await GetGamesPlayed(season, team).ToListAsync();
            var gamesLost = await GetGamesLost(season, team).ToListAsync();

            Conference conference = teams
                .Where(t => t.Team == team)
                .Select(t => t.Conference)
                .First()!;

            Division division = teams
                .Where(t => t.Team == team)
                .Select(t => t.Division)
                .First()!;

            if (gamesPlayed.Any())
            {
                int losses = 0;

                foreach (var game in gamesLost)
                {
                    var opponentsAlignment = teams
                        .Where(t => t.Team == (game.HomeTeam == team ? game.AwayTeam : game.HomeTeam));

                    Conference opponentsConference = opponentsAlignment
                        .Select(a => a.Conference)
                        .First()!;

                    Division opponentsDivision = opponentsAlignment
                        .Select(t => t.Division)
                        .First()!;

                    switch (vsGroup)
                    {
                        case DIVISION:
                            if (opponentsDivision == division)
                                losses++;

                            break;

                        case CONFERENCE:
                            if (opponentsConference == conference && opponentsDivision != division)
                                losses++;

                            break;

                        case INTER_CONFERENCE:
                            if (opponentsConference != conference)
                                losses++;

                            break;

                        default:
                            return gamesLost.Count();
                    }
                }

                return losses;
            }
            else
                return 0;
        }

        private async Task<int> GetStreak(int season, Team team)
        {
            var gamesPlayed = await GetGamesPlayed(season, team).ToListAsync();

            int streak = 0;
            foreach (var game in gamesPlayed)
            {
                if (game.HomeTeam == team)
                {
                    if (game.HomeScore > game.AwayScore)
                    {
                        if (streak >= 0)
                            streak++;
                        else
                            streak = 1;
                    }
                    else
                    {
                        if (streak <= 0)
                            streak--;
                        else
                            streak = -1;
                    }
                }
                else
                {
                    if (game.AwayScore > game.HomeScore)
                    {
                        if (streak >= 0)
                            streak++;
                        else
                            streak = 1;
                    }
                    else
                    {
                        if (streak <= 0)
                            streak--;
                        else
                            streak = -1;
                    }
                }
            }

            return streak;
        }

        private async Task<int[]> GetGoalRatio(int season, Team team)
        {
            var gamesPlayed = await GetGamesPlayed(season, team).ToListAsync();

            int[] goalRatio = { 0, 0 };
            const int GF = 0, GA = 1;

            if (gamesPlayed.Any())
            {
                foreach (var game in gamesPlayed)
                {
                    if (game.HomeTeam == team)
                    {
                        goalRatio[GF] += game.HomeScore;
                        goalRatio[GA] += game.AwayScore;
                    }
                    else
                    {
                        goalRatio[GF] += game.AwayScore;
                        goalRatio[GA] += game.HomeScore;
                    }
                }
            }

            return goalRatio;
        }

        private async Task UpdateGamesBehind(int season)
        {
            await UpdateGamesBehind(season, DIVISION);
            await UpdateGamesBehind(season, CONFERENCE);
            await UpdateGamesBehind(season, "league");

            await _localContext.SaveChangesAsync();
        }

        private async Task UpdateGamesBehind(int season, string groupBy)
        {
            var standings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            Standings leader;

            switch (groupBy)
            {
                case DIVISION:
                    standings = standings
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
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name);

                    var divisions = standings
                        .Select(s => s.Division)
                        .Distinct();

                    foreach (var division in divisions)
                    {
                        var divisionStandings = standings
                            .Where(s => s.Division == division);

                        leader = divisionStandings.First();

                        foreach (var team in divisionStandings)
                        {
                            team.DivisionGamesBehind = (team.TeamId != leader.TeamId) ?
                                (decimal)((leader.Wins - leader.Losses) - (team.Wins - team.Losses)) / 2 :
                                0;
                        }
                    }

                    break;

                case CONFERENCE:
                    standings = standings
                        .OrderByDescending(s => s.WinPct)
                        .ThenByDescending(s => s.Wins)
                        .ThenBy(s => s.Losses)
                        .ThenByDescending(s => s.RegulationWins)
                        .ThenByDescending(s => s.RegPlusOTWins)
                        .ThenByDescending(s => s.WinPctVsConference)
                        .ThenByDescending(s => s.WinsVsConference)
                        .ThenBy(s => s.LossesVsConference)
                        .ThenByDescending(s => s.WinPctVsDivision)
                        .ThenByDescending(s => s.WinsVsDivision)
                        .ThenBy(s => s.LossesVsDivision)
                        .ThenByDescending(s => s.InterConfWinPct)
                        .ThenByDescending(s => s.InterConfWins)
                        .ThenBy(s => s.InterConfLosses)
                        .ThenByDescending(s => s.GoalDifferential)
                        .ThenByDescending(s => s.GoalsFor)
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name);

                    var conferences = standings
                        .Select(s => s.Conference)
                        .Distinct();

                    foreach (var conference in conferences)
                    {
                        var conferenceStandings = standings
                            .Where(s => s.ConferenceId == conference.Id);

                        leader = conferenceStandings.First();

                        foreach (var team in conferenceStandings)
                        {
                            team.ConferenceGamesBehind = (team.TeamId != leader.TeamId) ?
                                (decimal)((leader.Wins - leader.Losses) - (team.Wins - team.Losses)) / 2 :
                                0;
                        }
                    }

                    break;

                default:
                    standings = standings
                        .OrderByDescending(s => s.WinPct)
                        .ThenByDescending(s => s.Wins)
                        .ThenBy(s => s.Losses)
                        .ThenByDescending(s => s.RegulationWins)
                        .ThenByDescending(s => s.RegPlusOTWins)
                        .ThenByDescending(s => s.InterConfWinPct)
                        .ThenByDescending(s => s.InterConfWins)
                        .ThenBy(s => s.InterConfLosses)
                        .ThenByDescending(s => s.WinPctVsConference)
                        .ThenByDescending(s => s.WinsVsConference)
                        .ThenBy(s => s.LossesVsConference)
                        .ThenByDescending(s => s.WinPctVsDivision)
                        .ThenByDescending(s => s.WinsVsDivision)
                        .ThenBy(s => s.LossesVsDivision)
                        .ThenByDescending(s => s.GoalDifferential)
                        .ThenByDescending(s => s.GoalsFor)
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name);

                    leader = standings.First();

                    foreach (var team in standings)
                    {
                        team.LeagueGamesBehind = (team.TeamId != leader.TeamId) ?
                            (decimal)((leader.Wins - leader.Losses) - (team.Wins - team.Losses)) / 2 :
                            0;
                    }

                    break;
            }
        }

        private async Task StandingsUpdateNowAvailable()
        {
            var flag = _localContext.ProgramFlags
                .Where(f => f.Description == "New Standings Update Available")
                .FirstOrDefault();

            flag!.State = true;

            _localContext.ProgramFlags.Update(flag);
            await _localContext.SaveChangesAsync();
        }

        private IActionResult ErrorMessage(string message)
        {
            var errorMessage = new ErrorViewModel { Description = message };
            return View("Error", errorMessage);
        }
    }
}
