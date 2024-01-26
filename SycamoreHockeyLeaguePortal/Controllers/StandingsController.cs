using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class StandingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StandingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Standings
        public async Task<IActionResult> Index(int season, string viewBy)
        {
            ViewBag.Season = season;
            ViewBag.ViewBy = viewBy;

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var sortOptions = _context.StandingsSortOption
                .OrderBy(s => s.Index);
            ViewBag.SortOptions = new SelectList(sortOptions, "Parameter", "Name");

            var conferences = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Conference.Name)
                .Select(a => a.ConferenceId)
                .Distinct();
            ViewBag.Conferences = conferences;

            var divisions = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Division.Name)
                .Select(a => a.DivisionId)
                .Distinct();
            ViewBag.Divisions = divisions;

            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .Distinct()
                .OrderBy(a => a.Team.City)
                .OrderBy(a => a.Team.Name);

            await UpdateStandings(season);

            IOrderedQueryable<Standings> standings;
            if (season <= 2022)
            {
                standings = _context.Standings
                    .Include(s => s.Season)
                    .Include(s => s.Team)
                    .Where(s => s.Season.Year == season)
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
                standings = _context.Standings
                    .Include(s => s.Season)
                    .Include(s => s.Team)
                    .Where(s => s.Season.Year == season)
                    .OrderByDescending(s => s.Points)
                    .ThenBy(s => s.GamesPlayed)
                    .ThenByDescending(s => s.RegulationWins)
                    .ThenByDescending(s => s.RegPlusOTWins)
                    .ThenByDescending(s => s.Wins)
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
                if (viewBy == "division")
                {
                    standings = _context.Standings
                        .Include(s => s.Season)
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
                        .ThenBy(s => s.Team.City)
                        .ThenBy(s => s.Team.Name);
                }
                else if (viewBy == "conference")
                {
                    standings = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == season)
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
                }
                else
                {
                    standings = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Team)
                        .Where(s => s.Season.Year == season)
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
                }
            }

            return View(await standings.ToListAsync());
        }

        // GET: Standings/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Standings == null)
            {
                return NotFound();
            }

            var standings = await _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (standings == null)
            {
                return NotFound();
            }

            return View(standings);
        }

        // GET: Standings/Create
        public IActionResult Create()
        {
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id");
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id");
            return View();
        }

        // POST: Standings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,ConferenceId,DivisionId,TeamId,PlayoffStatus,Wins,Losses,OTLosses,GamesBehind,GoalsFor,GoalsAgainst,Streak,WinsVsDivision,LossesVsDivision,OTLossesVsDivision,WinsVsConference,LossesVsConference,OTLossesVsConference,InterConfWins,InterConfLosses,InterConfOTLosses")] Standings standings)
        {
            if (ModelState.IsValid)
            {
                standings.Id = Guid.NewGuid();
                _context.Add(standings);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id", standings.SeasonId);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id", standings.TeamId);
            return View(standings);
        }

        // GET: Standings/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Standings == null)
            {
                return NotFound();
            }

            var standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Id == id);
            
            if (standings == null)
            {
                return NotFound();
            }
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", standings.Season.Year);
            ViewData["ConferenceId"] = new SelectList(_context.Conference, "Id", "Name", standings.Conference.Name);
            ViewData["DivisionId"] = new SelectList(_context.Division, "Id", "Name", standings.Division.Name);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "FullName", standings.Team.FullName);
            return View(standings);
        }

        // POST: Standings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,ConferenceId,DivisionId,TeamId,PlayoffStatus,Wins,Losses,OTLosses,GamesBehind,GoalsFor,GoalsAgainst,Streak,WinsVsDivision,LossesVsDivision,OTLossesVsDivision,WinsVsConference,LossesVsConference,OTLossesVsConference,InterConfWins,InterConfLosses,InterConfOTLosses")] Standings standings)
        {
            /*if (id != standings.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {*/
            standings.Season = _context.Season.FirstOrDefault(s => s.Id == standings.SeasonId);
            standings.Conference = _context.Conference.FirstOrDefault(s => s.Id == standings.ConferenceId);
            standings.Division = _context.Division.FirstOrDefault(s => s.Id == standings.DivisionId);
            standings.Team = _context.Team.FirstOrDefault(s => s.Id == standings.TeamId);
            
            _context.Update(standings);
            await _context.SaveChangesAsync();
                /*}
                catch (DbUpdateConcurrencyException)
                {
                    if (!StandingsExists(standings.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }*/
            return RedirectToAction(nameof(Index), new { season = 2022, viewBy = "division" });
            //}
            /*ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", standings.Season.Year);
            ViewData["ConferenceId"] = new SelectList(_context.Conference, "Id", "Name", standings.Conference.Name);
            ViewData["DivisionId"] = new SelectList(_context.Division, "Id", "Name", standings.Division.Name);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "FullName", standings.Team.FullName);
            return View(standings);*/
        }

        public async Task<IActionResult> PlayoffStatus(Guid id)
        {
            var statLine = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Id == id);

            return View(statLine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlayoffStatus(Guid id, [Bind("Id,PlayoffStatus")] Standings s)
        {
            var statLine = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Id == id);

            statLine.PlayoffStatus = s.PlayoffStatus;
            
            _context.Update(statLine);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { season = statLine.Season.Year, viewBy = "division" });
        }

        // GET: Standings/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Standings == null)
            {
                return NotFound();
            }

            var standings = await _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (standings == null)
            {
                return NotFound();
            }

            return View(standings);
        }

        // POST: Standings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Standings == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Standings'  is null.");
            }
            var standings = await _context.Standings.FindAsync(id);
            if (standings != null)
            {
                _context.Standings.Remove(standings);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StandingsExists(Guid id)
        {
          return (_context.Standings?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task UpdateStandings(int season)
        {
            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Team.City)
                .ThenBy(a => a.Team.Name);

            foreach (var team in teams)
            {
                var games = _context.Schedule
                    .Where(s => s.Season.Year == season &&
                                s.Type == "Regular Season" &&
                                s.Period >= 3 &&
                                    (s.AwayTeamId == team.TeamId ||
                                     s.HomeTeamId == team.TeamId))
                    .OrderBy(s => s.Date);

                int recordSize = (season < 2024) ? 3 : 2;

                int[] record = new int[recordSize];
                int[] goalRatio = { 0, 0 };
                int streak = 0;
                int rw = 0, row = 0;
                int[] divRecord = new int[recordSize];
                int[] confRecord = new int[recordSize];
                int[] interConfRecord = new int[recordSize];
                
                foreach (var game in games)
                {
                    if (season < 2024)
                    {
                        if (game.HomeTeamId == team.TeamId)
                        {
                            var myDivision = (from t in teams
                                              where t.TeamId == game.HomeTeamId
                                              select t.Division.Code).FirstOrDefault();

                            var myConference = (from t in teams
                                                where t.TeamId == game.HomeTeamId
                                                select t.Conference.Code).FirstOrDefault();

                            var opponentsDivision = (from t in teams
                                                     where t.TeamId == game.AwayTeamId
                                                     select t.Division.Code).FirstOrDefault();

                            var opponentsConference = (from t in teams
                                                       where t.TeamId == game.AwayTeamId
                                                       select t.Conference.Code).FirstOrDefault();

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

                                if (myDivision == opponentsDivision)
                                    divRecord[0]++;
                                else if (myConference == opponentsConference)
                                    confRecord[0]++;
                                else
                                    interConfRecord[0]++;
                            }
                            else
                            {
                                int lossIndex = (game.Status.Contains("OT") || game.Status.Contains("SO")) ? 2 : 1;
                                record[lossIndex]++;

                                if (streak >= 0)
                                    streak = -1;
                                else
                                    streak--;

                                if (myDivision == opponentsDivision)
                                    divRecord[lossIndex]++;
                                else if (myConference == opponentsConference)
                                    confRecord[lossIndex]++;
                                else
                                    interConfRecord[lossIndex]++;
                            }
                            
                            goalRatio[0] += (int)game.HomeScore;
                            goalRatio[1] += (int)game.AwayScore;
                        }
                        else
                        {
                            var myDivision = (from t in teams
                                              where t.TeamId == game.AwayTeamId
                                              select t.Division.Code).FirstOrDefault();

                            var myConference = (from t in teams
                                                where t.TeamId == game.AwayTeamId
                                                select t.Conference.Code).FirstOrDefault();

                            var opponentsDivision = (from t in teams
                                                     where t.TeamId == game.HomeTeamId
                                                     select t.Division.Code).FirstOrDefault();

                            var opponentsConference = (from t in teams
                                                       where t.TeamId == game.HomeTeamId
                                                       select t.Conference.Code).FirstOrDefault();

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

                                if (myDivision == opponentsDivision)
                                    divRecord[0]++;
                                else if (myConference == opponentsConference)
                                    confRecord[0]++;
                                else
                                    interConfRecord[0]++;
                            }
                            else
                            {
                                int lossIndex = (game.Status.Contains("OT") || game.Status.Contains("SO")) ? 2 : 1;
                                record[lossIndex]++;

                                if (streak >= 0)
                                    streak = -1;
                                else
                                    streak--;

                                if (myDivision == opponentsDivision)
                                    divRecord[lossIndex]++;
                                else if (myConference == opponentsConference)
                                    confRecord[lossIndex]++;
                                else
                                    interConfRecord[lossIndex]++;
                            }

                            goalRatio[0] += (int)game.AwayScore;
                            goalRatio[1] += (int)game.HomeScore;
                        }
                    }
                    else
                    {
                        if (game.HomeTeamId == team.TeamId)
                        {
                            var myDivision = (from t in teams
                                              where t.TeamId == game.HomeTeamId
                                              select t.Division.Code).FirstOrDefault();

                            var myConference = (from t in teams
                                                where t.TeamId == game.HomeTeamId
                                                select t.Conference.Code).FirstOrDefault();

                            var opponentsDivision = (from t in teams
                                                     where t.TeamId == game.AwayTeamId
                                                     select t.Division.Code).FirstOrDefault();

                            var opponentsConference = (from t in teams
                                                       where t.TeamId == game.AwayTeamId
                                                       select t.Conference.Code).FirstOrDefault();

                            if (game.HomeScore > game.AwayScore)
                            {
                                record[0]++;

                                if (streak <= 0)
                                    streak = 1;
                                else
                                    streak++;

                                if (myDivision == opponentsDivision)
                                    divRecord[0]++;
                                else if (myConference == opponentsConference)
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

                                if (myDivision == opponentsDivision)
                                    divRecord[1]++;
                                else if (myConference == opponentsConference)
                                    confRecord[1]++;
                                else
                                    interConfRecord[1]++;
                            }

                            goalRatio[0] += (int)game.HomeScore;
                            goalRatio[1] += (int)game.AwayScore;
                        }
                        else
                        {
                            var myDivision = (from t in teams
                                              where t.TeamId == game.HomeTeamId
                                              select t.Division.Code).FirstOrDefault();

                            var myConference = (from t in teams
                                                where t.TeamId == game.HomeTeamId
                                                select t.Conference.Code).FirstOrDefault();

                            var opponentsDivision = (from t in teams
                                                     where t.TeamId == game.AwayTeamId
                                                     select t.Division.Code).FirstOrDefault();

                            var opponentsConference = (from t in teams
                                                       where t.TeamId == game.AwayTeamId
                                                       select t.Conference.Code).FirstOrDefault();

                            if (game.AwayScore > game.HomeScore)
                            {
                                record[0]++;

                                if (streak <= 0)
                                    streak = 1;
                                else
                                    streak++;

                                if (myDivision == opponentsDivision)
                                    divRecord[0]++;
                                else if (myConference == opponentsConference)
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

                                if (myDivision == opponentsDivision)
                                    divRecord[1]++;
                                else if (myConference == opponentsConference)
                                    confRecord[1]++;
                                else
                                    interConfRecord[1]++;
                            }

                            goalRatio[0] += (int)game.AwayScore;
                            goalRatio[1] += (int)game.HomeScore;
                        }
                    }
                }

                var teamStats = _context.Standings
                    .Include(s => s.Season)
                    .Include(s => s.Team)
                    .Where(s => s.Season.Year == season &&
                                s.TeamId == team.TeamId)
                    .FirstOrDefault();

                if (season < 2024)
                    teamStats.GamesPlayed += record[2];
                teamStats.Wins = record[0];
                teamStats.Losses = record[1];
                if (season < 2024)
                    teamStats.OTLosses = record[2];
                teamStats.GamesPlayed = teamStats.Wins + teamStats.Losses + teamStats.OTLosses;

                teamStats.GoalsFor = goalRatio[0];
                teamStats.GoalsAgainst = goalRatio[1];
                teamStats.GoalDifferential = teamStats.GoalsFor - teamStats.GoalsAgainst;

                teamStats.Points = (teamStats.Wins * 2) + teamStats.OTLosses;
                teamStats.MaximumPossiblePoints = 
                    (teamStats.Season.GamesPerTeam * 2) - (teamStats.Losses * 2) - teamStats.OTLosses;
                teamStats.WinPct = (teamStats.GamesPlayed > 0) ?
                    100 * ((decimal)teamStats.Wins / teamStats.GamesPlayed) :
                    0;
                teamStats.PointsPct = (teamStats.GamesPlayed > 0) ?
                    100 * ((decimal)teamStats.Points / (teamStats.GamesPlayed * 2)) :
                    0;
                    
                teamStats.Streak = streak;
                teamStats.RegulationWins = rw;
                teamStats.RegPlusOTWins = row;
                    
                teamStats.WinsVsDivision = divRecord[0];
                teamStats.LossesVsDivision = divRecord[1];
                if (season < 2024)
                    teamStats.OTLossesVsDivision = divRecord[2];
                teamStats.GamesPlayedVsDivision = teamStats.WinsVsDivision + teamStats.LossesVsDivision + teamStats.OTLossesVsDivision;
                teamStats.WinPctVsDivision = teamStats.GamesPlayedVsDivision > 0 ?
                    100 * ((decimal)teamStats.WinsVsDivision / teamStats.GamesPlayedVsDivision) :
                    0;
                    
                teamStats.WinsVsConference = confRecord[0];
                teamStats.LossesVsConference = confRecord[1];
                if (season < 2024)
                    teamStats.OTLossesVsConference = confRecord[2];
                teamStats.GamesPlayedVsConference = teamStats.WinsVsConference + teamStats.LossesVsConference + teamStats.OTLossesVsConference;
                teamStats.WinPctVsConference = teamStats.GamesPlayedVsConference > 0 ?
                    100 * ((decimal)teamStats.WinsVsConference / teamStats.GamesPlayedVsConference) :
                    0;

                teamStats.InterConfWins = interConfRecord[0];
                teamStats.InterConfLosses = interConfRecord[1];
                if (season < 2024)
                    teamStats.InterConfOTLosses = interConfRecord[2];
                teamStats.InterConfGamesPlayed = teamStats.InterConfWins + teamStats.InterConfLosses + teamStats.InterConfOTLosses;
                teamStats.InterConfWinPct = teamStats.InterConfGamesPlayed > 0 ?
                    100 * ((decimal)teamStats.InterConfWins / teamStats.InterConfGamesPlayed) :
                    0;

                _context.Standings.Update(teamStats);
            }
            
            await _context.SaveChangesAsync();
        }
    }
}
