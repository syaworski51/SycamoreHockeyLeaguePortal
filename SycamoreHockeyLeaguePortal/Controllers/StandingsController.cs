using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class StandingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        private const string VIEWBY_DIVISION = "division";
        private const string VIEWBY_CONFERENCE = "conference";
        private const string VIEWBY_PLAYOFFS = "playoffs";
        private const string VIEWBY_LEAGUE = "league";

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

            List<Standings> standings = await GetStandings(season, viewBy);

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

            var standings = await GetStandings(season, VIEWBY_PLAYOFFS);

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

        private async Task<List<Standings>> GetStandings(int season, string viewBy)
        {
            IQueryable<Standings> standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            if (standings.Count(s => s.GamesPlayed == 0) == standings.Count())
                standings = standings
                    .OrderBy(s => s.Team.City)
                    .ThenBy(s => s.Team.Name);
            else
                standings = viewBy switch
                {
                    VIEWBY_DIVISION => standings.OrderBy(s => s.DivisionRanking),
                    VIEWBY_CONFERENCE => standings.OrderBy(s => s.ConferenceRanking),
                    VIEWBY_PLAYOFFS => standings.OrderBy(s => s.PlayoffRanking),
                    _ => standings.OrderBy(s => s.LeagueRanking),
                };
            
            return standings.ToList();
        }

        private async Task<List<Standings>> UpdateRankingsAsync(List<Standings> standings)
        {
            int ranking = 1;
            foreach (var team in standings)
            {
                team.LeagueRanking = ranking;
                team.DivisionRanking = GetDivisionRanking(standings, team);
                team.ConferenceRanking = GetConferenceRanking(standings, team);
                team.PlayoffRanking = GetPlayoffRanking(standings, team);
                _context.Standings.Update(team);

                ranking++;
            }

            await _context.SaveChangesAsync();
            return standings;
        }

        private Standings GetDivisionLeader(List<Standings> standings, Division division)
        {
            var divisionStandings = standings.Where(s => s.Division == division);
            return divisionStandings.First();
        }

        private int GetDivisionRanking(List<Standings> standings, Standings team)
        {
            standings = standings.Where(s => s.Division == team.Division).ToList();
            return standings.IndexOf(team) + 1;
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

        private Dictionary<Standings, bool> GetDivisionLeadingStatuses(List<Standings> standings, List<Standings> tiedTeams)
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
            standings = standings.Where(s => s.Conference == team.Conference).ToList();
            return standings.IndexOf(team) + 1;
        }

        private List<int> GetConferenceRankings(List<Standings> standings, Standings team1, Standings team2)
        {
            List<Standings> teams = new List<Standings> { team1, team2 };
            return GetConferenceRankings(standings, teams);
        }

        private List<int> GetConferenceRankings(List<Standings> standings, List<Standings> teams)
        {
            List<int> rankings = new();

            foreach (var team in teams)
            {
                int ranking = GetConferenceRanking(standings, team);
                rankings.Add(ranking);
            }

            return rankings;
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

        private List<int> GetPlayoffRankings(List<Standings> standings, Standings team1, Standings team2)
        {
            List<Standings> teams = new List<Standings> { team1, team2 };
            return GetPlayoffRankings(standings, teams);
        }

        private List<int> GetPlayoffRankings(List<Standings> standings, List<Standings> teams)
        {
            List<int> rankings = new List<int>();

            foreach (var team in teams)
            {
                int ranking = GetPlayoffRanking(standings, team);
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

            ranking = GetPlayoffRanking(standings, team);
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

            rankings = GetPlayoffRankings(standings, tiedTeams);
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
