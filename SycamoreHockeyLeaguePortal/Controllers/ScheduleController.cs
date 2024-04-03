using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        Random random = new Random();

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedule
        public async Task<IActionResult> Index(DateTime weekOf, string? team)
        {
            ViewBag.Team = team;

            ViewBag.Date = weekOf;
            var endOfWeek = weekOf.AddDays(6);
            ViewBag.EndOfWeek = endOfWeek;

            var seasons = await _context.Season
                .OrderByDescending(s => s.Year)
                .ToListAsync();
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var season = weekOf.Year;

            var teams = await _context.Alignment
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

            ViewBag.Dates = await schedule
                .Select(s => s.Date.Date)
                .Distinct()
                .ToListAsync();

            return View(await schedule.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Playoffs(int season, int round, string? team)
        {
            ViewBag.Season = season;
            ViewBag.Team = team;
            ViewBag.Round = round;

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var rounds = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season)
                .OrderBy(r => r.Index);
            ViewBag.Rounds = new SelectList(rounds, "Index", "Name");

            var teams = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.PlayoffRound.Index == round)
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
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == PLAYOFFS &&
                                s.PlayoffRound.Index == round &&
                                (s.AwayTeam.Code == team ||
                                 s.HomeTeam.Code == team))
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }
            else
            {
                playoffs = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == PLAYOFFS &&
                                s.PlayoffRound.Index == round)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }

            return View(await playoffs.AsNoTracking().ToListAsync());
        }

        // GET: Schedule/Details/5
        public async Task<IActionResult> GameCenter(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        // GET: Schedule/Create
        public IActionResult Create()
        {
            var teams = _context.Team
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            var seasons = _context.Season
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
            game.Season = _context.Season.FirstOrDefault(s => s.Id == game.SeasonId)!;
            game.PlayoffRound = _context.PlayoffRound.FirstOrDefault(r => r.Id == game.PlayoffRoundId) ?? null;
            game.AwayTeam = _context.Team.FirstOrDefault(t => t.Id == game.AwayTeamId)!;
            game.HomeTeam = _context.Team.FirstOrDefault(t => t.Id == game.HomeTeamId)!;

            _context.Add(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { season = game.Season.Year });
        }

        // GET: Schedule/Edit/5
        public async Task<IActionResult> GameControls(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync()!;

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

            /*ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", game.Season.Year);
            ViewData["PlayoffRoundId"] = new SelectList(_context.PlayoffRound, "Id", "Name", "Select Round");
            ViewData["AwayTeamId"] = new SelectList(_context.Team, "Id", "FullName", game.AwayTeam.FullName);
            ViewData["HomeTeamId"] = new SelectList(_context.Team, "Id", "FullName", game.HomeTeam.FullName);*/
            return View(game);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GameControls(Guid id, [Bind("Id,SeasonId,PlayoffRoundId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,IsFinalized,Notes,GameIndex")] Schedule game, bool finalized = true)
        {
            if (id != game.Id)
            {
                return NotFound();
            }

            game.Season = _context.Season.FirstOrDefault(s => s.Id == game.SeasonId)!;
            game.PlayoffRound = _context.PlayoffRound.FirstOrDefault(r => r.Id == game.PlayoffRoundId) ?? null;
            game.AwayTeam = _context.Team.FirstOrDefault(t => t.Id == game.AwayTeamId)!;
            game.HomeTeam = _context.Team.FirstOrDefault(t => t.Id == game.HomeTeamId)!;
            
            game.IsFinalized = finalized;
            game.IsLive = false;

            _context.Update(game);
            await _context.SaveChangesAsync();

            if (game.Type == REGULAR_SEASON && game.IsFinalized && game.Period >= 3)
                await UpdateStandings(game.Season.Year, game.AwayTeam, game.HomeTeam);

            if (game.Type == PLAYOFFS && game.IsFinalized)
            {
                var playoffSeries = _context.PlayoffSeries
                    .Include(s => s.Season)
                    .Include(s => s.Round)
                    .Include(s => s.Team1)
                    .Include(s => s.Team2)
                    .Where(s => s.Season.Year == game.Season.Year &&
                                (s.Team1Id == game.HomeTeamId && s.Team2Id == game.AwayTeamId) ||
                                (s.Team1Id == game.AwayTeamId && s.Team2Id == game.HomeTeamId))
                    .FirstOrDefault();

                var seriesSchedule = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.PlayoffRound)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == game.Season.Year &&
                                s.Type == PLAYOFFS &&
                                ((s.AwayTeamId == game.AwayTeamId && s.HomeTeamId == game.HomeTeamId) ||
                                 (s.AwayTeamId == game.HomeTeamId && s.HomeTeamId == game.AwayTeamId)));

                var gamesPlayed = seriesSchedule.Where(g => g.IsFinalized);

                playoffSeries.Team1Wins = gamesPlayed
                    .Where(g => (g.AwayTeamId == playoffSeries.Team1Id && g.AwayScore > g.HomeScore) ||
                                (g.HomeTeamId == playoffSeries.Team1Id && g.HomeScore > g.AwayScore))
                    .Count();
                playoffSeries.Team2Wins = gamesPlayed
                    .Where(g => (g.AwayTeamId == playoffSeries.Team2Id && g.AwayScore > g.HomeScore) ||
                                (g.HomeTeamId == playoffSeries.Team2Id && g.HomeScore > g.AwayScore))
                    .Count();

                _context.PlayoffSeries.Update(playoffSeries);

                game.Notes = playoffSeries.SeriesScoreString;
                _context.Schedule.Update(game);

                if (playoffSeries.Team1Wins == 4 || playoffSeries.Team2Wins == 4)
                {
                    var remainingGames = seriesSchedule.Where(g => g.Date.Date.CompareTo(game.Date.Date) > 0);

                    if (remainingGames.Any())
                    {
                        foreach (var remainingGame in remainingGames)
                            _context.Schedule.Remove(remainingGame);
                    }
                }

                await _context.SaveChangesAsync();
            }

            if (!finalized)
            {
                if (game.Type == PLAYOFFS)
                    return RedirectToAction(nameof(Playoffs), new { season = game.Season.Year, round = game.PlayoffRound!.Index });

                return RedirectToAction(nameof(Index), new { weekOf = game.Date.ToShortDateString() });
            }

            return RedirectToAction(nameof(GameCenter), new { id = id });
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
            game.Period++;
            
            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new { id = id });
        }

        public async Task<IActionResult> PreviousPeriod(Guid id)
        {
            var game = await GetGame(id);
            game.Period--;

            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new { id = id });
        }

        public async Task<IActionResult> AwayGoal(Guid id)
        {
            var game = await GetGame(id);
            game.AwayScore++;

            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new { id = id });
        }

        public async Task<IActionResult> RemoveAwayGoal(Guid id)
        {
            var game = await GetGame(id);
            
            if (game.AwayScore > 0)
            {
                game.AwayScore--;
                await UpdateGame(game);
            }
            
            return RedirectToAction(nameof(GameControls), new { id = id });
        }

        public async Task<IActionResult> HomeGoal(Guid id)
        {
            var game = await GetGame(id);
            game.HomeScore++;

            await UpdateGame(game);
            return RedirectToAction(nameof(GameControls), new { id = id });
        }

        public async Task<IActionResult> RemoveHomeGoal(Guid id)
        {
            var game = await GetGame(id);

            if (game.HomeScore > 0)
            {
                game.HomeScore--;
                await UpdateGame(game);
            }

            return RedirectToAction(nameof(GameControls), new { id = id });
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

            teamStats.WinsVsDivision = await GetWins(season, team, "division");
            teamStats.LossesVsDivision = await GetLosses(season, team, "division");
            teamStats.GamesPlayedVsDivision = teamStats.WinsVsDivision + teamStats.LossesVsDivision;
            teamStats.WinPctVsDivision = (teamStats.GamesPlayedVsDivision > 0) ?
                100 * ((decimal)teamStats.WinsVsDivision / teamStats.GamesPlayedVsDivision) :
                0;

            teamStats.WinsVsConference = await GetWins(season, team, "conference");
            teamStats.LossesVsConference = await GetLosses(season, team, "conference");
            teamStats.GamesPlayedVsConference = teamStats.WinsVsConference + teamStats.LossesVsConference;
            teamStats.WinPctVsConference = (teamStats.GamesPlayedVsConference > 0) ?
                100 * ((decimal)teamStats.WinsVsConference / teamStats.GamesPlayedVsConference) :
                0;

            teamStats.InterConfWins = await GetWins(season, team, "inter-conference");
            teamStats.InterConfLosses = await GetLosses(season, team, "inter-conference");
            teamStats.InterConfGamesPlayed = teamStats.InterConfWins + teamStats.InterConfLosses;
            teamStats.InterConfWinPct = (teamStats.InterConfGamesPlayed > 0) ?
                100 * ((decimal)teamStats.InterConfWins / teamStats.InterConfGamesPlayed) :
                0;

            int[] recordInLast5Games = await GetRecordInLast5Games(season, team);
            teamStats.WinsInLast5Games = recordInLast5Games[0];
            teamStats.LossesInLast5Games = recordInLast5Games[1];
            teamStats.GamesPlayedInLast5Games = teamStats.WinsInLast5Games + teamStats.LossesInLast5Games;
            teamStats.WinPctInLast5Games = teamStats.GamesPlayedInLast5Games > 0 ?
                100 * ((decimal)teamStats.WinsInLast5Games / teamStats.GamesPlayedInLast5Games) :
                0;

            _context.Standings.Update(teamStats);
            await _context.SaveChangesAsync();

            await UpdateGamesBehind(season);
        }

        private async Task<int[]> GetRecordInLast5Games(int season, Team team)
        {
            var gamesPlayed = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            (s.AwayTeam == team || s.HomeTeam == team) &&
                            s.IsFinalized && s.Period >= 3)
                .ToListAsync();

            int gamesPlayedCount = gamesPlayed.Count;
            int[] record = { 0, 0 };
            const int WINS = 0, LOSSES = 1;
            for (int index = gamesPlayedCount - 1; index >= 0 && index >= gamesPlayedCount - 5; index--)
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

        private IQueryable<Alignment> GetTeams(int season)
        {
            return _context.Alignment
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
                            s.Period >= 3);
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
            var teams = _context.Alignment
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
                        case "division":
                            if (opponentsDivision == division)
                                wins++;

                            break;

                        case "conference":
                            if (opponentsConference == conference && opponentsDivision != division)
                                wins++;

                            break;

                        case "inter-conference":
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

        private async Task<int> GetLosses(int season, Team team, string vsGroup="overall")
        {
            var teams = _context.Alignment
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
                        case "division":
                            if (opponentsDivision == division)
                                losses++;

                            break;

                        case "conference":
                            if (opponentsConference == conference && opponentsDivision != division)
                                losses++;

                            break;

                        case "inter-conference":
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
                        streak = streak > 0 ? streak++ : 1;
                    else
                        streak = streak < 0 ? streak-- : -1;
                }
                else
                {
                    if (game.AwayScore > game.HomeScore)
                        streak = streak > 0 ? streak++ : 1;
                    else
                        streak = streak < 0 ? streak-- : -1;
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
            await UpdateGamesBehind(season, "division");
            await UpdateGamesBehind(season, "conference");
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
                case "division":
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

                case "conference":
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
            var flag = _context.ProgramFlag
                .Where(f => f.Description == "New Standings Update Available")
                .FirstOrDefault();

            flag!.State = true;

            _context.ProgramFlag.Update(flag);
            await _context.SaveChangesAsync();
        }
    }
}
