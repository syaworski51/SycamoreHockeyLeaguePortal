using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Identity.Client;
using NuGet.ProjectModel;
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

            _context.Update(game);
            await _context.SaveChangesAsync();

            if (game.Type == "Regular Season" && game.IsFinalized)
            {
                await UpdateStandings(game.Season.Year, game.AwayTeamId, game.HomeTeamId);
            }

            if (game.Type == "Playoffs" && game.IsFinalized)
            {
                var playoffSeries = _context.PlayoffSeries
                    .Include(s => s.Season)
                    .Include(s => s.Round)
                    .Include(s => s.Team1)
                    .Include(s => s.Team2)
                    .Where(s => s.Season.Year == game.Season.Year &&
                                (s.Team1Id == game.AwayTeamId || s.Team1Id == game.HomeTeamId) &&
                                (s.Team2Id == game.AwayTeamId || s.Team2Id == game.HomeTeamId))
                    .FirstOrDefault();

                if (game.HomeScore > game.AwayScore)
                {
                    if (game.HomeTeamId == playoffSeries.Team1Id)
                        playoffSeries.Team1Wins++;
                    else
                        playoffSeries.Team2Wins++;
                }
                else
                {
                    if (game.AwayTeamId == playoffSeries.Team1Id)
                        playoffSeries.Team1Wins++;
                    else
                        playoffSeries.Team2Wins++;
                }

                _context.PlayoffSeries.Update(playoffSeries);

                if (playoffSeries.Status == "Series over")
                {
                    var remainingGames = _context.Schedule
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.Type == "Playoffs" &&
                                    ((s.AwayTeamId == playoffSeries.Team1Id &&
                                      s.HomeTeamId == playoffSeries.Team2Id) ||
                                     (s.AwayTeamId == playoffSeries.Team2Id &&
                                      s.HomeTeamId == playoffSeries.Team1Id)) &&
                                    s.Date.Date.CompareTo(game.Date.Date) > 0);

                    if (remainingGames.Any())
                    {
                        foreach (var remainingGame in remainingGames)
                            _context.Schedule.Remove(remainingGame);
                    }
                }
            }

            if (game.Type == "Playoffs")
                return RedirectToAction(nameof(Playoffs), new { season = game.Season.Year, round = game.PlayoffRound.Index });

            return RedirectToAction(nameof(Index), new { season = game.Season.Year, date = game.Date.ToShortDateString() });
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
            await UpdateTeamStats(season, awayTeamId, homeTeamId);
            await UpdateTeamStats(season, homeTeamId, awayTeamId);

            await StandingsUpdateNowAvailable();
        }

        private async Task UpdateTeamStats(int season, Guid teamId, Guid opponentId)
        {
            var team = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.TeamId == teamId)
                .FirstOrDefault();

            var opponent = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.TeamId == opponentId)
                .FirstOrDefault();

            var teamGamesPlayed = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == "Regular Season" &&
                            s.IsLive == false &&
                            s.Period >= 3 &&
                            (s.AwayTeamId == team.TeamId ||
                             s.HomeTeamId == team.TeamId));

            int[] record = { 0, 0 };
            int[] goalRatio = { 0, 0 };
            int streak = 0;
            int rw = 0, row = 0;
            int[] divRecord = { 0, 0 };
            int[] confRecord = { 0, 0 };
            int[] interConfRecord = { 0, 0 };
            foreach (var game in teamGamesPlayed)
            {
                string teamDivision;
                string teamConference;
                string opponentsDivision;
                string opponentsConference;

                if (game.HomeTeamId == teamId)
                {
                    teamDivision = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.HomeTeamId)
                        .Select(s => s.Division!.Code)
                        .FirstOrDefault()!;

                    teamConference = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.HomeTeamId)
                        .Select(s => s.Conference!.Code)
                        .FirstOrDefault()!;

                    opponentsDivision = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.AwayTeamId)
                        .Select(s => s.Division!.Code)
                        .FirstOrDefault()!;

                    opponentsConference = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.AwayTeamId)
                        .Select(s => s.Conference!.Code)
                        .FirstOrDefault()!;


                    if (game.HomeScore > game.AwayScore)
                    {
                        record[0]++;

                        if (streak <= 0)
                            streak = 1;
                        else
                            streak++;

                        if (!game.Status.Contains("SO"))
                        {
                            if (!game.Status.Contains("OT"))
                                rw++;

                            row++;
                        }

                        if (teamDivision == opponentsDivision)
                            divRecord[0]++;
                        else if (teamConference == opponentsConference)
                            confRecord[0]++;
                        else
                            interConfRecord[0]++;
                    }
                    else
                    {
                        record[1]++;

                        if (streak >= 0)
                            streak = -1;
                        else
                            streak--;

                        if (teamDivision == opponentsDivision)
                            divRecord[1]++;
                        else if (teamConference == opponentsConference)
                            confRecord[1]++;
                        else
                            interConfRecord[1]++;
                    }

                    goalRatio[0] += (int)game.HomeScore!;
                    goalRatio[1] += (int)game.AwayScore!;
                }
                else
                {
                    teamDivision = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.AwayTeamId)
                        .Select(s => s.Division!.Code)
                        .FirstOrDefault()!;

                    teamConference = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.AwayTeamId)
                        .Select(s => s.Conference!.Code)
                        .FirstOrDefault()!;

                    opponentsDivision = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.HomeTeamId)
                        .Select(s => s.Division!.Code)
                        .FirstOrDefault()!;

                    opponentsConference = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == game.Season.Year &&
                                    s.TeamId == game.HomeTeamId)
                        .Select(s => s.Conference!.Code)
                        .FirstOrDefault()!;

                    if (game.AwayScore > game.HomeScore)
                    {
                        record[0]++;

                        if (streak <= 0)
                            streak = 1;
                        else
                            streak++;

                        if (!game.Status.Contains("SO"))
                        {
                            if (!game.Status.Contains("OT"))
                                rw++;

                            row++;
                        }

                        if (teamDivision == opponentsDivision)
                            divRecord[0]++;
                        else if (teamConference == opponentsConference)
                            confRecord[0]++;
                        else
                            interConfRecord[0]++;
                    }
                    else
                    {
                        record[1]++;

                        if (streak >= 0)
                            streak = -1;
                        else
                            streak--;

                        if (teamDivision == opponentsDivision)
                            divRecord[1]++;
                        else if (teamConference == opponentsConference)
                            confRecord[1]++;
                        else
                            interConfRecord[1]++;
                    }

                    goalRatio[0] += (int)game.AwayScore!;
                    goalRatio[1] += (int)game.HomeScore!;
                }
            }

            team.Wins = record[0];
            team.Losses = record[1];
            team.GamesPlayed = team.Wins + team.Losses;
            team.WinPct = (team.GamesPlayed > 0) ?
                100 * ((decimal)team.Wins / team.GamesPlayed) :
                0;

            team.GoalsFor = goalRatio[0];
            team.GoalsAgainst = goalRatio[1];
            team.GoalDifferential = team.GoalsFor - team.GoalsAgainst;

            team.Streak = streak;
            team.RegulationWins = rw;
            team.RegPlusOTWins = row;

            team.WinsVsDivision = divRecord[0];
            team.LossesVsDivision = divRecord[1];
            team.GamesPlayedVsDivision = team.WinsVsDivision + team.LossesVsDivision;
            team.WinPctVsDivision = (team.GamesPlayedVsDivision > 0) ?
                100 * ((decimal)team.WinsVsDivision / team.GamesPlayedVsDivision) :
                0;

            team.WinsVsConference = divRecord[0];
            team.LossesVsConference = divRecord[1];
            team.GamesPlayedVsConference = team.WinsVsConference + team.LossesVsConference;
            team.WinPctVsConference = (team.GamesPlayedVsConference > 0) ?
                100 * ((decimal)team.WinsVsConference / team.GamesPlayedVsConference) :
                0;

            team.InterConfWins = divRecord[0];
            team.InterConfLosses = divRecord[1];
            team.InterConfGamesPlayed = team.InterConfWins + team.InterConfLosses;
            team.InterConfWinPct = (team.InterConfGamesPlayed > 0) ?
                100 * ((decimal)team.InterConfWins / team.InterConfGamesPlayed) :
                0;

            _context.Standings.Update(team);
            await _context.SaveChangesAsync();

            await UpdateGamesBehind(season);
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
