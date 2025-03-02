using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.InputForms;
using SycamoreHockeyLeaguePortal.Models.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        Random random = new Random();

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        private const string DIVISION = "division";
        private const string CONFERENCE = "conference";
        private const string INTER_CONFERENCE = "inter-conference";

        private int SEASON;
        private const int TEAM1 = 0, TEAM2 = 1;

        private readonly IConfigurationSection _secrets;

        public ScheduleController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
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

            var seasons = await _context.Seasons
                .OrderByDescending(s => s.Year)
                .ToListAsync();
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var season = weekOf.Year;

            var teams = await _context.Alignments
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

            IQueryable<Schedule> schedule = _context.Schedule
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
            if (season == 2021 && round >= 4)
                return RedirectToAction(nameof(Playoffs), new { season = season, round = 3, team = team });

            if (season >= 2022 && round > 4)
                return RedirectToAction(nameof(Playoffs), new { season = season, round = 4, team = team });

            ViewBag.Season = season;
            ViewBag.Team = team;
            ViewBag.Round = round;

            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var rounds = _context.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season)
                .OrderBy(r => r.Index);
            ViewBag.Rounds = new SelectList(rounds, "Index", "Name");

            string roundName = rounds
                .Where(r => r.Index == round)
                .Select(r => r.Name)
                .First();
            ViewBag.RoundName = roundName;

            var teams = _context.Schedule
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

            IQueryable<Schedule> playoffs;
            if (team != null)
            {
                playoffs = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.PlayoffRound)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == PLAYOFFS &&
                                s.PlayoffRound!.Index == round &&
                                (s.AwayTeam.Code == team ||
                                 s.HomeTeam.Code == team))
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }
            else
            {
                playoffs = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.PlayoffRound)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == PLAYOFFS &&
                                s.PlayoffRound!.Index == round)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }

            List<DateTime> dates = await playoffs
                .Select(p => p.Date.Date)
                .Distinct()
                .ToListAsync();
            ViewBag.Dates = dates;

            return View(await playoffs.AsNoTracking().ToListAsync());
        }

        [Route("Schedule/UploadSchedule/{season}")]
        public IActionResult UploadSchedule(int season)
        {
            var form = new Schedule_UploadForm
            {
                Season = season
            };
            return View(form);
        }

        [HttpPost]
        public async Task<IActionResult> UploadSchedule(Schedule_UploadForm form)
        {
            var season = _context.Seasons.FirstOrDefault(s => s.Year == form.Season)!;

            if (season == null)
                return NotFound($"There is no {form.Season} season in the database.");

            var schedule = _context.Schedule.Where(s => s.Season == season);
            if (schedule.Any())
                return BadRequest($"There is already a schedule for the {season.Year} season.");

            DateTime firstDay = DateTime.Now;
            if (form.File != null && form.File.Length > 0)
            {
                var extension = Path.GetExtension(form.File.FileName).ToLower();
                if (extension == ".csv") 
                {
                    using (var streamReader = new StreamReader(form.File.OpenReadStream()))
                    using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                    {
                        var games = csvReader.GetRecords<ScheduleCSV>().ToList();
                        firstDay = games.First().Date;

                        int gameIndex = _context.Schedule.Max(s => s.GameIndex);
                        int index = 1;
                        foreach (var _game in games)
                        {
                            var awayTeam = _context.Teams.FirstOrDefault(t => t.Code == _game.AwayTeam)!;
                            var homeTeam = _context.Teams.FirstOrDefault(t => t.Code == _game.HomeTeam)!;

                            var game = new Schedule
                            {
                                Id = Guid.NewGuid(),
                                SeasonId = season.Id,
                                Season = season,
                                Date = _game.Date,
                                GameIndex = gameIndex + index,
                                Type = "Regular Season",
                                AwayTeamId = awayTeam.Id,
                                AwayTeam = awayTeam,
                                HomeTeamId = homeTeam.Id,
                                HomeTeam = homeTeam
                            };
                            _context.Schedule.Add(game);

                            index++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { weekOf = firstDay.ToShortDateString() });
                }
            }

            return View(season.Year);
        }

        [Route("Schedule/PlayoffSeries/{season}/{team1}/{team2}")]
        [AllowAnonymous]
        public async Task<IActionResult> PlayoffSeries(int season, string team1, string team2)
        {
            var playoffSeries = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season &&
                            ((s.Team1.Code == team1 && s.Team2.Code == team2) ||
                             (s.Team1.Code == team2 && s.Team2.Code == team1)))
                .FirstOrDefault()!;

            Conference conference = _context.Alignments
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

            var schedule = await _context.Schedule
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
            if (_context.Schedule == null)
            {
                return NotFound();
            }

            var game = await _context.Schedule
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

            return View(game);
        }

        // GET: Schedule/Edit/5
        [Route("GameControls/{date}/{awayTeam}/{homeTeam}")]
        public async Task<IActionResult> GameControls(DateTime date, string awayTeam, string homeTeam)
        {
            if (_context.Schedule == null)
            {
                return NotFound();
            }

            var game = await _context.Schedule
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

            TempData["APIDomain"] = _secrets.GetValue<string>("API:LocalURL");
            TempData["MVCDomain"] = _secrets.GetValue<string>("MVC:LocalURL");
            TempData["Endpoints"] = _secrets.GetSection("API:Endpoints:Schedule");

            return View(game);
        }

        public async Task<Schedule> GetGameAsync(Guid? id)
        {
            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Id == id);

            return game!;
        }

        public async Task UpdateGameAsync(Schedule game)
        {
            _context.Schedule.Update(game);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> SaveGame(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive)
            {
                game.IsLive = false;
                await UpdateGameAsync(game);
            }

            if (game.Type == PLAYOFFS)
                return RedirectToAction(nameof(Playoffs), new { season = game.Date.Year, round = game.PlayoffRound!.Index });

            return RedirectToAction(nameof(Index), new { weekOf = game.Date.ToShortDateString() });
        }

        public async Task<IActionResult> FinalizeGame(Guid id)
        {
            var game = await GetGameAsync(id);

            if (IsGameLive(game) && game.Period >= 3 && game.AwayScore != game.HomeScore)
            {
                game.IsLive = false;
                game.IsFinalized = true;
                await UpdateGameAsync(game);

                if (game.Type == REGULAR_SEASON)
                {
                    await UpdateStandings(game.Season.Year, game.AwayTeam, game.HomeTeam);
                    await UpdateH2HSeries(game);
                }   

                if (game.Type == PLAYOFFS)
                    await UpdatePlayoffSeries(game.Season.Year, game);

                return RedirectToAction(nameof(GameCenter), new
                {
                    date = game.Date.ToShortDateString(),
                    awayTeam = game.AwayTeam.Code,
                    homeTeam = game.HomeTeam.Code
                });
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        private async Task UpdateH2HSeries(Schedule game)
        {
            var matchup = await _context.HeadToHeadSeries
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefaultAsync(s => s.Season.Year == game.Season.Year &&
                                          ((s.Team1 == game.AwayTeam && s.Team2 == game.HomeTeam) ||
                                           (s.Team1 == game.HomeTeam && s.Team2 == game.AwayTeam)));

            if (matchup!.Team1 == game.HomeTeam)
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

            _context.HeadToHeadSeries.Update(matchup);
            await _context.SaveChangesAsync();
        }

        private bool IsGameLive(Schedule game)
        {
            return game.IsLive && !game.IsFinalized;
        }

        private bool ScheduleExists(Guid id)
        {
            return (_context.Schedule?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public IActionResult Formula()
        {
            return View();
        }

        private async Task UpdatePlayoffSeries(int season, Schedule game)
        {
            var series = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season &&
                            ((s.Team1 == game.HomeTeam && s.Team2 == game.AwayTeam) ||
                             (s.Team1 == game.AwayTeam && s.Team2 == game.HomeTeam)))
                .FirstOrDefault()!;

            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == PLAYOFFS &&
                            ((s.AwayTeam == game.AwayTeam && s.HomeTeam == game.HomeTeam) ||
                             (s.AwayTeam == game.HomeTeam && s.HomeTeam == game.AwayTeam)))
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);

            var gamesPlayed = schedule.Where(g => g.IsFinalized);

            series.Team1Wins = gamesPlayed
                .Count(g => (g.AwayTeam == series.Team1 && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == series.Team1 && g.HomeScore > g.AwayScore));
            series.Team2Wins = gamesPlayed
                .Count(g => (g.AwayTeam == series.Team2 && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == series.Team2 && g.HomeScore > g.AwayScore));

            _context.PlayoffSeries.Update(series);

            game.Notes = series.SeriesScoreString;
            game.PlayoffSeriesScore = series.ShortSeriesScoreString;
            _context.Schedule.Update(game);

            var remainingGames = schedule.Where(g => g.Date.Date.CompareTo(game.Date.Date) > 0);
            if (remainingGames.Any())
            {
                var nextGame = remainingGames.First();

                nextGame.Notes = remainingGames.Count() > 1 ? series.SeriesScoreString : "Game 7";
                nextGame.PlayoffSeriesScore = series.ShortSeriesScoreString;
                _context.Schedule.Update(nextGame);
            }

            if (series.Team1Wins == 4 || series.Team2Wins == 4)
            {
                if (remainingGames.Any())
                {
                    foreach (var _game in remainingGames)
                        _context.Schedule.Remove(_game);
                }

                if ((series.Season.Year == 2021 && series.Round.Index == 3) ||
                    (series.Season.Year >= 2022 && series.Round.Index == 4))
                {
                    Team _champion = (series.Team1Wins == 4) ? series.Team1 : series.Team2;

                    var champion = new Champion
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = series.SeasonId,
                        Season = series.Season,
                        TeamId = _champion.Id,
                        Team = _champion
                    };
                    _context.Champions.Add(champion);

                    var roundsWon = _context.PlayoffSeries
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
                        Team opponent = (champion.Team == round.Team1) ? round.Team2 : round.Team1;

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
                        _context.ChampionsRounds.Add(roundWon);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateStandings(int season, Team awayTeam, Team homeTeam)
        {
            await UpdateTeamStats(season, awayTeam);
            await UpdateTeamStats(season, homeTeam);
            await UpdateRankings(season);

            await StandingsUpdateNowAvailable();
        }

        private async Task UpdateRankings(int season)
        {
            List<Standings> standings = await GetStandingsAsync(season);

            int ranking = 1;
            foreach (var team in standings)
            {
                team.LeagueRanking = ranking;
                _context.Standings.Update(team);

                ranking++;
            }

            var conferences = standings.Select(s => s.Conference).Distinct();
            foreach (var conference in conferences)
            {
                ranking = 1;
                
                var conferenceStandings = standings.Where(s => s.Conference == conference).ToList();
                conferenceStandings = ApplyHeadToHeadTiebreakers(conferenceStandings);

                foreach (var team in conferenceStandings)
                {
                    team.ConferenceRanking = ranking;
                    _context.Standings.Update(team);

                    ranking++;
                }

                ranking = 1;
                
                var leaders = GetLeaders(conferenceStandings);
                leaders = ApplyHeadToHeadTiebreakers(leaders);
                foreach (var team in leaders)
                {
                    team.PlayoffRanking = ranking;
                    _context.Standings.Update(team);

                    ranking++;
                }

                var wildCards = GetWildCards(conferenceStandings, leaders);
                wildCards = ApplyHeadToHeadTiebreakers(wildCards);
                foreach (var team in wildCards)
                {
                    team.PlayoffRanking = ranking;
                    _context.Standings.Update(team);

                    ranking++;
                }
            }

            var divisions = standings.Select(s => s.Division).Distinct();
            foreach (var division in divisions)
            {
                ranking = 1;

                var divisionStandings = standings.Where(s => s.Division == division).ToList();
                divisionStandings = ApplyHeadToHeadTiebreakers(divisionStandings);

                foreach (var team in divisionStandings)
                {
                    team.DivisionRanking = ranking;
                    _context.Standings.Update(team);

                    ranking++;
                }
            }

            await _context.SaveChangesAsync();
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

            var wildCards = GetWildCards(standings, leaders);
            return wildCards.IndexOf(team) + (leaders.Count + 1);
        }

        private List<Standings> GetWildCards(List<Standings> standings, List<Standings>? leaders = null)
        {
            leaders ??= GetLeaders(standings);
            List<Standings> wildCards = new List<Standings>();

            foreach (var team in standings)
            {
                if (!leaders.Contains(team))
                    wildCards.Add(team);
            }

            return wildCards;
        }

        private List<Standings> GetLeaders(List<Standings> standings)
        {
            var divisions = standings
                .Select(s => s.Division)
                .Distinct();

            List<Standings> leaders = new List<Standings>();

            foreach (var division in divisions)
            {
                var leader = standings
                    .Where(s => s.Division == division)
                    .First();

                leaders.Add(leader);
            }

            return leaders;
        }

        private async Task<List<Standings>> GetStandingsAsync(int season)
        {
            IQueryable<Standings> standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            if (season <= 2022)
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
            else
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

            List<Standings> standingsList = standings.ToList();

            if (season >= 2025)
                standingsList = ApplyHeadToHeadTiebreakers(standingsList);

            return standingsList;
        }

        private List<Standings> ApplyHeadToHeadTiebreakers(List<Standings> standings)
        {
            SEASON = GetSeason(standings);

            List<Standings> tiedTeams = new List<Standings>();
            List<int> indexes;
            for (int index = 0; index < standings.Count - 1; index++)
            {
                Standings currentTeam = standings[index];
                Standings nextTeam = standings[index + 1];

                if (currentTeam.Wins == nextTeam.Wins &&
                    currentTeam.Losses == nextTeam.Losses)
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
            var matchup = _context.HeadToHeadSeries
                .Include(m => m.Season)
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .FirstOrDefault(m => m.Season.Year == SEASON &&
                                     ((m.Team1 == team1.Team && m.Team2 == team2.Team) ||
                                      (m.Team1 == team2.Team && m.Team2 == team1.Team)))!;

            if (matchup.Team1Wins == matchup.Team2Wins)
                return standings;

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

            return standings;
        }

        private List<Standings> ApplyMultiWayH2HTiebreaker(List<Standings> standings, List<int> indexes, List<Standings> tiedTeams)
        {
            const int GAMES_PLAYED = 0, WINS = 1, LOSSES = 2;

            Dictionary<Team, int[]> records = new();

            foreach (var team in tiedTeams)
                records.Add(team.Team, new int[] { 0, 0, 0 });

            for (int index = 0; index < tiedTeams.Count - 1; index++)
            {
                for (int nextIndex = index + 1; nextIndex < tiedTeams.Count; nextIndex++)
                {
                    Team currentTeam = tiedTeams[index].Team;
                    Team nextTeam = tiedTeams[nextIndex].Team;

                    var matchup = _context.HeadToHeadSeries
                        .Include(s => s.Season)
                        .Include(s => s.Team1)
                        .Include(s => s.Team2)
                        .FirstOrDefault(s => s.Season.Year == SEASON &&
                                             ((s.Team1 == currentTeam && s.Team2 == nextTeam) ||
                                              (s.Team1 == nextTeam && s.Team2 == currentTeam)))!;

                    int gamesPlayed = matchup.Team1Wins + matchup.Team2Wins;
                    if (gamesPlayed > 0)
                    {
                        records[currentTeam][GAMES_PLAYED] += gamesPlayed;
                        records[nextTeam][GAMES_PLAYED] += gamesPlayed;

                        if (currentTeam == matchup!.Team1)
                        {
                            records[currentTeam][WINS] += matchup.Team1Wins;
                            records[currentTeam][LOSSES] += matchup.Team2Wins;

                            records[nextTeam][WINS] += matchup.Team2Wins;
                            records[nextTeam][LOSSES] += matchup.Team1Wins;
                        }
                        else
                        {
                            records[currentTeam][WINS] += matchup.Team2Wins;
                            records[currentTeam][LOSSES] += matchup.Team1Wins;

                            records[nextTeam][WINS] += matchup.Team1Wins;
                            records[nextTeam][LOSSES] += matchup.Team2Wins;
                        }
                    }
                }
            }

            if (records.Count(r => r.Value[GAMES_PLAYED] == 0) < records.Count)
            {
                var tiebreaker = records
                    .OrderByDescending(r => (r.Value[GAMES_PLAYED] > 0) ?
                                                (decimal)(r.Value[WINS] / r.Value[GAMES_PLAYED]) : 0)
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
                                                         indexes[0], teamsStillTied[0],
                                                         indexes[1], teamsStillTied[1]) :
                                ApplyMultiWayH2HTiebreaker(standings, indexes, teamsStillTied);
                        }
                    }
                    else
                    {
                        if (teamsStillTied.Any())
                        {
                            if (records[teamsStillTied.First().Team][GAMES_PLAYED] > 0)
                            {
                                indexes = GetIndexesInLeagueStandings(standings, teamsStillTied);

                                standings = teamsStillTied.Count == 2 ?
                                    ApplyTwoWayH2HTiebreaker(standings,
                                                             indexes[0], teamsStillTied[0],
                                                             indexes[1], teamsStillTied[1]) :
                                    ApplyMultiWayH2HTiebreaker(standings, indexes, teamsStillTied);
                            }

                            teamsStillTied.Clear();
                        }
                    }
                }
            }

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
            return _context.Standings
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
            var teamStats = await _context.Standings
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

            Schedule nextGame = await GetNextGame(season, team);

            _context.Standings.Update(teamStats);
            await _context.SaveChangesAsync();

            await UpdateGamesBehind(season);
        }

        private async Task<int[]> GetRecordInLast10Games(int season, Team team)
        {
            var gamesPlayed = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            (s.AwayTeam == team || s.HomeTeam == team) &&
                            s.IsFinalized && s.Period >= 3)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex)
                .ToListAsync();

            int gamesPlayedCount = gamesPlayed.Count;
            int[] record = { 0, 0 };
            const int WINS = 0, LOSSES = 1;
            for (int index = gamesPlayedCount - 1; index >= 0 && index >= gamesPlayedCount - 10; index--)
            {
                Schedule game = gamesPlayed[index];
                int recordIndex;

                if (game.HomeTeam == team)
                {
                    recordIndex = (game.HomeScore > game.AwayScore) ? WINS : LOSSES;
                    record[recordIndex]++;
                }
                else
                {
                    recordIndex = (game.AwayScore > game.HomeScore) ? WINS : LOSSES;
                    record[recordIndex]++;
                }
            }

            return record;
        }

        private async Task<Schedule> GetNextGame(int season, Team team)
        {
            return await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == REGULAR_SEASON &&
                            s.Period == 0 &&
                            (s.AwayTeam == team || s.HomeTeam == team))
                .FirstOrDefaultAsync()!;
        }

        private IQueryable<Alignment> GetTeams(int season)
        {
            return _context.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);
        }

        private Alignment GetAlignment(int season, Team team)
        {
            var teams = GetTeams(season);

            return teams
                .Where(t => t.Team == team)
                .First();
        }

        private Conference GetConference(int season, Team team)
        {
            var alignment = GetAlignment(season, team);
            return alignment.Conference!;
        }

        private IQueryable<Schedule> GetGamesPlayed(int season, Team team)
        {
            return _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == REGULAR_SEASON &&
                            (s.AwayTeam == team || s.HomeTeam == team) &&
                            s.IsFinalized &&
                            s.Period >= 3)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);
        }

        private IQueryable<Schedule> GetGamesWon(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

            return gamesPlayed
                .Where(g => (g.AwayTeam == team && g.AwayScore > g.HomeScore) ||
                            (g.HomeTeam == team && g.HomeScore > g.AwayScore));
        }

        private IQueryable<Schedule> GetGamesLost(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

            return gamesPlayed
                .Where(g => (g.AwayTeam == team && g.AwayScore < g.HomeScore) ||
                            (g.HomeTeam == team && g.HomeScore < g.AwayScore));
        }

        private async Task<int> GetWins(int season, Team team, string vsGroup = "overall")
        {
            var teams = _context.Alignments
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
            var teams = _context.Alignments
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

            await _context.SaveChangesAsync();
        }

        private async Task UpdateGamesBehind(int season, string groupBy)
        {
            var standings = _context.Standings
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
                            .Where(s => s.DivisionId == division.Id);

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
            var flag = _context.ProgramFlags
                .Where(f => f.Description == "New Standings Update Available")
                .FirstOrDefault();

            flag!.State = true;

            _context.ProgramFlags.Update(flag);
            await _context.SaveChangesAsync();
        }
    }
}
