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

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var season = weekOf.Year;

            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Team.City)
                .ThenBy(a => a.Team.Name)
                .Select(a => a.Team);
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

            ViewBag.Dates = schedule
                .Select(s => s.Date.Date)
                .Distinct()
                .ToList();

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
                                s.Type == "Playoffs" &&
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
                                s.Type == "Playoffs" &&
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

            var game = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Id == id)
                .FirstOrDefault();

            if (!game.IsFinalized)
            {
                game.IsLive = true;

                if (game.Period == 0)
                    game.Period = 1;

                _context.Schedule.Update(game);
                await _context.SaveChangesAsync();
            }

            if (game == null)
            {
                return NotFound();
            }

            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", game.Season.Year);
            ViewData["PlayoffRoundId"] = new SelectList(_context.PlayoffRound, "Id", "Name", "Select Round");
            ViewData["AwayTeamId"] = new SelectList(_context.Team, "Id", "FullName", game.AwayTeam.FullName);
            ViewData["HomeTeamId"] = new SelectList(_context.Team, "Id", "FullName", game.HomeTeam.FullName);
            return View(game);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GameControls(Guid id, [Bind("Id,SeasonId,PlayoffRoundId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,IsFinalized,Notes,GameIndex")] Schedule game, bool finalized)
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
            game.IsLive = !finalized;

            _context.Update(game);
            await _context.SaveChangesAsync();

            if (game.Type == "Regular Season" && game.IsFinalized && game.Period >= 3)
                await UpdateStandings(game.Season.Year, game.AwayTeamId, game.HomeTeamId);

            if (game.Type == "Playoffs" && game.IsFinalized)
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
                                s.Type == "Playoffs" &&
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

            if (!game.IsFinalized)
                return RedirectToAction(nameof(GameControls), new { id = game.Id });

            if (game.Type == "Playoffs")
                return RedirectToAction(nameof(Playoffs), new { season = game.Season.Year, round = game.PlayoffRound.Index });

            return RedirectToAction(nameof(Index), new { season = game.Season.Year, weekOf = game.Date.ToShortDateString() });
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

        private async Task UpdateStandings(int season, Guid awayTeamId, Guid homeTeamId)
        {
            await UpdateTeamStats(season, awayTeamId);
            await UpdateTeamStats(season, homeTeamId);

            await StandingsUpdateNowAvailable();
        }

        private async Task UpdateTeamStats(int season, Guid teamId)
        {
            var team = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.TeamId == teamId)
                .FirstOrDefault()!;

            team.Wins = GetWins(season, team.Team);
            team.Losses = GetLosses(season, team.Team);
            team.GamesPlayed = team.Wins + team.Losses;
            team.WinPct = (team.GamesPlayed > 0) ?
                100 * ((decimal)team.Wins / team.GamesPlayed) :
                0;

            int[] goalRatio = GetGoalRatio(season, team.Team);
            team.GoalsFor = goalRatio[0];
            team.GoalsAgainst = goalRatio[1];
            team.GoalDifferential = team.GoalsFor - team.GoalsAgainst;

            int[] winsByType = GetWinsByType(season, team.Team);
            team.Streak = GetStreak(season, team.Team);
            team.RegulationWins = winsByType[0];
            team.RegPlusOTWins = winsByType[1];

            team.WinsVsDivision = GetWins(season, team.Team, "division");
            team.LossesVsDivision = GetLosses(season, team.Team, "division");
            team.GamesPlayedVsDivision = team.WinsVsDivision + team.LossesVsDivision;
            team.WinPctVsDivision = (team.GamesPlayedVsDivision > 0) ?
                100 * ((decimal)team.WinsVsDivision / team.GamesPlayedVsDivision) :
                0;

            team.WinsVsConference = GetWins(season, team.Team, "conference");
            team.LossesVsConference = GetLosses(season, team.Team, "conference");
            team.GamesPlayedVsConference = team.WinsVsConference + team.LossesVsConference;
            team.WinPctVsConference = (team.GamesPlayedVsConference > 0) ?
                100 * ((decimal)team.WinsVsConference / team.GamesPlayedVsConference) :
                0;

            team.InterConfWins = GetWins(season, team.Team, "inter-conference");
            team.InterConfLosses = GetLosses(season, team.Team, "inter-conference");
            team.InterConfGamesPlayed = team.InterConfWins + team.InterConfLosses;
            team.InterConfWinPct = (team.InterConfGamesPlayed > 0) ?
                100 * ((decimal)team.InterConfWins / team.InterConfGamesPlayed) :
                0;

            _context.Standings.Update(team);
            await _context.SaveChangesAsync();

            await UpdateGamesBehind(season);
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
                            s.Type == "Regular Season" &&
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

        private int GetWins(int season, Team team, string vsGroup="overall")
        {
            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);
            
            var gamesPlayed = GetGamesPlayed(season, team);
            var gamesWon = GetGamesWon(season, team);

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

        private int[] GetWinsByType(int season, Team team)
        {
            var gamesWon = GetGamesWon(season, team);
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

        private int GetLosses(int season, Team team, string vsGroup="overall")
        {
            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season);

            var gamesPlayed = GetGamesPlayed(season, team);
            var gamesLost = GetGamesLost(season, team);

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

        private int GetStreak(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

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

        private int[] GetGoalRatio(int season, Team team)
        {
            var gamesPlayed = GetGamesPlayed(season, team);

            int[] goalRatio = { 0, 0 };
            const int GF = 0, GA = 1;

            if (gamesPlayed.Any())
            {
                foreach (var game in gamesPlayed)
                {
                    if (game.HomeTeam == team)
                    {
                        goalRatio[GF] += (int)game.HomeScore!;
                        goalRatio[GA] += (int)game.AwayScore!;
                    }
                    else
                    {
                        goalRatio[GF] += (int)game.AwayScore!;
                        goalRatio[GA] += (int)game.HomeScore!;
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
