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
using Microsoft.IdentityModel.Tokens;
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

            var leaders = GetLeaders(standings);
            ViewBag.Leaders = leaders;

            var nonLeaders = GetNonLeaders(standings, leaders);
            ViewBag.NonLeaders = nonLeaders;

            return View(standings);
        }

        public List<Standings> GetLeaders(List<Standings> standings)
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

        public List<Standings> GetNonLeaders(List<Standings> standings, List<Standings>? leaders = null)
        {
            leaders ??= GetLeaders(standings);
            List<Standings> nonLeaders = new List<Standings>();

            foreach (var team in standings)
            {
                if (!leaders.Contains(team))
                    nonLeaders.Add(team);
            }

            return nonLeaders;
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
                    .ThenByDescending(s => s.WinPctInLast5Games)
                    .ThenByDescending(s => s.WinsInLast5Games)
                    .ThenBy(s => s.LossesInLast5Games)
                    .ThenByDescending(s => s.Streak)
                    .ThenBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            }

            List<Standings> standingsList = standings.ToList();

            if (season >= 2024)
                standingsList = await ApplyTiebreakers(standingsList);

            return standingsList;
        }

        private IQueryable<Schedule> GetSchedule(int season)
        {
            return _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == "Regular Season");
        }

        private bool CheckGamesPlayed(List<Standings> tiedTeams)
        {
            return tiedTeams.First()!.GamesPlayed > 0;
        }

        private int GetDivisionRanking(List<Standings> standings, Standings team)
        {
            var divisionStandings = standings
                .Where(s => s.Division == team.Division);

            int ranking = 1;
            while (ranking <= divisionStandings.Count())
            {
                int index = ranking - 1;
                Standings currentTeam = divisionStandings.ElementAt(index);

                if (currentTeam == team)
                    break;

                ranking++;
            }

            return ranking;
        }

        private bool IsTeamDivisionLeader(List<Standings> standings, Standings team, int? ranking = null)
        {
            ranking ??= GetDivisionRanking(standings, team);
            return ranking == 1;
        }

        private async Task<List<Standings>> ApplyTiebreakers(List<Standings> standings)
        {
            for (int index = 0; index < standings.Count() - 1; index++)
            {
                Standings currentTeam = standings[index];
                Standings nextTeam = standings[index + 1];

                if (currentTeam.Wins == nextTeam.Wins &&
                    currentTeam.Losses == nextTeam.Losses)
                {
                    List<Standings> tiedTeams = SearchForMoreTiedTeams(standings, index + 2, currentTeam, nextTeam);
                    int tiedTeamCount = tiedTeams.Count();
                    bool min1GamePlayed = CheckGamesPlayed(tiedTeams);

                    if (min1GamePlayed)
                    {
                        if (tiedTeamCount == 2)
                            standings = ApplyDivisionLeaderTiebreaker(standings, index, currentTeam, nextTeam);
                        else
                            standings = ApplyDivisionLeaderTiebreaker(standings, index, tiedTeams);
                    }

                    index += tiedTeamCount - 1;
                }   
            }

            await StandingsUpdated();
            return standings;
        }

        private List<Standings> SearchForMoreTiedTeams(List<Standings> standings, int startingIndex,
                                                       Standings team1, Standings team2)
        {
            List<Standings> tiedTeams = new List<Standings> { team1, team2 };

            for (int index = startingIndex; index < standings.Count(); index++)
            {
                Standings currentTeam = standings[index];
                Standings previousTeam = standings[index - 1];

                if (currentTeam.Wins == previousTeam.Wins &&
                    currentTeam.Losses == previousTeam.Losses)
                    tiedTeams.Add(currentTeam);
                else
                    break;
            }

            return tiedTeams;
        }

        /// <summary>
        ///     Apply the division leader tiebreaker to a two-way tie.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="index">The index in the standings where the tie starts.</param>
        /// <param name="team1">Team #1 in the tie.</param>
        /// <param name="team2">Team #2 in the tie.</param>
        /// <returns></returns>
        private List<Standings> ApplyDivisionLeaderTiebreaker(List<Standings> standings, int index,
                                                              Standings team1, Standings team2)
        {
            // Get Team #1's division ranking and determine whether they are a division leader
            int team1Ranking = GetDivisionRanking(standings, team1);
            bool team1IsADivisionLeader = IsTeamDivisionLeader(standings, team1, team1Ranking);

            // Get Team #2's division ranking and determine whether they are a division leader
            int team2Ranking = GetDivisionRanking(standings, team2);
            bool team2IsADivisionLeader = IsTeamDivisionLeader(standings, team2, team2Ranking);

            
            // If both teams are the same rank in their division,
            // or they are different ranks but neither of them are division leaders,
            // apply the H2H Series tiebreaker
            if (team1Ranking == team2Ranking || !(team1IsADivisionLeader || team2IsADivisionLeader))
                return ApplyH2HSeriesTiebreaker(standings, index, team1, team2);

            // If Team #2 is a division leader while Team #1 is not, swap the teams
            else if (!team1IsADivisionLeader && team2IsADivisionLeader)
                return SwapTeams(standings, index, team1, team2);


            // If here, Team #1 is a division leader while Team #2 is not.
            // Return the standings as they are.
            return standings;
        }

        /// <summary>
        ///     Apply the division leader tiebreaker to a tie of three teams or more.
        /// </summary>
        /// <param name="standings">The current league standings.</param>
        /// <param name="startingIndex">The index in the standings where the tie starts.</param>
        /// <param name="tiedTeams">The list of teams in the tie being broken.</param>
        /// <returns>The newly updated league standings.</returns>
        private List<Standings> ApplyDivisionLeaderTiebreaker(List<Standings> standings, int startingIndex,
                                                              List<Standings> tiedTeams)
        {
            // Separate the leaders and non-leaders into their own lists
            const int LEADERS = 0, NON_LEADERS = 1;
            var separatedTeams = SeparateLeadersAndNonLeaders(standings, tiedTeams);
            var leaders = separatedTeams[LEADERS];
            var nonLeaders = separatedTeams[NON_LEADERS];
            

            // If there are any division leaders...
            if (leaders.Any())
            {
                // Reorder the tied teams so that the leaders are at the top of the tie,
                // and the rest are at the bottom of the tie
                tiedTeams = ReorderTiedTeamsForDivisionLeaderTiebreaker(leaders, nonLeaders);
                standings = ReorderTeams(standings, startingIndex, tiedTeams);

                // If there are at least 2 division leaders, bring them to the head-to-head gateway
                if (leaders.Count >= 2)
                    standings = GoToH2HGateway(standings, startingIndex, leaders);
            }

            // If there are at least 2 non-leaders, bring them to the head-to-head gateway
            if (nonLeaders.Count >= 2)
                standings = GoToH2HGateway(standings, startingIndex + leaders.Count, nonLeaders);


            // Return the newly updated standings
            return standings;
        }

        /// <summary>
        ///     Separate a list of tied teams into lists for leaders and non-leaders.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="teams">The teams in the tie being broken.</param>
        /// <returns>The lists of leaders and non-leaders wrapped in an array.</returns>
        private List<Standings>[] SeparateLeadersAndNonLeaders(List<Standings> standings, List<Standings> teams)
        {
            // Create separate lists for the leaders and non-leaders
            List<Standings> leaders = new List<Standings>();
            List<Standings> nonLeaders = new List<Standings>();

            // For each team in the tie...
            foreach (var team in teams)
            {
                // Determine whether the current team is a division leader
                bool teamIsADivisionLeader = IsTeamDivisionLeader(standings, team);

                
                // If they are a division leader, add them to the leaders list
                if (teamIsADivisionLeader)
                    leaders.Add(team);
                
                // If not, add them to the non-leaders list
                else
                    nonLeaders.Add(team);
            }

            // Return the lists of leaders and non-leaders wrapped in an array
            return new List<Standings>[] { leaders, nonLeaders };
        }

        /// <summary>
        ///     Reorder a list of tied teams so that any division leaders are at the top,
        ///     and the rest are at the bottom.
        /// </summary>
        /// <param name="leaders">The list of teams leading their division.</param>
        /// <param name="nonLeaders">The list of teams not leading their division.</param>
        /// <returns>The reordered list of teams, with the leaders at the top and the rest at the bottom.</returns>
        private List<Standings> ReorderTiedTeamsForDivisionLeaderTiebreaker(List<Standings> leaders, 
                                                                            List<Standings> nonLeaders)
        {
            // If there are any division leaders...
            if (leaders.Any())
            {
                // Create a list for reordering the teams
                List<Standings> reorderedTeams = new List<Standings>();


                // Add the division leaders to the list first,...
                foreach (var team in leaders)
                    reorderedTeams.Add(team);

                // ... followed by the non-leaders
                foreach (var team in nonLeaders)
                    reorderedTeams.Add(team);


                // Return the re-ordered list of teams
                return reorderedTeams;
            }

            // If there are no division leaders, return the list of non-leaders as it is
            return nonLeaders;
        }

        /// <summary>
        ///     Apply the appropriate head-to-head (H2H) tiebreaker to a list of teams.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="startingIndex">The index in the standings where the tie starts.</param>
        /// <param name="tiedTeams">The teams in the tie being broken.</param>
        /// <returns>The updated league standings with the appropriate tiebreaker applied.</returns>
        private List<Standings> GoToH2HGateway(List<Standings> standings, int startingIndex, List<Standings> tiedTeams)
        {
            // If there are only two tied teams, unpack the list and apply the H2H Series tiebreaker.
            if (tiedTeams.Count == 2)
                return ApplyH2HSeriesTiebreaker(standings, startingIndex, tiedTeams[0], tiedTeams[1]);

            // If there are three or more tied teams, apply the appropriate multi-way tiebreakers.
            if (tiedTeams.Count >= 3)
                return ApplyMultiWayTiebreakers(standings, startingIndex, tiedTeams);

            // If here, there is either one team in the tied teams list, or no teams at all.
            // Return the standings as they are.
            return standings;
        }

        private List<Standings> ApplyH2HSeriesTiebreaker(List<Standings> standings, int index,
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

        private List<Standings> ApplyMultiWayTiebreakers(List<Standings> standings, int startingIndex, 
                                                         List<Standings> tiedTeams)
        {
            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, tiedTeams);
            
            return h2hTiebreakerApplies ?
                ApplyGroupH2HStandingsTiebreaker(standings, startingIndex, tiedTeams) :
                ApplyRegulationWinsTiebreaker(standings, startingIndex, tiedTeams);
        }

        private List<Standings> ApplyGroupH2HStandingsTiebreaker(List<Standings> standings, int index,
                                                                 List<Standings> tiedTeams)
        {
            int season = standings.First()!.Season.Year;
            var games = GetH2HGamesPlayed(season, tiedTeams);
            var teams = ExtractTeams(tiedTeams);

            var h2hStandings = GetMultiWayH2HStats(season, standings, index, tiedTeams);


            List<Standings> sortedTeams = new List<Standings>();
            foreach (var team in h2hStandings)
            {
                var statLine = standings
                    .Where(s => s.Team == team.Key)
                    .FirstOrDefault()!;

                sortedTeams.Add(statLine);
            }

            standings = ReorderTeams(standings, index, sortedTeams);

            List<Standings> teamsTiedAfterH2H = new List<Standings>();
            for (int h2hIndex = 0; h2hIndex < h2hStandings.Count() - 1; h2hIndex++)
            {
                var currentTeam = h2hStandings.ElementAt(h2hIndex);
                var currentTeamStatLine = standings
                    .Where(s => s.Team == currentTeam.Key)
                    .FirstOrDefault()!;

                var nextTeam = h2hStandings.ElementAt(h2hIndex + 1);
                var nextTeamStatLine = standings
                    .Where(s => s.Team == nextTeam.Key)
                    .FirstOrDefault()!;

                if (currentTeam.Value.Wins == nextTeam.Value.Wins &&
                    currentTeam.Value.Losses == nextTeam.Value.Losses)
                {
                    teamsTiedAfterH2H.Add(currentTeamStatLine);
                    teamsTiedAfterH2H.Add(nextTeamStatLine);
                    
                    for (int h2hCursor = h2hIndex + 2; h2hCursor < h2hStandings.Count(); h2hCursor++)
                    {
                        var cursorTeam = h2hStandings.ElementAt(h2hCursor);
                        var cursorTeamStatLine = standings
                            .Where(s => s.Team == cursorTeam.Key)
                            .FirstOrDefault()!;

                        if (cursorTeam.Value.Wins == currentTeam.Value.Wins &&
                            cursorTeam.Value.Losses == currentTeam.Value.Losses)
                            teamsTiedAfterH2H.Add(cursorTeamStatLine);
                        else
                            break;
                    }

                    if (teamsTiedAfterH2H.Count == 2)
                    {
                        standings = ApplyH2HSeriesTiebreaker(standings, index + h2hIndex,
                                                       teamsTiedAfterH2H[0], teamsTiedAfterH2H[1]);
                        h2hIndex++;
                        teamsTiedAfterH2H.Clear();
                        
                        
                        continue;
                    }
                    else
                    {
                        standings = ApplyMultiWayTiebreakers(standings, index + h2hIndex, teamsTiedAfterH2H);
                        h2hIndex += teamsTiedAfterH2H.Count - 1;
                        teamsTiedAfterH2H.Clear();

                        continue;
                    }
                }
            }

            return standings;
        }

        private Dictionary<Team, GroupH2HStats> CreateH2HDictionary(List<Team> teams)
        {
            Dictionary<Team, GroupH2HStats> dictionary = new Dictionary<Team, GroupH2HStats>();
            foreach (var team in teams)
                dictionary.Add(team, new GroupH2HStats());

            return dictionary;
        }

        private IEnumerable<KeyValuePair<Team, GroupH2HStats>> GetMultiWayH2HStats
            (int season, List<Standings> standings, int startingIndex, List<Standings> tiedTeams)
        {
            var teams = ExtractTeams(tiedTeams);
            Dictionary<Team, GroupH2HStats> stats = CreateH2HDictionary(teams);

            int teamCount = teams.Count();
            for (int index = 0; index < teamCount - 1; index++)
            {
                for (int cursor = index + 1; cursor < teamCount; cursor++)
                {
                    Standings team1 = tiedTeams[index];
                    Standings team2 = tiedTeams[cursor];
                    var gamesPlayed = GetH2HGamesPlayed(season, team1, team2);

                    foreach (var game in gamesPlayed)
                    {
                        bool homeTeamWins = game.HomeScore > game.AwayScore;
                        Team winner = homeTeamWins ? game.HomeTeam : game.AwayTeam;
                        Team loser = homeTeamWins ? game.AwayTeam : game.HomeTeam;

                        stats[winner].Wins++;
                        stats[loser].Losses++;
                    }
                }
            }

            var h2hStandings = stats
                .OrderByDescending(s => s.Value.WinPct)
                .ThenByDescending(s => s.Value.Wins)
                .ThenBy(s => s.Value.Losses);

            return h2hStandings;
        }

        /// <summary>
        ///     Determine whether all of the teams in a tie have played each other at least once.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="tiedTeams">The teams in the tie being evaluated.</param>
        /// <returns>True if all of the tied teams have played each other at least once, otherwise False.</returns>
        private bool CheckH2HGamesPlayed(List<Standings> standings, List<Standings> tiedTeams)
        {
            int season = GetSeason(standings);  // Get the current season
            int teamCount = tiedTeams.Count();  // Get the number of tied teams

            // For each possible matchup among the tied teams...
            for (int primaryIndex = 0; primaryIndex < teamCount - 1; primaryIndex++)
            {
                for (int secondaryIndex = primaryIndex + 1; secondaryIndex < teamCount; secondaryIndex++)
                {
                    // Get the teams at the primary and secondary indexes
                    Standings currentTeam = tiedTeams[primaryIndex];
                    Standings nextTeam = tiedTeams[secondaryIndex];

                    // Get the games played between these teams
                    var gamesPlayed = GetH2HGamesPlayed(season, currentTeam, nextTeam);

                    // If these teams have not played each other, return False
                    if (!gamesPlayed.Any())
                        return false;
                }
            }

            // If all the teams in the tie have played each other at least once, return True
            return true;
        }

        /// <summary>
        ///     Apply the RW tiebreaker, then the ROW tiebreaker if necessary, between 2 teams.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="index">The index in the standings where the tie starts.</param>
        /// <param name="team1">Team #1 in the tie.</param>
        /// <param name="team2">Team #2 in the tie.</param>
        /// <returns>The current league standings, re-ordered as necessary.</returns>
        private List<Standings> ApplyWinsByTypeTiebreaker(List<Standings> standings, int index,
                                                          Standings team1, Standings team2)
        {
            // If the two teams are tied for RW's...
            if (team1.RegulationWins == team2.RegulationWins)
            {
                // If the two teams are tied for ROW's, proceed to the group record tiebreakers
                if (team1.RegPlusOTWins == team2.RegPlusOTWins)
                    return ApplyGroupRecordTiebreakers(standings, index, team1, team2);

                // If Team #2 leads in ROW's, swap the teams in the standings
                else if (team1.RegPlusOTWins < team2.RegPlusOTWins)
                    return SwapTeams(standings, index, team1, team2);
            }
            // If Team #2 leads in RW's, swap the teams in the standings
            else if (team1.RegulationWins < team2.RegulationWins)
                return SwapTeams(standings, index, team1, team2);

            // If Team #1 leads either of these tiebreakers, there is no need to swap the teams.
            // Return the standings as they were at the point of entry.
            return standings;
        }

        /// <summary>
        ///     Apply the regulation wins tiebreaker to ties between 3 or more teams.
        /// </summary>
        /// <param name="standings">The current league standings, for reference.</param>
        /// <param name="startingIndex">The index in the standings where the tie starts.</param>
        /// <param name="tiedTeams">The list of teams in the tie currently being broken.</param>
        /// <returns>The updated standings with the tied teams sorted in order of regulation wins, or otherwise.</returns>
        private List<Standings> ApplyRegulationWinsTiebreaker(List<Standings> standings, int startingIndex, 
                                                              List<Standings> tiedTeams)
        {
            // Sort the tied teams in descending order of regulation wins
            var regulationWinsTiebreaker = tiedTeams.OrderByDescending(t => t.RegulationWins);

            // Reorder the tied teams in the standings according to their ranking in the tiebreaker
            standings = ReorderTeams(standings, startingIndex, regulationWinsTiebreaker);


            // Search through the tiebreaker results for any ties
            List<Standings> teamsTiedAfterRW = new List<Standings>();
            for (int index = 0; index < regulationWinsTiebreaker.Count() - 1; index++)
            {
                // Get the teams at the current and next indexes
                Standings currentTeam = regulationWinsTiebreaker.ElementAt(index);
                Standings nextTeam = regulationWinsTiebreaker.ElementAt(index + 1);

                // If these teams are tied for regulation wins...
                if (currentTeam.RegulationWins == nextTeam.RegulationWins)
                {
                    // If the list of tied teams is empty, add the team at the current index to it
                    if (teamsTiedAfterRW.IsNullOrEmpty())
                        teamsTiedAfterRW.Add(currentTeam);

                    // Add the team at the next index to the list
                    teamsTiedAfterRW.Add(nextTeam);

                    // If at the last possible index in the loop...
                    if (index == tiedTeams.Count - 2)
                    {
                        // If all the teams that entered this tiebreaker remain tied, proceed to the ROW tiebreaker
                        if (teamsTiedAfterRW.Count == tiedTeams.Count)
                            return ApplyRegPlusOTWinsTiebreaker(standings, startingIndex, tiedTeams);

                        startingIndex += index - (teamsTiedAfterRW.Count - 2);

                        // If only 2 teams remain tied, restart the tiebreaking process at the H2H Series step for 2-way ties
                        // with those teams
                        if (teamsTiedAfterRW.Count == 2)
                            return ApplyH2HSeriesTiebreaker(standings, startingIndex,
                                                            teamsTiedAfterRW[0], teamsTiedAfterRW[1]);

                        // If 3 or more teams remain tied...
                        if (teamsTiedAfterRW.Count >= 3)
                        {
                            // Check whether the Group H2H Standings tiebreaker applies to those teams
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterRW);

                            
                            // If the tiebreaker DOES apply, apply it to those teams
                            if (h2hTiebreakerApplies)
                                return ApplyGroupH2HStandingsTiebreaker(standings, startingIndex, teamsTiedAfterRW);
                            
                            // If NOT, proceed to the ROW tiebreaker with only those teams
                            else
                                return ApplyRegPlusOTWinsTiebreaker(standings, startingIndex, teamsTiedAfterRW);
                        }
                    }
                }
                // If NOT tied for RW's...
                else
                {
                    // If there are any teams in the list of tied teams...
                    if (teamsTiedAfterRW.Any())
                    {
                        int startingIndexForThisTie = startingIndex + (index - (teamsTiedAfterRW.Count - 1));
                        
                        // If there are only 2 teams in the list, apply the H2H Series tiebreaker to those teams
                        if (teamsTiedAfterRW.Count == 2)
                            standings = ApplyH2HSeriesTiebreaker(standings, startingIndexForThisTie,
                                                                 teamsTiedAfterRW[0], teamsTiedAfterRW[1]);

                        // If there are 3 or more teams in the list...
                        else if (teamsTiedAfterRW.Count >= 3)
                        {
                            // Check whether the Group H2H Standings tiebreaker applies to those teams
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterRW);

                            // If the tiebreaker DOES apply, apply it to those teams
                            if (h2hTiebreakerApplies)
                                standings = ApplyGroupH2HStandingsTiebreaker(standings, startingIndexForThisTie,
                                                                             teamsTiedAfterRW);
                            // If NOT, proceed to the ROW tiebreaker with only those teams
                            else
                                standings = ApplyRegPlusOTWinsTiebreaker(standings, startingIndexForThisTie, 
                                                                         teamsTiedAfterRW);
                        }

                        // Clear the list of tied teams
                        teamsTiedAfterRW.Clear();
                    }
                }
            }

            // If no teams remain tied, return the reordered standings
            return standings;
        }

        private List<Standings> ApplyRegPlusOTWinsTiebreaker(List<Standings> standings, int startingIndex,
                                                             List<Standings> tiedTeams)
        {
            var regPlusOTWinsTiebreaker = tiedTeams.OrderByDescending(t => t.RegPlusOTWins);
            standings = ReorderTeams(standings, startingIndex, regPlusOTWinsTiebreaker);

            List<Standings> teamsTiedAfterROW = new List<Standings>();
            for (int index = 0; index < regPlusOTWinsTiebreaker.Count() - 1; index++)
            {
                Standings currentTeam = regPlusOTWinsTiebreaker.ElementAt(index);
                Standings nextTeam = regPlusOTWinsTiebreaker.ElementAt(index + 1);

                if (currentTeam.RegPlusOTWins == nextTeam.RegPlusOTWins)
                {
                    if (!teamsTiedAfterROW.Any())
                        teamsTiedAfterROW.Add(currentTeam);

                    teamsTiedAfterROW.Add(nextTeam);

                    if (index == tiedTeams.Count - 2)
                    {
                        if (teamsTiedAfterROW.Count == tiedTeams.Count)
                            return ApplyDivisionRecordTiebreaker(standings, startingIndex, tiedTeams);

                        startingIndex += index - (teamsTiedAfterROW.Count - 2);

                        if (teamsTiedAfterROW.Count == 2)
                            return ApplyH2HSeriesTiebreaker(standings, startingIndex,
                                                            teamsTiedAfterROW[0], teamsTiedAfterROW[1]);

                        else if (teamsTiedAfterROW.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterROW);

                            if (h2hTiebreakerApplies)
                                return ApplyGroupH2HStandingsTiebreaker(standings, startingIndex, teamsTiedAfterROW);
                            
                            return ApplyDivisionRecordTiebreaker(standings, startingIndex, teamsTiedAfterROW);
                        }
                            
                    }
                }
                else
                {
                    if (teamsTiedAfterROW.Any())
                    {
                        int startingIndexForThisTie = startingIndex + (index - (teamsTiedAfterROW.Count - 1));

                        if (teamsTiedAfterROW.Count == 2)
                            standings = ApplyH2HSeriesTiebreaker(standings, startingIndexForThisTie,
                                                                 teamsTiedAfterROW[0], teamsTiedAfterROW[1]);

                        if (teamsTiedAfterROW.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterROW);

                            if (h2hTiebreakerApplies)
                                standings = ApplyGroupH2HStandingsTiebreaker(standings, startingIndexForThisTie,
                                                                             teamsTiedAfterROW);
                            else
                                standings = ApplyDivisionRecordTiebreaker(standings, startingIndexForThisTie, 
                                                                          teamsTiedAfterROW);
                        }

                        teamsTiedAfterROW.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyGroupRecordTiebreakers(List<Standings> standings, int index,
                                                            Standings team1, Standings team2)
        {
            if (team1.Division == team2.Division)
                return BreakTieWithinDivision(standings, index, team1, team2);

            if (team1.Conference == team2.Conference && 
                team1.Division != team2.Division)
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

        private List<Standings> ApplyDivisionRecordTiebreaker(List<Standings> standings, int startingIndex,
                                                              List<Standings> tiedTeams)
        {
            var divisionRecordTiebreaker = tiedTeams
                .OrderByDescending(t => t.WinPctVsDivision)
                .ThenByDescending(t => t.WinsVsDivision)
                .ThenBy(t => t.LossesVsDivision);
            standings = ReorderTeams(standings, startingIndex, divisionRecordTiebreaker);

            List<Standings> teamsTiedAfterDivisionRecords = new List<Standings>();
            for (int index = 0; index < divisionRecordTiebreaker.Count() - 1; index++)
            {
                Standings currentTeam = divisionRecordTiebreaker.ElementAt(index);
                Standings nextTeam = divisionRecordTiebreaker.ElementAt(index + 1);

                if (currentTeam.WinsVsDivision == nextTeam.WinsVsDivision &&
                    currentTeam.LossesVsDivision == nextTeam.LossesVsDivision)
                {
                    if (teamsTiedAfterDivisionRecords.IsNullOrEmpty())
                        teamsTiedAfterDivisionRecords.Add(currentTeam);

                    teamsTiedAfterDivisionRecords.Add(nextTeam);

                    if (index == tiedTeams.Count - 2)
                    {
                        if (teamsTiedAfterDivisionRecords.Count == tiedTeams.Count)
                            return ApplyConferenceRecordTiebreaker(standings, startingIndex, tiedTeams);

                        startingIndex += index - (teamsTiedAfterDivisionRecords.Count - 2);

                        if (teamsTiedAfterDivisionRecords.Count == 2)
                            return ApplyH2HSeriesTiebreaker(standings, startingIndex,
                                                            teamsTiedAfterDivisionRecords[0], 
                                                            teamsTiedAfterDivisionRecords[1]);

                        if (teamsTiedAfterDivisionRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterDivisionRecords);

                            if (h2hTiebreakerApplies)
                                return ApplyGroupH2HStandingsTiebreaker(standings, startingIndex,
                                                                        teamsTiedAfterDivisionRecords);
                            else
                                return ApplyConferenceRecordTiebreaker(standings, startingIndex, 
                                                                       teamsTiedAfterDivisionRecords);
                        }
                            
                    }
                }
                else
                {
                    if (teamsTiedAfterDivisionRecords.Any())
                    {
                        int startingIndexForThisTie = startingIndex + (index - (teamsTiedAfterDivisionRecords.Count - 1));

                        if (teamsTiedAfterDivisionRecords.Count == 2)
                            standings = ApplyH2HSeriesTiebreaker(standings, startingIndexForThisTie,
                                                                 teamsTiedAfterDivisionRecords[0],
                                                                 teamsTiedAfterDivisionRecords[1]);

                        if (teamsTiedAfterDivisionRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterDivisionRecords);

                            if (h2hTiebreakerApplies)
                                standings = ApplyGroupH2HStandingsTiebreaker(standings, startingIndexForThisTie,
                                                                             teamsTiedAfterDivisionRecords);
                            else
                                standings = ApplyConferenceRecordTiebreaker(standings, startingIndexForThisTie,
                                                                            teamsTiedAfterDivisionRecords);
                        }

                        teamsTiedAfterDivisionRecords.Clear();
                    }
                }
            }

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

        private List<Standings> ApplyConferenceRecordTiebreaker(List<Standings> standings, int startingIndex,
                                                                List<Standings> tiedTeams)
        {
            var conferenceRecordTiebreaker = tiedTeams
                .OrderByDescending(t => t.WinPctVsConference)
                .ThenByDescending(t => t.WinsVsConference)
                .ThenBy(t => t.LossesVsConference);
            standings = ReorderTeams(standings, startingIndex, conferenceRecordTiebreaker);

            List<Standings> teamsTiedAfterConferenceRecords = new List<Standings>();
            for (int index = 0; index < conferenceRecordTiebreaker.Count() - 1; index++)
            {
                Standings currentTeam = conferenceRecordTiebreaker.ElementAt(index);
                Standings nextTeam = conferenceRecordTiebreaker.ElementAt(index + 1);

                if (currentTeam.WinsVsConference == nextTeam.WinsVsConference &&
                    currentTeam.LossesVsConference == nextTeam.LossesVsConference)
                {
                    if (teamsTiedAfterConferenceRecords.IsNullOrEmpty())
                        teamsTiedAfterConferenceRecords.Add(currentTeam);

                    teamsTiedAfterConferenceRecords.Add(nextTeam);

                    if (index == tiedTeams.Count - 2)
                    {
                        if (teamsTiedAfterConferenceRecords.Count == tiedTeams.Count)
                            return ApplyInterConferenceRecordTiebreaker(standings, startingIndex, tiedTeams);

                        startingIndex += index - (teamsTiedAfterConferenceRecords.Count - 2);

                        if (teamsTiedAfterConferenceRecords.Count == 2)
                            return ApplyH2HSeriesTiebreaker(standings, startingIndex,
                                                            teamsTiedAfterConferenceRecords[0],
                                                            teamsTiedAfterConferenceRecords[1]);

                        if (teamsTiedAfterConferenceRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterConferenceRecords);

                            if (h2hTiebreakerApplies)
                                return ApplyGroupH2HStandingsTiebreaker(standings, startingIndex,
                                                                        teamsTiedAfterConferenceRecords);
                            else
                                return ApplyInterConferenceRecordTiebreaker(standings, startingIndex,
                                                                            teamsTiedAfterConferenceRecords);
                        }
                    }
                }
                else
                {
                    if (teamsTiedAfterConferenceRecords.Any())
                    {
                        int startingIndexForThisTie = startingIndex + (index - (teamsTiedAfterConferenceRecords.Count - 1));
                        
                        if (teamsTiedAfterConferenceRecords.Count == 2)
                            standings = ApplyH2HSeriesTiebreaker(standings, startingIndexForThisTie,
                                                                 teamsTiedAfterConferenceRecords[0],
                                                                 teamsTiedAfterConferenceRecords[1]);

                        else if (teamsTiedAfterConferenceRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterConferenceRecords);

                            if (h2hTiebreakerApplies)
                                standings = ApplyGroupH2HStandingsTiebreaker(standings, startingIndexForThisTie,
                                                                             teamsTiedAfterConferenceRecords);
                            else
                                standings = ApplyInterConferenceRecordTiebreaker(standings, startingIndexForThisTie,
                                                                                 teamsTiedAfterConferenceRecords);
                        }

                        teamsTiedAfterConferenceRecords.Clear();
                    }
                }
            }

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

        private List<Standings> ApplyInterConferenceRecordTiebreaker(List<Standings> standings, int startingIndex,
                                                                     List<Standings> tiedTeams)
        {
            var interConferenceRecordTiebreaker = tiedTeams
                .OrderByDescending(t => t.InterConfWinPct)
                .ThenByDescending(t => t.InterConfWins)
                .ThenBy(t => t.InterConfLosses);
            standings = ReorderTeams(standings, startingIndex, interConferenceRecordTiebreaker);

            List<Standings> teamsTiedAfterInterConferenceRecords = new List<Standings>();
            for (int index = 0; index < interConferenceRecordTiebreaker.Count() - 1; index++)
            {
                Standings currentTeam = interConferenceRecordTiebreaker.ElementAt(index);
                Standings nextTeam = interConferenceRecordTiebreaker.ElementAt(index + 1);

                if (currentTeam.InterConfWins == nextTeam.InterConfWins &&
                    currentTeam.InterConfLosses == nextTeam.InterConfLosses)
                {
                    if (teamsTiedAfterInterConferenceRecords.IsNullOrEmpty())
                        teamsTiedAfterInterConferenceRecords.Add(currentTeam);

                    teamsTiedAfterInterConferenceRecords.Add(nextTeam);

                    if (index == tiedTeams.Count - 2)
                    {
                        if (teamsTiedAfterInterConferenceRecords.Count == tiedTeams.Count)
                            return standings;

                        startingIndex += index - (teamsTiedAfterInterConferenceRecords.Count - 2);

                        if (teamsTiedAfterInterConferenceRecords.Count == 2)
                            return ApplyH2HSeriesTiebreaker(standings, startingIndex,
                                                            teamsTiedAfterInterConferenceRecords[0],
                                                            teamsTiedAfterInterConferenceRecords[1]);

                        if (teamsTiedAfterInterConferenceRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterInterConferenceRecords);

                            if (h2hTiebreakerApplies)
                                return ApplyGroupH2HStandingsTiebreaker(standings, startingIndex,
                                                                        teamsTiedAfterInterConferenceRecords);
                        }
                    }
                }
                else
                {
                    if (teamsTiedAfterInterConferenceRecords.Any())
                    {
                        int startingIndexForThisTie = startingIndex + 
                                                      (index - (teamsTiedAfterInterConferenceRecords.Count - 1));

                        if (teamsTiedAfterInterConferenceRecords.Count == 2)
                            standings = ApplyH2HSeriesTiebreaker(standings, startingIndexForThisTie,
                                                                 teamsTiedAfterInterConferenceRecords[0],
                                                                 teamsTiedAfterInterConferenceRecords[1]);

                        if (teamsTiedAfterInterConferenceRecords.Count >= 3)
                        {
                            bool h2hTiebreakerApplies = CheckH2HGamesPlayed(standings, teamsTiedAfterInterConferenceRecords);

                            if (h2hTiebreakerApplies)
                                standings = ApplyGroupH2HStandingsTiebreaker(standings, startingIndexForThisTie,
                                                                             teamsTiedAfterInterConferenceRecords);
                        }

                        teamsTiedAfterInterConferenceRecords.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> SwapTeams(List<Standings> standings, int index,
                                          Standings team1, Standings team2)
        {
            standings[index] = team2;
            standings[index + 1] = team1;
            
            return standings;
        }

        private List<Standings> ReorderTeams(List<Standings> standings, int startingIndex, IEnumerable<Standings> sortedTeams)
        {
            int sortedTeamCount = sortedTeams.Count();
            for (int index = 0; index < sortedTeamCount; index++)
            {
                int currentIndex = startingIndex + index;
                standings[currentIndex] = sortedTeams.ElementAt(index);
            }

            return standings;
        }

        private List<Team> ExtractTeams(List<Standings> standings)
        {
            List<Team> teams = new List<Team>();

            foreach (var team in standings)
                teams.Add(team.Team);

            return teams;
        }

        private Standings GetTeamStatLine(List<Standings> standings, Team team)
        {
            var statLine = standings
                .Where(s => s.Team == team)
                .FirstOrDefault()!;

            return statLine;
        }

        private int GetSeason(List<Standings> standings)
        {
            return standings.First()!.Season.Year;
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
            var schedule = GetSchedule(season);
            var games = schedule
                .Where(s => ((s.AwayTeamId == team1.TeamId && s.HomeTeamId == team2.TeamId) ||
                             (s.AwayTeamId == team2.TeamId && s.HomeTeamId == team1.TeamId)));

            return games;
        }

        private IQueryable<Schedule> GetH2HGames(int season, List<Standings> teams)
        {
            var schedule = GetSchedule(season);
            var teamNames = ExtractTeams(teams);
            var games = schedule
                .Where(s => teamNames.Contains(s.AwayTeam) && 
                            teamNames.Contains(s.HomeTeam));

            return games;
        }

        private IQueryable<Schedule> GetH2HGamesPlayed(int season, Standings team1, Standings team2)
        {
            var h2hGames = GetH2HGames(season, team1, team2);
            return h2hGames.Where(g => g.IsFinalized && g.Period >= 3);
        }

        private IQueryable<Schedule> GetH2HGamesPlayed(int season, List<Standings> teams)
        {
            var games = GetH2HGames(season, teams);
            return games.Where(g => g.IsFinalized && g.Period >= 3);
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
