using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.ViewModels;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        Random random = new Random();

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        private const string DIVISION = "division";
        private const string CONFERENCE = "conference";
        private const string INTER_CONFERENCE = "inter-conference";

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedule
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

        public async Task<IActionResult> Playoffs(int season, int round, string? team)
        {
            if (season == 2021 && round == 4)
            {
                round--;
                return RedirectToAction(nameof(Playoffs), new { season = season, round = round, team = team });
            }

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

        [Route("Schedule/PlayoffSeries/{season}/{team1}/{team2}")]
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

        // GET: Schedule/Create
        public IActionResult Create()
        {
            var teams = _context.Teams
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);

            ViewData["AwayTeamId"] = new SelectList(teams, "Id", "FullName");
            ViewData["HomeTeamId"] = new SelectList(teams, "Id", "FullName");
            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year");
            return View();
        }

        // POST: Schedule/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,PlayoffRoundId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,IsFinalized,Notes")] Schedule game)
        {
            game.Id = Guid.NewGuid();
            game.GameIndex = (_context.Schedule.Any()) ?
                _context.Schedule
                    .Select(s => s.GameIndex)
                    .Max() + 1 :
                1;
            game.Season = _context.Seasons.FirstOrDefault(s => s.Id == game.SeasonId)!;
            game.PlayoffRound = _context.PlayoffRounds.FirstOrDefault(r => r.Id == game.PlayoffRoundId) ?? null;
            game.AwayTeam = _context.Teams.FirstOrDefault(t => t.Id == game.AwayTeamId)!;
            game.HomeTeam = _context.Teams.FirstOrDefault(t => t.Id == game.HomeTeamId)!;

            _context.Add(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { season = game.Season.Year });
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

            if (!game.IsFinalized && !game.IsLive)
            {
                game.IsLive = true;

                if (game.Period == 0)
                    game.Period = 1;

                _context.Schedule.Update(game);
                await _context.SaveChangesAsync();
            }

            return View(game);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GameControls(Guid? id, [Bind("Id,SeasonId,PlayoffRoundId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,IsFinalized,Notes,GameIndex")] Schedule game, bool finalized = true)
        {
            if (id != game.Id)
            {
                return NotFound();
            }

            game.Season = _context.Seasons.FirstOrDefault(s => s.Id == game.SeasonId)!;
            game.PlayoffRound = _context.PlayoffRounds.FirstOrDefault(r => r.Id == game.PlayoffRoundId) ?? null;
            game.AwayTeam = _context.Teams.FirstOrDefault(t => t.Id == game.AwayTeamId)!;
            game.HomeTeam = _context.Teams.FirstOrDefault(t => t.Id == game.HomeTeamId)!;

            game.IsFinalized = finalized;
            game.IsLive = false;

            _context.Update(game);
            await _context.SaveChangesAsync();

            if (game.Type == REGULAR_SEASON && game.IsFinalized && game.Period >= 3)
                await UpdateStandings(game.Season.Year, game.AwayTeam, game.HomeTeam);

            if (game.Type == PLAYOFFS && game.IsFinalized)
                await UpdatePlayoffSeries(game.Season.Year, game);

            if (!finalized)
            {
                if (game.Type == PLAYOFFS)
                    return RedirectToAction(nameof(Playoffs), new
                    {
                        season = game.Season.Year,
                        round = game.PlayoffRound!.Index
                    });

                return RedirectToAction(nameof(Index), new { weekOf = game.Date.ToShortDateString() });
            }

            return RedirectToAction(nameof(GameCenter), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<Schedule> GetGame(Guid? id)
        {
            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Id == id);

            return game!;
        }

        public async Task UpdateGame(Schedule game)
        {
            _context.Update(game);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> NextPeriod(Guid id)
        {
            var game = await GetGame(id);

            if (game.Period < 3 || game.AwayScore == game.HomeScore)
            {
                game.Period++;
                await UpdateGame(game);
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> PreviousPeriod(Guid id)
        {
            var game = await GetGame(id);

            if (game.Period > 1)
            {
                game.Period--;
                await UpdateGame(game);
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> AwayGoal(Guid id)
        {
            var game = await GetGame(id);
            game.AwayScore++;

            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> RemoveAwayGoal(Guid id)
        {
            var game = await GetGame(id);

            if (game.AwayScore > 0)
            {
                game.AwayScore--;
                await UpdateGame(game);
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> HomeGoal(Guid id)
        {
            var game = await GetGame(id);
            game.HomeScore++;

            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> RemoveHomeGoal(Guid id)
        {
            var game = await GetGame(id);

            if (game.HomeScore > 0)
            {
                game.HomeScore--;
                await UpdateGame(game);
            }

            return RedirectToAction(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        public async Task<IActionResult> SaveGame(Guid id)
        {
            var game = await GetGame(id);

            game.IsLive = false;
            await UpdateGame(game);

            if (game.Type == PLAYOFFS)
                return RedirectToAction(nameof(Playoffs), new { season = game.Date.Year, round = game.PlayoffRound!.Index });

            return RedirectToAction(nameof(Index), new { weekOf = game.Date.ToShortDateString() });
        }

        public async Task<IActionResult> FinalizeGame(Guid id)
        {
            var game = await GetGame(id);

            if (game.Period >= 3 && game.AwayScore != game.HomeScore)
            {
                game.IsFinalized = true;
                await UpdateGame(game);

                if (game.Type == REGULAR_SEASON)
                    await UpdateStandings(game.Season.Year, game.AwayTeam, game.HomeTeam);

                if (game.Type == PLAYOFFS)
                    await UpdatePlayoffSeries(game.Season.Year, game);

                return RedirectToAction(nameof(GameCenter), new
                {
                    date = game.Date.ToShortDateString(),
                    awayTeam = game.AwayTeam.Code,
                    homeTeam = game.HomeTeam.Code
                });
            }

            return View(nameof(GameControls), new
            {
                date = game.Date.ToShortDateString(),
                awayTeam = game.AwayTeam.Code,
                homeTeam = game.HomeTeam.Code
            });
        }

        // GET: Schedule/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Schedule == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Schedule'  is null.");
            }

            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefault();

            if (schedule != null)
            {
                _context.Schedule.Remove(schedule);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { season = schedule.Season.Year });
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

            await StandingsUpdateNowAvailable();
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
