using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Humanizer.Localisation.TimeToClockNotation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging.Signing;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class StandingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        private int SEASON;
        const int TEAM1 = 0, TEAM2 = 1;

        public StandingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Standings
        public async Task<IActionResult> Index(int season, string viewBy)
        {
            SEASON = season;
            ViewBag.Season = season;
            ViewBag.ViewBy = viewBy;

            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var sortOptions = _context.StandingsSortOptions
                .Where(s => s.LastYear >= season || s.LastYear == null)
                .OrderBy(s => s.Index);
            ViewBag.SortOptions = new SelectList(sortOptions, "Parameter", "Name");

            ViewBag.Conferences = GetConferences(season);
            ViewBag.Divisions = GetDivisions(season);

            var teams = _context.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .Distinct()
                .OrderBy(a => a.Team.City)
                .OrderBy(a => a.Team.Name);

            List<Standings> standings = await GetStandings(season);

            var leaders = GetLeaders(standings);
            ViewBag.Leaders = leaders;

            var wildCards = GetWildCards(standings, leaders);
            ViewBag.WildCards = wildCards;

            return View(standings);
        }

        [Route("Standings/PlayoffBracket/{season}")]
        public async Task<IActionResult> PlayoffBracket(int season)
        {
            ViewBag.Season = season;
            
            var matchups = await _context.PlayoffSeries
                .Include(ps => ps.Season)
                .Include(ps => ps.Round)
                .Include(ps => ps.Team1)
                .Include(ps => ps.Team2)
                .Where(ps => ps.Season.Year == season)
                .ToListAsync();

            return View(matchups);
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

        public List<Standings> GetWildCards(List<Standings> standings, List<Standings>? leaders = null)
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

        [Route("Standings/PlayoffMatchups/{season}")]
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
                    .Where(s => s.ConferenceId == conference!.Id)
                    .ToList();

                var divisions = conferenceStandings
                    .Select(c => c.Division)
                    .Distinct();

                List<Standings> leaders = new List<Standings>();
                List<Standings> wildCards = new List<Standings>();
                foreach (var division in divisions)
                {
                    var divisionStandings = conferenceStandings
                        .Where(c => c.DivisionId == division!.Id);

                    var leader = divisionStandings.First();
                    leaders.Add(leader);
                }

                int playoffTeamsPerConference = (season > 2021) ? 8 : 4;
                int maxFollowerCount = playoffTeamsPerConference - leaders.Count;
                foreach (var team in conferenceStandings)
                {
                    if (wildCards.Count >= maxFollowerCount)
                        break;
                    
                    if (leaders.Contains(team))
                        continue;

                    wildCards.Add(team);
                }

                foreach (var team in leaders)
                    playoffTeams.Last().Add(team);

                foreach (var team in wildCards)
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
                    string[] codes = new string[2];

                    matchup[0] = conference[index];
                    matchup[1] = conference[(teamCount - 1) - index];
                    
                    matchups.Last().Add(matchup);
                }
            }

            return View(matchups);
        }

        [Route("Standings/PlayoffStatus/{season}/{team}")]
        public async Task<IActionResult> PlayoffStatus(int season, string team, string currentStatus, string viewBy)
        {
            var statLine = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Season.Year == season &&
                                     s.Team.Code == team);

            var playoffStatuses = _context.PlayoffStatuses
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

            statLine!.PlayoffStatus = s.PlayoffStatus;
            
            _context.Update(statLine);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { season = statLine.Season.Year, viewBy = viewBy });
        }

        public IActionResult Tiebreakers()
        {
            return View();
        }

        public IActionResult PlayoffFormat()
        {
            return View();
        }

        [Route("Standings/HeadToHead/{season}/{team1Code}/{team2Code}/")]
        public async Task<IActionResult> HeadToHeadComparison(int season, string? team1Code, string? team2Code)
        {
            var seasons = _context.Seasons
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

            // Note: Head-to-head tiebreaking system is suspended indefinitely.
            //if (season >= 2025)
                //standingsList = await ApplyTiebreakers(standingsList);

            return standingsList;
        }

        private async Task<List<Standings>> ApplyTiebreakers(List<Standings> standings)
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
                        if (tiedTeams.Count == standings.Count &&
                            tiedTeams.First().GamesPlayed == 0)
                            return standings;

                        indexes = GetIndexesInLeagueStandings(standings, tiedTeams);

                        standings = tiedTeams.Count == 2 ?
                            ApplyTwoWayTiebreakers(standings, indexes[0], tiedTeams[0], indexes[1], tiedTeams[1]) :
                            ApplyMultiWayTiebreakers(standings, indexes, tiedTeams);
                    }
                }
                else
                {
                    if (tiedTeams.Any())
                    {
                        indexes = GetIndexesInLeagueStandings(standings, tiedTeams);
                        standings = tiedTeams.Count == 2 ?
                            ApplyTwoWayTiebreakers(standings, indexes[0], tiedTeams[0], indexes[1], tiedTeams[1]) :
                            ApplyMultiWayTiebreakers(standings, indexes, tiedTeams);

                        tiedTeams.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyTwoWayTiebreakers(List<Standings> standings, 
                                                       int index1, Standings team1, 
                                                       int index2, Standings team2)
        {
            if (team1.Division == team2.Division)
                return ApplyHeadToHeadTiebreaker(standings, index1, team1, index2, team2);

            return ApplyDivisionLeaderTiebreaker(standings, index1, team1, index2, team2);
        }

        private List<Standings> ApplyMultiWayTiebreakers(List<Standings> standings, 
                                                         List<int> indexes, 
                                                         List<Standings> tiedTeams)
        {
            

            /*var tiedTeamsByDivision = GetTiedTeamsByDivision(tiedTeams);
            var tiedTeamsByConference = GetTiedTeamsByConference(tiedTeams);

            List<Standings> teams;
            List<int> indexesInTie;
            foreach (var division in tiedTeamsByDivision)
            {
                teams = division.Value;

                if (teams.Count >= 2)
                {
                    bool tiebreakerCanDetermineDivisionLeader = CanTiebreakerDetermineDivisionLeader(standings, teams);
                    
                    if (tiebreakerCanDetermineDivisionLeader)
                    {
                        indexesInTie = GetIndexesInLeagueStandings(standings, teams);
                        standings = teams.Count == 2 ?
                            ApplyTwoWayTiebreakers(standings,
                                                   indexesInTie[0], teams[0],
                                                   indexesInTie[1], teams[1]) :
                            ApplyMultiWayTiebreakers(standings, indexesInTie, teams);
                    }
                }
            }

            foreach (var conference in tiedTeamsByConference)
            {
                teams = conference.Value;

                if (teams.Count >= 2)
                {
                    bool tiebreakerCanDeterminePlayoffTeams = CanTiebreakerDeterminePlayoffTeams(standings, teams);
                    
                    if (tiebreakerCanDeterminePlayoffTeams)
                    {
                        indexesInTie = GetIndexesInLeagueStandings(standings, teams);
                        standings = teams.Count == 2 ?
                            ApplyTwoWayTiebreakers(standings,
                                                   indexesInTie[0], teams[0],
                                                   indexesInTie[1], teams[1]) :
                            ApplyMultiWayTiebreakers(standings, indexesInTie, teams);
                    }
                }
            }*/



            return standings;
        }

        private List<Standings> ApplyHeadToHeadTiebreaker(List<Standings> standings,
                                                          int index1, Standings team1,
                                                          int index2, Standings team2)
        {
            const int TEAM1 = 0, TEAM2 = 1;
            int[] h2hSeries = GetH2HSeries(SEASON, team1, team2);

            if (h2hSeries[TEAM1] == h2hSeries[TEAM2])
                return ApplyRegulationWinsTiebreaker(standings, index1, team1, index2, team2);
            else if (h2hSeries[TEAM1] < h2hSeries[TEAM2])
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyDivisionLeaderTiebreaker(List<Standings> standings, 
                                                              int index1, Standings team1, 
                                                              int index2, Standings team2)
        {
            bool team1IsDivisionLeader = IsTeamDivisionLeader(standings, team1);
            bool team2IsDivisionLeader = IsTeamDivisionLeader(standings, team2);

            if (team1IsDivisionLeader == team2IsDivisionLeader)
            {
                if ((team1IsDivisionLeader && team2IsDivisionLeader) || team1.Conference == team2.Conference)
                    return ApplyRegulationWinsTiebreaker(standings, index1, team1, index2, team2);

                return ApplyPlayoffTeamTiebreaker(standings, index1, team1, index2, team2);
            }
            else if (!team1IsDivisionLeader && team2IsDivisionLeader)
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyDivisionLeaderTiebreaker(List<Standings> standings,
                                                              List<int> indexes,
                                                              List<Standings> tiedTeams)
        {
            var statuses = GetDivisionLeadingStatuses(standings, tiedTeams);
            var divisionLeaderTiebreaker = statuses.OrderByDescending(s => s.Value);
            var teams = divisionLeaderTiebreaker
                .ToDictionary(s => s.Key)
                .Keys.ToList();

            standings = ReorderTeams(standings, indexes, teams);

            List<Standings> teamsTiedAfterDivisionLeaderTiebreaker = new List<Standings>();
            for (int index = 0; index < divisionLeaderTiebreaker.Count() - 1; index++)
            {
                var currentTeam = divisionLeaderTiebreaker.ElementAt(index);
                var nextTeam = divisionLeaderTiebreaker.ElementAt(index + 1);

                if (currentTeam.Value == nextTeam.Value)
                {
                    if (teamsTiedAfterDivisionLeaderTiebreaker.IsNullOrEmpty())
                        teamsTiedAfterDivisionLeaderTiebreaker.Add(currentTeam.Key);

                    teamsTiedAfterDivisionLeaderTiebreaker.Add(nextTeam.Key);

                    if (index == divisionLeaderTiebreaker.Count() - 2)
                    {
                        if (teamsTiedAfterDivisionLeaderTiebreaker.Count == tiedTeams.Count)
                            return ApplyPlayoffTeamTiebreaker(standings, indexes, tiedTeams);

                        indexes = GetIndexesInLeagueStandings(standings, teamsTiedAfterDivisionLeaderTiebreaker);

                        if (teamsTiedAfterDivisionLeaderTiebreaker.Count == 2)
                        {
                            return currentTeam.Value ?
                                ApplyRegulationWinsTiebreaker(standings,
                                                              indexes[TEAM1],
                                                              teamsTiedAfterDivisionLeaderTiebreaker[TEAM1],
                                                              indexes[TEAM2],
                                                              teamsTiedAfterDivisionLeaderTiebreaker[TEAM2]) :
                                ApplyPlayoffTeamTiebreaker(standings,
                                                           indexes[TEAM1], teamsTiedAfterDivisionLeaderTiebreaker[TEAM1],
                                                           indexes[TEAM2], teamsTiedAfterDivisionLeaderTiebreaker[TEAM2]);
                        }   

                        return currentTeam.Value ?
                            ApplyRegulationWinsTiebreaker(standings, indexes, teamsTiedAfterDivisionLeaderTiebreaker) :
                            ApplyPlayoffTeamTiebreaker(standings, indexes, teamsTiedAfterDivisionLeaderTiebreaker);
                    }
                }
                else
                {
                    if (teamsTiedAfterDivisionLeaderTiebreaker.Any())
                    {
                        List<int> indexesInTie = GetIndexesInLeagueStandings(standings, 
                                                                             teamsTiedAfterDivisionLeaderTiebreaker);

                        if (teamsTiedAfterDivisionLeaderTiebreaker.Count == 2)
                        {
                            standings = currentTeam.Value ?
                                ApplyRegulationWinsTiebreaker(standings,
                                                              indexesInTie[TEAM1], 
                                                              teamsTiedAfterDivisionLeaderTiebreaker[TEAM1],
                                                              indexesInTie[TEAM2], 
                                                              teamsTiedAfterDivisionLeaderTiebreaker[TEAM2]) :
                                ApplyPlayoffTeamTiebreaker(standings,
                                                           indexesInTie[TEAM1], 
                                                           teamsTiedAfterDivisionLeaderTiebreaker[TEAM1],
                                                           indexesInTie[TEAM2], 
                                                           teamsTiedAfterDivisionLeaderTiebreaker[TEAM2]);
                        }
                        else
                        {
                            standings = currentTeam.Value ?
                                ApplyRegulationWinsTiebreaker(standings, indexesInTie,
                                                              teamsTiedAfterDivisionLeaderTiebreaker) :
                                ApplyPlayoffTeamTiebreaker(standings, indexesInTie, teamsTiedAfterDivisionLeaderTiebreaker);
                        }

                        teamsTiedAfterDivisionLeaderTiebreaker.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyPlayoffTeamTiebreaker(List<Standings> standings, 
                                                           int index1, Standings team1, 
                                                           int index2, Standings team2)
        {
            bool team1IsInAPlayoffSpot = IsTeamInPlayoffSpot(standings, team1);
            bool team2IsInAPlayoffSpot = IsTeamInPlayoffSpot(standings, team2);

            if (team1IsInAPlayoffSpot == team2IsInAPlayoffSpot)
                return ApplyRegulationWinsTiebreaker(standings, index1, team1, index2, team2);
            else if (!team1IsInAPlayoffSpot && team2IsInAPlayoffSpot)
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyPlayoffTeamTiebreaker(List<Standings> standings, 
                                                           List<int> indexes, 
                                                           List<Standings> tiedTeams)
        {
            var statuses = GetPlayoffStatuses(standings, tiedTeams);
            var playoffTeamTiebreaker = statuses.OrderByDescending(s => s.Value);
            var teams = playoffTeamTiebreaker
                .ToDictionary(s => s.Key)
                .Keys.ToList();

            standings = ReorderTeams(standings, indexes, teams);

            List<Standings> teamsTiedAfterPlayoffTeamTiebreaker = new List<Standings>();
            for (int index = 0; index < playoffTeamTiebreaker.Count() - 1; index++)
            {
                var currentTeam = playoffTeamTiebreaker.ElementAt(index);
                var nextTeam = playoffTeamTiebreaker.ElementAt(index + 1);

                if (currentTeam.Value == nextTeam.Value)
                {
                    if (teamsTiedAfterPlayoffTeamTiebreaker.IsNullOrEmpty())
                        teamsTiedAfterPlayoffTeamTiebreaker.Add(currentTeam.Key);

                    teamsTiedAfterPlayoffTeamTiebreaker.Add(nextTeam.Key);

                    if (index == playoffTeamTiebreaker.Count() - 2)
                    {
                        if (teamsTiedAfterPlayoffTeamTiebreaker.Count == tiedTeams.Count)
                            return ApplyRegulationWinsTiebreaker(standings, indexes, tiedTeams);

                        indexes = GetIndexesInLeagueStandings(standings, teamsTiedAfterPlayoffTeamTiebreaker);

                        if (teamsTiedAfterPlayoffTeamTiebreaker.Count == 2)
                            return ApplyRegulationWinsTiebreaker(standings,
                                                                 indexes[TEAM1],
                                                                 teamsTiedAfterPlayoffTeamTiebreaker[TEAM1],
                                                                 indexes[TEAM2],
                                                                 teamsTiedAfterPlayoffTeamTiebreaker[TEAM2]);

                        return ApplyRegulationWinsTiebreaker(standings, indexes, teamsTiedAfterPlayoffTeamTiebreaker);
                    }
                }
                else
                {
                    if (teamsTiedAfterPlayoffTeamTiebreaker.Any())
                    {
                        List<int> indexesInTie = GetIndexesInLeagueStandings(standings, teamsTiedAfterPlayoffTeamTiebreaker);

                        standings = teamsTiedAfterPlayoffTeamTiebreaker.Count == 2 ?
                            ApplyRegulationWinsTiebreaker(standings,
                                                          indexes[TEAM1], teamsTiedAfterPlayoffTeamTiebreaker[TEAM1],
                                                          indexes[TEAM2], teamsTiedAfterPlayoffTeamTiebreaker[TEAM2]) :
                            ApplyRegulationWinsTiebreaker(standings, indexes, teamsTiedAfterPlayoffTeamTiebreaker);

                        teamsTiedAfterPlayoffTeamTiebreaker.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyRegulationWinsTiebreaker(List<Standings> standings,
                                                              int index1, Standings team1,
                                                              int index2, Standings team2)
        {
            if (team1.RegulationWins == team2.RegulationWins)
                return ApplyRegPlusOTWinsTiebreaker(standings, index1, team1, index2, team2);
            else if (team1.RegulationWins < team2.RegulationWins)
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyRegulationWinsTiebreaker(List<Standings> standings, 
                                                              List<int> indexes, 
                                                              List<Standings> tiedTeams)
        {
            var regulationWinsTiebreaker = tiedTeams.OrderByDescending(s => s.RegulationWins)
                                                    .ToList();
            standings = ReorderTeams(standings, indexes, regulationWinsTiebreaker);

            List<Standings> teamsTiedAfterRWTiebreaker = new List<Standings>();
            for (int index = 0; index < regulationWinsTiebreaker.Count - 1; index++)
            {
                Standings currentTeam = regulationWinsTiebreaker[index];
                Standings nextTeam = regulationWinsTiebreaker[index + 1];

                if (currentTeam.RegulationWins == nextTeam.RegulationWins)
                {
                    if (teamsTiedAfterRWTiebreaker.IsNullOrEmpty())
                        teamsTiedAfterRWTiebreaker.Add(currentTeam);

                    teamsTiedAfterRWTiebreaker.Add(nextTeam);

                    if (index == regulationWinsTiebreaker.Count - 2)
                    {
                        if (teamsTiedAfterRWTiebreaker.Count == tiedTeams.Count)
                            return ApplyRegPlusOTWinsTiebreaker(standings, indexes, tiedTeams);

                        indexes = GetIndexesInLeagueStandings(standings, teamsTiedAfterRWTiebreaker);

                        return teamsTiedAfterRWTiebreaker.Count == 2 ?
                            ApplyRegPlusOTWinsTiebreaker(standings,
                                                         indexes[TEAM1], teamsTiedAfterRWTiebreaker[TEAM1],
                                                         indexes[TEAM2], teamsTiedAfterRWTiebreaker[TEAM2]) :
                            ApplyRegPlusOTWinsTiebreaker(standings, indexes, teamsTiedAfterRWTiebreaker);
                    }
                }
                else
                {
                    if (teamsTiedAfterRWTiebreaker.Any())
                    {
                        List<int> indexesInTie = GetIndexesInLeagueStandings(standings, teamsTiedAfterRWTiebreaker);

                        standings = teamsTiedAfterRWTiebreaker.Count == 2 ?
                            ApplyRegPlusOTWinsTiebreaker(standings,
                                                         indexesInTie[TEAM1], teamsTiedAfterRWTiebreaker[TEAM1],
                                                         indexesInTie[TEAM2], teamsTiedAfterRWTiebreaker[TEAM2]) :
                            ApplyRegPlusOTWinsTiebreaker(standings, indexesInTie, teamsTiedAfterRWTiebreaker);

                        teamsTiedAfterRWTiebreaker.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyRegPlusOTWinsTiebreaker(List<Standings> standings, 
                                                             int index1, Standings team1, 
                                                             int index2, Standings team2)
        {
            if (team1.RegPlusOTWins == team2.RegPlusOTWins)
                return ApplyH2HSeriesTiebreaker(standings, index1, team1, index2, team2);
            else if (team1.RegPlusOTWins < team2.RegPlusOTWins)
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyRegPlusOTWinsTiebreaker(List<Standings> standings, 
                                                             List<int> indexes, 
                                                             List<Standings> tiedTeams)
        {
            var regPlusOTWinsTiebreaker = tiedTeams.OrderByDescending(s => s.RegPlusOTWins)
                                                   .ToList();
            standings = ReorderTeams(standings, indexes, regPlusOTWinsTiebreaker);

            List<Standings> teamsTiedAfterROWTiebreaker = new List<Standings>();
            for (int index = 0; index < regPlusOTWinsTiebreaker.Count - 1; index++)
            {
                Standings currentTeam = regPlusOTWinsTiebreaker[index];
                Standings nextTeam = regPlusOTWinsTiebreaker[index + 1];

                if (currentTeam.RegPlusOTWins == nextTeam.RegPlusOTWins)
                {
                    if (teamsTiedAfterROWTiebreaker.IsNullOrEmpty())
                        teamsTiedAfterROWTiebreaker.Add(currentTeam);

                    teamsTiedAfterROWTiebreaker.Add(nextTeam);

                    if (index == regPlusOTWinsTiebreaker.Count - 2)
                    {
                        if (teamsTiedAfterROWTiebreaker.Count == tiedTeams.Count)
                            return ApplyGroupH2HStandingsTiebreaker(standings, indexes, tiedTeams);

                        indexes = GetIndexesInLeagueStandings(standings, teamsTiedAfterROWTiebreaker);

                        return teamsTiedAfterROWTiebreaker.Count == 2 ?
                            ApplyH2HSeriesTiebreaker(standings,
                                                     indexes[TEAM1], teamsTiedAfterROWTiebreaker[TEAM1],
                                                     indexes[TEAM2], teamsTiedAfterROWTiebreaker[TEAM2]) :
                            ApplyGroupH2HStandingsTiebreaker(standings, indexes, teamsTiedAfterROWTiebreaker);
                    }
                }
                else
                {
                    if (teamsTiedAfterROWTiebreaker.Any())
                    {
                        List<int> indexesInTie = GetIndexesInLeagueStandings(standings, teamsTiedAfterROWTiebreaker);

                        standings = teamsTiedAfterROWTiebreaker.Count == 2 ?
                            ApplyH2HSeriesTiebreaker(standings,
                                                     indexes[TEAM1], teamsTiedAfterROWTiebreaker[TEAM1],
                                                     indexes[TEAM2], teamsTiedAfterROWTiebreaker[TEAM2]) :
                            ApplyGroupH2HStandingsTiebreaker(standings, indexesInTie, teamsTiedAfterROWTiebreaker);

                        teamsTiedAfterROWTiebreaker.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyH2HSeriesTiebreaker(List<Standings> standings, 
                                                         int index1, Standings team1, 
                                                         int index2, Standings team2)
        {
            int[] h2hSeries = GetH2HSeries(SEASON, team1, team2);
            
            if (h2hSeries[TEAM1] == h2hSeries[TEAM2])
                return ApplyH2HGoalsForTiebreaker(standings, index1, team1, index2, team2);
            else if (h2hSeries[TEAM1] < h2hSeries[TEAM2])
                return SwapTeams(standings, index1, team1, index2, team2);

            return standings;
        }

        private List<Standings> ApplyGroupH2HStandingsTiebreaker(List<Standings> standings, 
                                                                 List<int> indexes, 
                                                                 List<Standings> tiedTeams)
        {
            var stats = GetGroupH2HStats(standings, tiedTeams);
            var groupH2HStandingsTiebreaker = stats.OrderByDescending(s => s.Value.WinPct)
                                                   .ThenByDescending(s => s.Value.Wins)
                                                   .ThenBy(s => s.Value.Losses)
                                                   .ThenByDescending(s => s.Value.GoalDifferential)
                                                   .ThenByDescending(s => s.Value.GoalsFor)
                                                   .ThenBy(s => s.Value.GoalsAgainst)
                                                   .ToList();
            var teams = groupH2HStandingsTiebreaker
                .ToDictionary(s => s.Key)
                .Keys.ToList();

            standings = ReorderTeams(standings, indexes, teams);

            List<Standings> teamsTiedAfterGroupH2HStandingsTiebreaker = new List<Standings>();
            for (int index = 0; index < groupH2HStandingsTiebreaker.Count - 1; index++)
            {
                var currentTeam = groupH2HStandingsTiebreaker[index];
                var nextTeam = groupH2HStandingsTiebreaker[index + 1];

                if (currentTeam.Value.Wins == nextTeam.Value.Wins &&
                    currentTeam.Value.Losses == nextTeam.Value.Losses)
                {
                    if (teamsTiedAfterGroupH2HStandingsTiebreaker.IsNullOrEmpty())
                        teamsTiedAfterGroupH2HStandingsTiebreaker.Add(currentTeam.Key);

                    teamsTiedAfterGroupH2HStandingsTiebreaker.Add(nextTeam.Key);

                    if (index == groupH2HStandingsTiebreaker.Count - 2)
                    {
                        if (teamsTiedAfterGroupH2HStandingsTiebreaker.Count == tiedTeams.Count)
                            return standings;

                        indexes = GetIndexesInLeagueStandings(standings, teamsTiedAfterGroupH2HStandingsTiebreaker);

                        return teamsTiedAfterGroupH2HStandingsTiebreaker.Count == 2 ?
                            ApplyH2HSeriesTiebreaker(standings,
                                                     indexes[TEAM1], teamsTiedAfterGroupH2HStandingsTiebreaker[TEAM1],
                                                     indexes[TEAM2], teamsTiedAfterGroupH2HStandingsTiebreaker[TEAM2]) :
                            ApplyGroupH2HStandingsTiebreaker(standings, indexes, teamsTiedAfterGroupH2HStandingsTiebreaker);

                    }
                }
                else
                {
                    if (teamsTiedAfterGroupH2HStandingsTiebreaker.Any())
                    {
                        List<int> indexesInTie = GetIndexesInLeagueStandings(standings, 
                                                                             teamsTiedAfterGroupH2HStandingsTiebreaker);

                        standings = teamsTiedAfterGroupH2HStandingsTiebreaker.Count == 2 ?
                            ApplyH2HSeriesTiebreaker(standings,
                                                     indexes[TEAM1], teamsTiedAfterGroupH2HStandingsTiebreaker[TEAM1],
                                                     indexes[TEAM2], teamsTiedAfterGroupH2HStandingsTiebreaker[TEAM2]) :
                            ApplyGroupH2HStandingsTiebreaker(standings, indexes, teamsTiedAfterGroupH2HStandingsTiebreaker);

                        teamsTiedAfterGroupH2HStandingsTiebreaker.Clear();
                    }
                }
            }

            return standings;
        }

        private List<Standings> ApplyH2HGoalsForTiebreaker(List<Standings> standings, 
                                                           int index1, Standings team1, 
                                                           int index2, Standings team2,
                                                           List<Schedule>? h2hGamesPlayed = null)
        {
            h2hGamesPlayed ??= GetH2HGamesPlayed(SEASON, team1, team2).ToList();
            int[] h2hGoalsFor = GetH2HGoalsFor(SEASON, team1, team2, h2hGamesPlayed);

            if (h2hGoalsFor[TEAM1] < h2hGoalsFor[TEAM2])
                return SwapTeams(standings, index1, team1, index2, team2);

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

        private Standings GetDivisionLeader(List<Standings> standings, Division division)
        {
            var divisionStandings = standings.Where(s => s.Division == division);
            return divisionStandings.First();
        }

        private int GetDivisionRanking(List<Standings> standings, Standings team)
        {
            var divisionStandings = standings.Where(s => s.Division == team.Division);
            int ranking = 1;

            foreach (var _team in divisionStandings)
            {
                if (_team == team)
                    break;

                ranking++;
            }

            return ranking;
        }

        private List<int> GetDivisionRankings(List<Standings> standings, Standings team1, Standings team2)
        {
            List<Standings> teams = new List<Standings>() { team1, team2 };
            return GetDivisionRankings(standings, teams);
        }

        private List<int> GetDivisionRankings(List<Standings> standings, List<Standings> teams)
        {
            List<int> rankings = new List<int>();

            foreach (var team in teams)
            {
                int ranking = GetDivisionRanking(standings, team);
                rankings.Add(ranking);
            }

            return rankings;
        }

        private bool IsTeamDivisionLeader(List<Standings> standings, Standings team)
        {
            Standings leader = GetDivisionLeader(standings, team.Division!);
            return leader == team;
        }

        private Dictionary<Standings, bool> GetDivisionLeadingStatuses(List<Standings> standings, 
                                                                               List<Standings> tiedTeams)
        {
            Dictionary<Standings, bool> statuses = new Dictionary<Standings, bool>();

            foreach (var team in tiedTeams)
            {
                bool status = IsTeamDivisionLeader(standings, team);
                statuses.Add(team, status);
            }

            return statuses;
        }

        private int GetConferenceRanking(List<Standings> standings, Standings team)
        {
            var conferenceStandings = standings.Where(s => s.Conference == team.Conference);
            int ranking = 1;

            var leaders = GetLeaders(standings);
            foreach (var _team in leaders)
            {
                if (_team == team)
                    return ranking;

                ranking++;
            }

            var wildCards = GetWildCards(standings, leaders);
            foreach (var _team in wildCards)
            {
                if (_team == team)
                    break;

                ranking++;
            }

            return ranking;
        }

        private List<int> GetConferenceRankings(List<Standings> standings, Standings team1, Standings team2)
        {
            List<Standings> teams = new List<Standings> { team1, team2 };
            return GetConferenceRankings(standings, teams);
        }

        private List<int> GetConferenceRankings(List<Standings> standings, List<Standings> teams)
        {
            List<int> rankings = new List<int>();

            foreach (var team in teams)
            {
                int ranking = GetConferenceRanking(standings, team);
                rankings.Add(ranking);
            }

            return rankings;
        }

        private bool IsTeamInPlayoffSpot(List<Standings> standings, Standings team)
        {
            int ranking;
            
            if (SEASON <= 2022)
            {
                ranking = GetDivisionRanking(standings, team);
                return ranking <= 4;
            }

            ranking = GetConferenceRanking(standings, team);
            return ranking <= 8;
        }

        private List<KeyValuePair<Standings, bool>> GetPlayoffStatuses(List<Standings> standings, List<Standings> tiedTeams)
        {
            Dictionary<Standings, bool> statuses = new Dictionary<Standings, bool>();

            foreach (var team in tiedTeams)
            {
                bool status = IsTeamInPlayoffSpot(standings, team);
                statuses.Add(team, status);
            }

            return statuses
                .OrderByDescending(s => s.Value)
                .ToList();
        }

        private bool CanTiebreakerDetermineDivisionLeader(List<Standings> standings, Standings team1, Standings team2)
        {
            var rankings = GetDivisionRankings(standings, team1, team2);
            return rankings.Min() == 1;
        }

        private bool CanTiebreakerDetermineDivisionLeader(List<Standings> standings, List<Standings> tiedTeams)
        {
            var rankings = GetDivisionRankings(standings, tiedTeams);
            return rankings.Min() == 1;
        }

        private bool CanTiebreakerDeterminePlayoffTeams(List<Standings> standings, Standings team1, Standings team2)
        {
            List<Standings> teams = new List<Standings> { team1, team2 };
            return CanTiebreakerDeterminePlayoffTeams(standings, teams);
        }

        private bool CanTiebreakerDeterminePlayoffTeams(List<Standings> standings, List<Standings> tiedTeams)
        {
            List<int> rankings;

            if (SEASON <= 2022)
            {
                rankings = GetDivisionRankings(standings, tiedTeams);
                return rankings.Min() <= 4 && rankings.Max() > 4;
            }

            rankings = GetConferenceRankings(standings, tiedTeams);
            return rankings.Min() <= 8 && rankings.Max() > 8;
        }

        private Dictionary<Division, List<Standings>> GetTiedTeamsByDivision(List<Standings> tiedTeams)
        {
            var divisions = GetDivisions();
            Dictionary<Division, List<Standings>> tiedTeamsByDivision = new Dictionary<Division, List<Standings>>();

            foreach (var division in divisions)
            {
                var tiedTeamsInDivision = tiedTeams.Where(d => d.Division == division)
                                                   .ToList();

                if (tiedTeamsInDivision.Any())
                    tiedTeamsByDivision.Add(division, tiedTeamsInDivision);
            }

            return tiedTeamsByDivision;
        }

        private Dictionary<Conference, List<Standings>> GetTiedTeamsByConference(List<Standings> tiedTeams)
        {
            var conferences = GetConferences();
            Dictionary<Conference, List<Standings>> tiedTeamsByConference = new Dictionary<Conference, List<Standings>>();

            foreach (var conference in conferences)
            {
                var tiedTeamsInConference = tiedTeams.Where(c => c.Conference == conference)
                                                     .ToList();

                if (tiedTeamsInConference.Any())
                    tiedTeamsByConference.Add(conference, tiedTeamsInConference);
            }

            return tiedTeamsByConference;
        }

        private int GetSeason(List<Standings> standings)
        {
            return standings.First().Season.Year;
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

        private List<Team> ExtractTeams(List<Standings> teamList)
        {
            List<Team> teams = new List<Team>();

            foreach (var team in teamList)
                teams.Add(team.Team);

            return teams;
        }

        private List<Standings> ExtractStatLines(int season, List<KeyValuePair<Team, GroupH2HStats>> teamList)
        {
            List<Standings> teams = new List<Standings>();

            foreach (var team in teamList)
            {
                var statLine = GetTeamStatLine(season, team.Key);
                teams.Add(statLine);
            }

            return teams;
        }

        private IQueryable<Schedule> GetSchedule(int season)
        {
            return _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == REGULAR_SEASON)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);
        }

        private List<Alignment> GetAlignment(int? season = null)
        {
            season ??= SEASON;

            return _context.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Conference!.Name)
                .ThenBy(s => s.Division!.Name)
                .ToList();
        }

        private List<Conference> GetConferences(int? season = null, List<Alignment>? alignment = null)
        {
            season ??= SEASON;
            alignment ??= GetAlignment(season);

            return alignment
                .Select(a => a.Conference)
                .ToList()!;
        }

        private List<Division> GetDivisions(int? season = null, List<Alignment>? alignment = null)
        {
            season ??= SEASON;
            alignment ??= GetAlignment(season);

            return alignment
                .Select(a => a.Division)
                .ToList()!;
        }

        private IQueryable<Schedule> GetH2HGamesPlayed(int season, Standings team1, Standings team2)
        {
            var schedule = GetSchedule(season);
            
            return schedule.Where(s => s.IsFinalized && s.Period >= 3 &&
                                       ((s.AwayTeam == team1.Team && s.HomeTeam == team2.Team) ||
                                        (s.AwayTeam == team2.Team && s.HomeTeam == team1.Team)));
        }

        private List<Schedule> GetH2HGamesPlayed(int season, List<Standings> teams)
        {
            List<Schedule> games = new List<Schedule>();
            for (int primaryIndex = 0; primaryIndex < teams.Count - 1; primaryIndex++)
            {
                for (int secondaryIndex = primaryIndex + 1; secondaryIndex < teams.Count; secondaryIndex++)
                {
                    Standings currentTeam = teams[primaryIndex];
                    Standings nextTeam = teams[secondaryIndex];

                    var h2hGames = GetH2HGamesPlayed(season, currentTeam, nextTeam);

                    foreach (var game in h2hGames)
                        games.Add(game);
                }
            }

            return games;
        }

        private int[] GetH2HSeries(int season, Standings team1, Standings team2, List<Schedule>? h2hGamesPlayed = null)
        {
            h2hGamesPlayed ??= GetH2HGamesPlayed(season, team1, team2).ToList();

            int[] h2hSeries = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                int winningIndex;
                
                if (game.HomeTeam == team1.Team)
                    winningIndex = (game.HomeScore > game.AwayScore) ? TEAM1 : TEAM2;
                else
                    winningIndex = (game.AwayScore > game.HomeScore) ? TEAM1 : TEAM2;

                h2hSeries[winningIndex]++;
            }

            return h2hSeries;
        }

        private Dictionary<Standings, GroupH2HStats> GetGroupH2HStats(List<Standings> standings,
                                                                      List<Standings> tiedTeams,
                                                                      int? season = null)
        {
            season ??= SEASON;
            
            var groupH2HStats = CreateGroupH2HStatsDictionary(standings, tiedTeams);
            var gamesPlayed = GetH2HGamesPlayed((int)season, tiedTeams);

            foreach (var game in gamesPlayed)
            {
                bool homeTeamWon = game.HomeScore > game.AwayScore;
                
                Team winner = homeTeamWon ? game.HomeTeam : game.AwayTeam;
                Standings winnerStatLine = GetTeamStatLine((int)season, winner);

                Team loser = homeTeamWon ? game.AwayTeam : game.HomeTeam;
                Standings loserStatLine = GetTeamStatLine((int)season, loser);

                groupH2HStats[winnerStatLine].Wins++;
                groupH2HStats[winnerStatLine].GoalsFor += homeTeamWon ? game.HomeScore : game.AwayScore;
                groupH2HStats[winnerStatLine].GoalsAgainst += homeTeamWon ? game.AwayScore : game.HomeScore;

                groupH2HStats[loserStatLine].Losses++;
                groupH2HStats[loserStatLine].GoalsFor += homeTeamWon ? game.AwayScore : game.HomeScore;
                groupH2HStats[loserStatLine].GoalsAgainst += homeTeamWon ? game.HomeScore : game.AwayScore;
            }

            return groupH2HStats;
        }

        private Dictionary<Standings, GroupH2HStats> CreateGroupH2HStatsDictionary(List<Standings> standings,
                                                                                   List<Standings> tiedTeams)
        {
            Dictionary<Standings, GroupH2HStats> groupH2HStats = new Dictionary<Standings, GroupH2HStats>();

            foreach (var team in tiedTeams)
                groupH2HStats.Add(team, new GroupH2HStats());

            return groupH2HStats;
        }

        private int[] GetH2HGoalsFor(int season, Standings team1, Standings team2, List<Schedule>? h2hGamesPlayed = null)
        {
            h2hGamesPlayed ??= GetH2HGamesPlayed(season, team1, team2).ToList();
            int[] h2hGoalsFor = { 0, 0 };
            foreach (var game in h2hGamesPlayed)
            {
                bool team1IsHomeTeam = game.HomeTeam == team1.Team;
                h2hGoalsFor[TEAM1] += team1IsHomeTeam ? game.HomeScore : game.AwayScore;
                h2hGoalsFor[TEAM2] += team1IsHomeTeam ? game.AwayScore : game.HomeScore;
            }

            return h2hGoalsFor;
        }

        private int[] GetH2HGFInWins(int season, Standings team1, Standings team2)
        {
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
                    winningScore = homeTeamWon ? game.HomeScore! : game.AwayScore!;
                }
                else
                {
                    bool awayTeamWon = (game.AwayScore > game.HomeScore);
                    winningIndex = awayTeamWon ? TEAM1 : TEAM2;
                    winningScore = awayTeamWon ? game.AwayScore! : game.HomeScore!;
                }

                h2hGFInWins[winningIndex] += winningScore;
            }

            return h2hGFInWins;
        }

        private int[] GetH2HWinPoints(int season, Standings team1, Standings team2)
        {
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
            double[] h2hGAPerGame = { 0.00, 0.00 };
            h2hGAPerGame[TEAM1] = (team1.GamesPlayed > 0) ?
                (double)team1.GoalsAgainst / team1.GamesPlayed :
                0;
            h2hGAPerGame[TEAM2] = (team2.GamesPlayed > 0) ?
                (double)team2.GoalsAgainst / team2.GamesPlayed :
                0;

            return h2hGAPerGame;
        }

        private async Task<bool> GetStandingsUpdateStatus()
        {
            var updateAvailable = await _context.ProgramFlags
                .Where(f => f.Description == "New Standings Update Available")
                .Select(f => f.State)
                .FirstOrDefaultAsync();

            return updateAvailable;
        }

        private async Task StandingsUpdated()
        {
            var flag = await _context.ProgramFlags
                .Where(f => f.Description == "New Standings Update Available")
                .FirstOrDefaultAsync();

            flag!.State = false;

            _context.ProgramFlags.Update(flag);
            await _context.SaveChangesAsync();
        }
    }
}
