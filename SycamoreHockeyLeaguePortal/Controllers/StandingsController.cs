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
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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

            var standings = await GetStandings(season);

            return View(standings);
        }

        public async Task<IActionResult> PlayoffMatchups(int season)
        {
            ViewBag.Season = season;

            List<List<Standings>> playoffTeams = new List<List<Standings>>();

            var standings = await GetStandings(season);

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
                .FirstOrDefault()!;
            ViewData["Team1"] = team1;

            var team2 = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.Team.Code == team2Code)
                .FirstOrDefault()!;
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

        private async Task<List<Standings>> GetStandings(int season)
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

            List<Standings> standingsList = standings.ToList();

            if (season >= 2024)
                standingsList = await ApplyTiebreakers(standingsList);

            return standingsList;
        }

        private async Task<List<Standings>> ApplyTiebreakers(List<Standings> standings)
        {
            for (int index = 0; index < standings.Count() - 1; index++)
            {
                Standings currentTeam = standings[index];
                Standings nextTeam = standings[index + 1];

                if (currentTeam.Wins == nextTeam.Wins &&
                    currentTeam.Losses == nextTeam.Losses)
                    standings = ApplyH2HTiebreaker(standings, index, currentTeam, nextTeam);
            }

            await StandingsUpdated();
            return standings;
        }

        private List<Standings> ApplyH2HTiebreaker(List<Standings> standings, int index,
                                                   Standings team1, Standings team2)
        {
            int season = standings.First()!.Season.Year;
            int[] series = GetH2HSeries(season, team1, team2);
            const int TEAM1 = 0, TEAM2 = 1;

            if (series[TEAM1] == series[TEAM2])
                return ApplyWinsByTypeTiebreaker(standings, index, team1, team2);
            else if (series[TEAM1] < series[TEAM2])
                return SwapTeams(standings, index, team1, team2);

            return standings;
        }

        private List<Standings> ApplyWinsByTypeTiebreaker(List<Standings> standings, int index,
                                                          Standings team1, Standings team2)
        {
            if (team1.RegulationWins == team2.RegulationWins)
            {
                if (team1.RegPlusOTWins == team2.RegPlusOTWins)
                    return ApplyGroupRecordTiebreakers(standings, index, team1, team2);
                else if (team1.RegPlusOTWins < team2.RegPlusOTWins)
                    return SwapTeams(standings, index, team1, team2);
            }
            else if (team1.RegulationWins < team2.RegulationWins)
                return SwapTeams(standings, index, team1, team2);

            return standings;
        }

        private List<Standings> ApplyGroupRecordTiebreakers(List<Standings> standings, int index,
                                                            Standings team1, Standings team2)
        {
            if (team1.Division == team2.Division)
                return BreakTieWithinDivision(standings, index, team1, team2);

            if (team1.Conference == team2.Conference && team1.Division != team2.Division)
                return BreakTieWithinConference(standings, index, team1, team2);

            return BreakTieInOverallStandings(standings, index, team1, team2);
        }

        private List<Standings> BreakTieWithinDivision(List<Standings> standings, int index,
                                                       Standings team1, Standings team2)
        {
            return ApplyDivisionRecordTiebreaker(standings, index, team1, team2, "division");
        }

        private List<Standings> ApplyDivisionRecordTiebreaker(List<Standings> standings, int index,
                                                              Standings team1, Standings team2,
                                                              string tiebreakingArea)
        {
            if (team1.WinsVsDivision == team2.WinsVsDivision &&
                team1.LossesVsDivision == team2.LossesVsDivision)
            {
                switch (tiebreakingArea)
                {
                    case "division":
                        return ApplyConferenceRecordTiebreaker(standings, index, team1, team2, tiebreakingArea);
                    
                    case "conference":
                        return ApplyInterConferenceRecordTiebreaker(standings, index, team1, team2, tiebreakingArea);

                    default:
                        return standings;
                }
            }
            else if (team1.WinPctVsDivision < team2.WinPctVsDivision)
                return SwapTeams(standings, index, team1, team2);

            return standings;
        }

        private List<Standings> BreakTieWithinConference(List<Standings> standings, int index,
                                                         Standings team1, Standings team2)
        {
            return ApplyConferenceRecordTiebreaker(standings, index, team1, team2, "conference");
        }

        private List<Standings> ApplyConferenceRecordTiebreaker(List<Standings> standings, int index,
                                                                Standings team1, Standings team2,
                                                                string tiebreakingArea)
        {
            if (team1.WinsVsConference == team2.WinsVsConference &&
                team1.LossesVsConference == team2.LossesVsConference)
            {
                switch (tiebreakingArea)
                {
                    case "division":
                        return ApplyInterConferenceRecordTiebreaker(standings, index, team1, team2, tiebreakingArea);

                    default:
                        return ApplyDivisionRecordTiebreaker(standings, index, team1, team2, tiebreakingArea);
                }
            }
            else if (team1.WinPctVsConference < team2.WinPctVsConference)
                return SwapTeams(standings, index, team1, team2);

            return standings;
        }

        private List<Standings> BreakTieInOverallStandings(List<Standings> standings, int index,
                                                           Standings team1, Standings team2)
        {
            return ApplyInterConferenceRecordTiebreaker(standings, index, team1, team2, "league");
        }

        private List<Standings> ApplyInterConferenceRecordTiebreaker(List<Standings> standings, int index,
                                                                     Standings team1, Standings team2,
                                                                     string tiebreakingArea)
        {
            if (team1.InterConfWins == team2.InterConfWins &&
                team1.InterConfLosses == team2.InterConfLosses)
            {
                switch (tiebreakingArea)
                {
                    case "league":
                        return ApplyConferenceRecordTiebreaker(standings, index, team1, team2, tiebreakingArea);

                    default:
                        return standings;
                }
            }
            else if (team1.InterConfWinPct < team2.InterConfWinPct)
                return SwapTeams(standings, index, team1, team2);

            return standings;
        }

        private List<Standings> SwapTeams(List<Standings> standings, int index,
                                          Standings team1, Standings team2)
        {
            standings[index] = team2;
            standings[index + 1] = team1;
            
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
            return h2hGames.Where(g => g.IsFinalized && g.Period >= 3);
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
