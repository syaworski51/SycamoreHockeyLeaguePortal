using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
                .Where(s => s.LastYear >= season || s.LastYear == null)
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

            var standings = await GetStandings(season, viewBy);

            return View(await standings.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> PlayoffMatchups(int season)
        {
            ViewBag.Season = season;

            List<List<Standings>> playoffTeams = new List<List<Standings>>();

            var standings = await GetStandings(season, "conference");

            var conferences = standings
                .Select(s => s.Conference)
                .Distinct()
                .OrderBy(c => c.Name);
            ViewBag.Conferences = conferences.ToList();

            foreach (var conference in conferences)
            {
                playoffTeams.Add(new List<Standings>());
                
                var conferenceStandings = standings
                    .Where(s => s.ConferenceId == conference.Id)
                    .ToList();

                var divisions = conferenceStandings
                    .Select(c => c.Division)
                    .Distinct();

                List<Standings> leaders = new List<Standings>();
                List<Standings> followers = new List<Standings>();
                foreach (var division in divisions)
                {
                    var divisionStandings = conferenceStandings
                        .Where(c => c.DivisionId == division.Id);

                    var leader = divisionStandings.First();
                    leaders.Add(leader);
                }

                int playoffTeamsPerConference = (season > 2021) ? 8 : 4;
                int maxFollowerCount = playoffTeamsPerConference - leaders.Count;
                foreach (var team in conferenceStandings)
                {
                    if (followers.Count >= maxFollowerCount)
                        break;
                    
                    if (leaders.Contains(team))
                        continue;

                    followers.Add(team);
                }

                foreach (var team in leaders)
                    playoffTeams.Last().Add(team);

                foreach (var team in followers)
                    playoffTeams.Last().Add(team);
            }

            List<List<Standings[]>> matchups = new List<List<Standings[]>>();
            foreach (var conference in playoffTeams)
            {
                int teamCount = conference.Count;
                matchups.Add(new List<Standings[]>());

                for (int index = 0; index < teamCount / 2; index++)
                {
                    Standings[] matchup = new Standings[2];
                    matchup[0] = conference[index];
                    matchup[1] = conference[(teamCount - 1) - index];
                    
                    matchups.Last().Add(matchup);
                }
            }

            return View(matchups);
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
            standings.Season = _context.Season.FirstOrDefault(s => s.Id == standings.SeasonId);
            standings.Conference = _context.Conference.FirstOrDefault(s => s.Id == standings.ConferenceId);
            standings.Division = _context.Division.FirstOrDefault(s => s.Id == standings.DivisionId);
            standings.Team = _context.Team.FirstOrDefault(s => s.Id == standings.TeamId);
            
            _context.Update(standings);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index), new { season = 2022, viewBy = "division" });
        }

        public async Task<IActionResult> PlayoffStatus(Guid id, string currentStatus, string viewBy)
        {
            var statLine = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Id == id);

            var playoffStatuses = _context.PlayoffStatus
                .Where(s => s.ActiveTo == null)
                .OrderByDescending(s => s.Index);
            ViewData["PlayoffStatuses"] = new SelectList(playoffStatuses, "Symbol", "Description", currentStatus);

            ViewBag.ViewBy = viewBy;

            return View(statLine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlayoffStatus(Guid id, [Bind("Id,PlayoffStatus")] Standings s, string viewBy)
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

            return RedirectToAction(nameof(Index), new { season = statLine.Season.Year, viewBy = viewBy });
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

        public async Task<IActionResult> Tiebreakers()
        {
            return View();
        }

        public async Task<IActionResult> PlayoffFormat()
        {
            return View();
        }

        [Route("Standings/HeadToHead/{season}/{team1Code}/{team2Code}/")]
        public async Task<IActionResult> HeadToHeadComparison(int season, string? team1Code, string? team2Code)
        {
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewData["Seasons"] = new SelectList(seasons, "Year", "Year");
            
            var team1 = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.Team.Code == team1Code)
                .FirstOrDefault();
            ViewData["Team1"] = team1;

            var team2 = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.Team.Code == team2Code)
                .FirstOrDefault();
            ViewData["Team2"] = team2;

            var teams = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .Select(s => s.Team)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);
            ViewData["Teams"] = new SelectList(teams, "Code", "FullName");

            var comparisonOptions = teams
                .Where(t => t.Code != team1Code);
            ViewData["ComparisonOptions"] = new SelectList(comparisonOptions, "Code", "FullName");

            ViewBag.H2HSeries = GetH2HSeries(season, team1, team2);
            ViewBag.H2HGoalsFor = GetH2HGoalsFor(season, team1, team2);
            ViewBag.H2HGFInWins = GetH2HGFInWins(season, team1, team2);
            ViewBag.H2HWinPoints = GetH2HWinPoints(season, team1, team2);
            ViewBag.H2HGFPerGame = GetH2HGFPerGame(team1, team2);
            ViewBag.H2HGAPerGame = GetH2HGAPerGame(team1, team2);

            return View();
        }

        private async Task<IQueryable<Standings>> GetStandings(int season, string? viewBy)
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
                    .ThenByDescending(s => s.GoalDifferential)
                    .ThenByDescending(s => s.GoalsFor)
                    .ThenByDescending(s => s.Streak)
                    .ThenByDescending(s => s.StreakGoalDifferential)
                    .ThenByDescending(s => s.StreakGoalsFor)
                    .ThenBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            }

            bool standingsUpdateAvailable = await GetStandingsUpdateStatus();
            if (standingsUpdateAvailable)
                standings = (IQueryable<Standings>)await ApplyH2HTiebreakers(standings.ToList());

            return standings;
        }

        private async Task<List<Standings>> ApplyH2HTiebreakers(List<Standings> standings)
        {
            for (int index = 0; index < standings.Count - 1; index++)
            {
                var currentTeam = standings[index];
                var nextTeam = standings[index + 1];

                if (currentTeam.Wins == nextTeam.Wins && 
                    currentTeam.Losses == nextTeam.Losses)
                {
                    var h2hGamesPlayed = _context.Schedule
                        .Include(s => s.Season)
                        .Include(s => s.PlayoffRound)
                        .Include(s => s.AwayTeam)
                        .Include(s => s.HomeTeam)
                        .Where(s => s.Season.Year == currentTeam.Season.Year &&
                                    s.Type == "Regular Season" &&
                                    !s.IsLive &&
                                    s.Period >= 3 &&
                                    ((s.AwayTeamId == currentTeam.TeamId &&
                                      s.HomeTeamId == nextTeam.TeamId) ||
                                     (s.AwayTeamId == nextTeam.TeamId &&
                                      s.HomeTeamId == currentTeam.TeamId)));

                    if (h2hGamesPlayed.Any())
                    {
                        int[] h2hWins = { 0, 0 };
                        int[] h2hGoalsFor = { 0, 0 };
                        int[] h2hGFInWins = { 0, 0 };
                        int[] h2hWinPoints = { 0, 0 };
                        const int CURRENT_TEAM = 0, NEXT_TEAM = 1;
                        
                        foreach (var game in h2hGamesPlayed)
                        {
                            if (game.HomeTeamId == currentTeam.TeamId)
                            {
                                h2hGoalsFor[CURRENT_TEAM] += (int)game.HomeScore!;
                                h2hGoalsFor[NEXT_TEAM] += (int)game.AwayScore!;

                                if (game.HomeScore > game.AwayScore)
                                {
                                    h2hWins[CURRENT_TEAM]++;
                                    h2hGFInWins[CURRENT_TEAM] += (int)game.HomeScore!;
                                    h2hWinPoints[CURRENT_TEAM] += 5 - game.Period;
                                }
                                else
                                {
                                    h2hWins[NEXT_TEAM]++;
                                    h2hGFInWins[NEXT_TEAM] += (int)game.AwayScore!;
                                    h2hWinPoints[NEXT_TEAM] += 5 - game.Period;
                                }
                            }
                            else
                            {
                                h2hGoalsFor[CURRENT_TEAM] += (int)game.AwayScore!;
                                h2hGoalsFor[NEXT_TEAM] += (int)game.HomeScore!;

                                if (game.AwayScore > game.HomeScore)
                                {
                                    h2hWins[CURRENT_TEAM]++;
                                    h2hGFInWins[CURRENT_TEAM] += (int)game.AwayScore!;
                                    h2hWinPoints[CURRENT_TEAM] += 5 - game.Period;
                                }
                                else
                                {
                                    h2hWins[NEXT_TEAM]++;
                                    h2hGFInWins[NEXT_TEAM] += (int)game.HomeScore!;
                                    h2hWinPoints[NEXT_TEAM] += 5 - game.Period;
                                }
                            }
                        }

                        if (h2hWins[CURRENT_TEAM] == h2hWins[NEXT_TEAM])
                        {
                            if (h2hGoalsFor[CURRENT_TEAM] == h2hGoalsFor[NEXT_TEAM])
                            {
                                if (h2hGFInWins[CURRENT_TEAM] == h2hGFInWins[NEXT_TEAM])
                                {
                                    if (h2hWinPoints[CURRENT_TEAM] == h2hWinPoints[NEXT_TEAM])
                                        standings = ApplyGroupRecordTiebreakers(standings, currentTeam, index, nextTeam, index + 1);
                                    else if (h2hWinPoints[CURRENT_TEAM] < h2hWinPoints[NEXT_TEAM])
                                        standings = SwapTeams(standings, currentTeam, index, nextTeam, index + 1);
                                }   
                                else if (h2hGFInWins[CURRENT_TEAM] < h2hGFInWins[NEXT_TEAM])
                                    standings = SwapTeams(standings, currentTeam, index, nextTeam, index + 1);
                            }
                            else if (h2hGoalsFor[CURRENT_TEAM] < h2hGoalsFor[NEXT_TEAM])
                                standings = SwapTeams(standings, currentTeam, index, nextTeam, index + 1);
                        }
                        else if (h2hWins[CURRENT_TEAM] < h2hWins[NEXT_TEAM])
                            standings = SwapTeams(standings, currentTeam, index, nextTeam, index + 1);
                    }
                }
            }

            await StandingsUpdated();
            return standings;
        }

        private List<Standings> ApplyGroupRecordTiebreakers(List<Standings> standings, 
                                                            Standings team1, int index1, 
                                                            Standings team2, int index2)
        {
            if (team1.DivisionId == team2.DivisionId)
                return BreakTieWithinDivision(standings, team1, index1, team2, index2);
            
            else if (team1.ConferenceId == team2.ConferenceId)
                return BreakTieWithinConference(standings, team1, index1, team2, index2);
            
            else
                return BreakTieInOverallStandings(standings, team1, index1, team2, index2);
        }

        private List<Standings> BreakTieWithinDivision(List<Standings> standings, 
                                            Standings team1, int index1, 
                                            Standings team2, int index2)
        {
            if (team1.WinsVsDivision == team2.WinsVsDivision &&
                team1.LossesVsDivision == team2.LossesVsDivision)
            {
                if (team1.WinsVsConference == team2.WinsVsConference &&
                    team1.LossesVsConference == team2.LossesVsConference)
                {
                    if (team1.InterConfWinPct < team2.InterConfWinPct)
                        standings = SwapTeams(standings, team1, index1, team2, index2);
                }
                else if (team1.WinPctVsConference < team2.WinPctVsConference)
                    standings = SwapTeams(standings, team1, index1, team2, index2);
            }
            else if (team1.WinPctVsDivision < team2.WinPctVsDivision)
                standings = SwapTeams(standings, team1, index1, team2, index2);

            return standings;
        }

        private List<Standings> BreakTieWithinConference(List<Standings> standings, 
                                              Standings team1, int index1, 
                                              Standings team2, int index2)
        {
            if (team1.WinsVsConference == team2.WinsVsConference &&
                team1.LossesVsConference == team2.LossesVsConference)
            {
                if (team1.WinsVsDivision == team2.WinsVsDivision &&
                    team1.LossesVsDivision == team2.LossesVsDivision)
                {
                    if (team1.InterConfWinPct < team2.InterConfWinPct)
                        standings = SwapTeams(standings, team1, index1, team2, index2);
                }
                else if (team1.WinPctVsDivision < team2.WinPctVsDivision)
                    standings = SwapTeams(standings, team1, index1, team2, index2);
            }
            else if (team1.WinPctVsConference < team2.WinPctVsConference)
                standings = SwapTeams(standings, team1, index1, team2, index2);

            return standings;
        }

        private List<Standings> BreakTieInOverallStandings(List<Standings> standings, 
                                                Standings team1, int index1, 
                                                Standings team2, int index2)
        {
            if (team1.InterConfWins == team2.InterConfWins &&
                team1.InterConfLosses == team2.InterConfLosses)
            {
                if (team1.WinsVsConference == team2.WinsVsConference &&
                    team2.LossesVsConference == team2.LossesVsConference)
                {
                    if (team1.WinPctVsDivision < team2.WinPctVsDivision)
                        standings = SwapTeams(standings, team1, index1, team2, index2);
                }
                else if (team1.WinPctVsConference < team2.WinPctVsConference)
                    standings = SwapTeams(standings, team1, index1, team2, index2);
            }
            else if (team1.InterConfWinPct < team2.InterConfWinPct)
                standings = SwapTeams(standings, team1, index1, team2, index2);

            return standings;
        }

        private List<Standings> SwapTeams(List<Standings> standings,
                                          Standings team1, int index1,
                                          Standings team2, int index2)
        {
            standings[index1] = team2;
            standings[index2] = team1;
            return standings;
        }

        private async Task<bool> GetStandingsUpdateStatus()
        {
            var updateAvailable = await _context.ProgramFlag
                .Where(f => f.Description == "New Standings Update Available")
                .Select(f => f.State)
                .FirstOrDefaultAsync();

            return updateAvailable;
        }

        private async Task StandingsUpdated()
        {
            var flag = await _context.ProgramFlag
                .Where(f => f.Description == "New Standings Update Available")
                .FirstOrDefaultAsync();

            flag!.State = false;

            _context.ProgramFlag.Update(flag);
            await _context.SaveChangesAsync();
        }

        private IQueryable<Schedule> GetH2HGames(int season, Standings team1, Standings team2)
        {
            var h2hGames = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == "Regular Season" &&
                            ((s.AwayTeamId == team1.TeamId && s.HomeTeamId == team2.TeamId) ||
                             (s.AwayTeamId == team2.TeamId && s.HomeTeamId == team1.TeamId)));

            return h2hGames;
        }

        private IQueryable<Schedule> GetH2HGamesPlayed(int season, Standings team1, Standings team2)
        {
            var h2hGames = GetH2HGames(season, team1, team2);
            return h2hGames.Where(g => !g.IsLive && g.Period >= 3);
        }

        private int[] GetH2HSeries(int season, Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            var h2hGamesPlayed = GetH2HGamesPlayed(season, team1, team2);

            int[] h2hSeries = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                int winningIndex;
                
                if (game.HomeTeamId == team1.TeamId)
                    winningIndex = (game.HomeScore > game.AwayScore) ? TEAM1 : TEAM2;
                else
                    winningIndex = (game.AwayScore > game.HomeScore) ? TEAM1 : TEAM2;

                h2hSeries[winningIndex]++;
            }

            return h2hSeries;
        }

        private int[] GetH2HGoalsFor(int season, Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            var h2hGamesPlayed = GetH2HGamesPlayed(season, team1, team2);

            int[] h2hGoalsFor = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                if (game.HomeTeamId == team1.TeamId)
                {
                    h2hGoalsFor[TEAM1] += (int)game.HomeScore!;
                    h2hGoalsFor[TEAM2] += (int)game.AwayScore!;
                }
                else
                {
                    h2hGoalsFor[TEAM1] += (int)game.AwayScore!;
                    h2hGoalsFor[TEAM2] += (int)game.HomeScore!;
                }
            }

            return h2hGoalsFor;
        }

        private int[] GetH2HGFInWins(int season, Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            var h2hGamesPlayed = GetH2HGamesPlayed(season, team1, team2);

            int[] h2hGFInWins = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                int winningIndex;
                int winningScore;

                if (game.HomeTeamId == team1.TeamId)
                {
                    bool homeTeamWon = (game.HomeScore > game.AwayScore);
                    winningIndex = homeTeamWon ? TEAM1 : TEAM2;
                    winningScore = homeTeamWon ? (int)game.HomeScore! : (int)game.AwayScore!;
                }
                else
                {
                    bool awayTeamWon = (game.AwayScore > game.HomeScore);
                    winningIndex = awayTeamWon ? TEAM1 : TEAM2;
                    winningScore = awayTeamWon ? (int)game.AwayScore! : (int)game.HomeScore!;
                }

                h2hGFInWins[winningIndex] += winningScore;
            }

            return h2hGFInWins;
        }

        private int[] GetH2HWinPoints(int season, Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            var h2hGamesPlayed = GetH2HGamesPlayed(season, team1, team2);

            int[] h2hWinPoints = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                int winningIndex;

                if (game.HomeTeamId == team1.TeamId)
                    winningIndex = (game.HomeScore > game.AwayScore) ? TEAM1 : TEAM2;
                else
                    winningIndex = (game.AwayScore > game.HomeScore) ? TEAM1 : TEAM2;

                int pointValue = 5 - game.Period;
                h2hWinPoints[winningIndex] += pointValue;
            }

            return h2hWinPoints;
        }

        private double[] GetH2HGFPerGame(Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            double[] h2hGFPerGame = { 0.00, 0.00 };
            h2hGFPerGame[TEAM1] = (team1.GamesPlayed > 0) ?
                (double)team1.GoalsFor / team1.GamesPlayed :
                0;
            h2hGFPerGame[TEAM2] = (team2.GamesPlayed > 0) ?
                (double)team2.GoalsFor / team2.GamesPlayed :
                0;

            return h2hGFPerGame;
        }

        private double[] GetH2HGAPerGame(Standings team1, Standings team2)
        {
            const int TEAM1 = 0;
            const int TEAM2 = 1;

            double[] h2hGAPerGame = { 0.00, 0.00 };
            h2hGAPerGame[TEAM1] = (team1.GamesPlayed > 0) ?
                (double)team1.GoalsAgainst / team1.GamesPlayed :
                0;
            h2hGAPerGame[TEAM2] = (team2.GamesPlayed > 0) ?
                (double)team2.GoalsAgainst / team2.GamesPlayed :
                0;

            return h2hGAPerGame;
        }
    }
}
