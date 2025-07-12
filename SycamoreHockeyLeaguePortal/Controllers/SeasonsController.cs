using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages;
using SycamoreHockeyLeaguePortal.Models.InputForms;
using SycamoreHockeyLeaguePortal.Services;
using System.Net;
using ZstdSharp.Unsafe;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeasonsController : Controller
    {
        private readonly ApplicationDbContext _localContext;

        public SeasonsController(ApplicationDbContext local)
        {
            _localContext = local;
        }

        // GET: Seasons
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var seasons = _localContext.Seasons.OrderByDescending(s => s.Year);

            Dictionary<int, DateTime?> firstDaysOfSeasons = new();
            Dictionary<int, bool> doSeasonsHavePlayoffSchedules = new();
            foreach (var season in seasons)
            {
                var schedule = _localContext.Schedule
                    .Where(s => s.Season.Year == season.Year)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);

                DateTime firstDay = !schedule.IsNullOrEmpty() ? schedule.FirstOrDefault()!.Date : DateTime.MinValue;
                bool hasPlayoffSchedule = schedule!.Any(s => s.Type == "Playoffs");

                firstDaysOfSeasons.Add(season.Year, firstDay);
                doSeasonsHavePlayoffSchedules.Add(season.Year, hasPlayoffSchedule);
            }
            ViewBag.FirstDaysOfSeasons = firstDaysOfSeasons;
            ViewBag.DoSeasonsHavePlayoffSchedules = doSeasonsHavePlayoffSchedules;

            return View(await seasons.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> GoLive(int year)
        {
            var season = _localContext.Seasons.FirstOrDefault(s => s.Year == year)!;
            var schedule = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == year)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);

            var seasonHasSchedule = schedule.Any();

            if (season.IsLive)
                throw new InvalidOperationException($"The {year} season is already live.");

            if (season.IsComplete)
                throw new InvalidOperationException($"The {year} season is already complete. It cannot go live again.");

            if (!seasonHasSchedule)
                throw new InvalidOperationException($"The {year} season doesn't have a schedule uploaded. It cannot go live yet.");

            season.InTestMode = false;
            season.IsLive = true;
            _localContext.Seasons.Update(season);
            await _localContext.SaveChangesAsync();

            return RedirectToAction("Index", "Schedule", new { weekOf = schedule.Min(s => s.Date).ToString("yyyy-MM-dd") });
        }

        // GET: Seasons/Create
        public async Task<IActionResult> Create()
        {
            List<Season> seasons = _localContext.Seasons
                .OrderBy(s => s.Year)
                .ToList();

            int previousYear = seasons.Max(s => s.Year);
            int year = previousYear + 1;
            int gamesPerTeam = seasons
                .Where(s => s.Year == previousYear)
                .Select(s => s.GamesPerTeam)
                .FirstOrDefault();

            var conferences = _localContext.Conferences
                .OrderBy(c => c.Name)
                .ToDictionary(c => c.Code);

            var previousTeams = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == previousYear)
                .Select(s => s.Team);

            var easternReplacements = _localContext.Teams
                .Where(t => t.Conference == conferences["EAST"] && !previousTeams.Contains(t))
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);
            ViewBag.EasternReplacements = new SelectList(easternReplacements, "Id", "FullName");

            var westernReplacements = _localContext.Teams
                .Where(t => t.Conference == conferences["WEST"] && !previousTeams.Contains(t))
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);
            ViewBag.WesternReplacements = new SelectList(westernReplacements, "Id", "FullName");

            Season_CreateForm form = new()
            {
                Year = year,
                GamesPerTeam = gamesPerTeam
            };

            return View(form);
        }

        // POST: Seasons/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Season_CreateForm form)
        {
            if (form.EasternReplacementId == null || form.WesternReplacementId == null)
                return RedirectToAction(nameof(Create));

            form.EasternReplacement = _localContext.Teams.FirstOrDefault(t => t.Id == form.EasternReplacementId);
            form.WesternReplacement = _localContext.Teams.FirstOrDefault(t => t.Id == form.WesternReplacementId);
            
            var previousYear = _localContext.Seasons.Max(s => s.Year);
            var conferences = _localContext.Conferences
                .OrderBy(c => c.Name)
                .ToDictionary(c => c.Code);

            if (form.Year <= previousYear || form.GamesPerTeam <= 0)
                return BadRequest();

            var season = new Season
            {
                Id = Guid.NewGuid(),
                Year = form.Year,
                GamesPerTeam = form.GamesPerTeam,
                InTestMode = true
            };
            _localContext.Seasons.Add(season);
            var package = new DTP_NewSeason(season);

            var teams = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == previousYear && s.ConferenceRanking < 12)
                .Select(s => s.Team)
                .ToList();
            teams.Add(form.EasternReplacement!);
            teams.Add(form.WesternReplacement!);

            teams = teams
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name)
                .ToList();

            foreach (var team in teams)
            {
                var alignment = new Alignment
                {
                    Id = Guid.NewGuid(),
                    SeasonId = season.Id,
                    Season = season,
                    ConferenceId = team.ConferenceId,
                    Conference = team.Conference,
                    TeamId = team.Id,
                    Team = team
                };
                var alignmentDTO = new DTO_Alignment
                {
                    Id = alignment.Id,
                    SeasonId = alignment.SeasonId,
                    ConferenceId = alignment.ConferenceId,
                    TeamId = alignment.TeamId
                };
                _localContext.Alignments.Add(alignment);
                package.Alignments.Add(alignmentDTO);

                var teamStats = new Standings
                {
                    Id = Guid.NewGuid(),
                    SeasonId = season.Id,
                    Season = season,
                    ConferenceId = team.ConferenceId,
                    Conference = team.Conference,
                    TeamId = team.Id,
                    Team = team
                };
                var teamStatsDTO = new DTO_Standings
                {
                    Id = teamStats.Id,
                    SeasonId = teamStats.SeasonId,
                    ConferenceId = teamStats.ConferenceId,
                    TeamId = teamStats.TeamId
                };
                _localContext.Standings.Add(teamStats);
                package.Standings.Add(teamStatsDTO);
            }

            var playoffRounds = _localContext.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == previousYear)
                .OrderBy(r => r.Index);

            var lastYearsMatchups = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == previousYear)
                .OrderBy(s => s.Index)
                .ToDictionary(s => s.Index);

            int ascii = 65;  // ASCII code for 'A'
            foreach (var round in playoffRounds)
            {
                var newRound = new PlayoffRound
                {
                    Id = Guid.NewGuid(),
                    SeasonId = season.Id,
                    Season = season,
                    Index = round.Index,
                    Name = round.Name,
                    MatchupsConfirmed = false
                };
                var newRoundDTO = new DTO_PlayoffRound
                {
                    Id = newRound.Id,
                    SeasonId = newRound.SeasonId,
                    Index = newRound.Index,
                    Name = newRound.Name,
                    MatchupsConfirmed = newRound.MatchupsConfirmed
                };
                _localContext.PlayoffRounds.Add(newRound);
                package.PlayoffRounds.Add(newRoundDTO);

                int numberOfMatchups = (int)Math.Pow(2, 4 - round.Index);
                for (int matchupIndex = 0; matchupIndex < numberOfMatchups; matchupIndex++)
                {
                    string index = ((char)ascii).ToString();

                    var previousMatchup = lastYearsMatchups[index];
                    
                    var matchup = new PlayoffSeries
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = season.Id,
                        Season = season,
                        RoundId = newRound.Id,
                        Round = newRound,
                        Index = index,
                        Description = previousMatchup.Description,
                        Team1Placeholder = previousMatchup.Team1Placeholder,
                        Team2Placeholder = previousMatchup.Team2Placeholder,
                        IsConfirmed = false,
                        HasEnded = false
                    };
                    var matchupDTO = new DTO_PlayoffSeries
                    {
                        Id = matchup.Id,
                        SeasonId = matchup.SeasonId,
                        RoundId = matchup.RoundId,
                        Index = matchup.Index,
                        Description = matchup.Description,
                        Team1Placeholder = matchup.Team1Placeholder,
                        Team2Placeholder = matchup.Team2Placeholder,
                        IsConfirmed = matchup.IsConfirmed,
                        HasEnded = matchup.HasEnded
                    };
                    _localContext.PlayoffSeries.Add(matchup);
                    package.PlayoffSeries.Add(matchupDTO);

                    ascii++;
                }
            }
            await _localContext.SaveChangesAsync();

            for (int index = 0; index < teams.Count - 1; index++)
            {
                Team team1 = teams[index];
                for (int cursor = index + 1; cursor < teams.Count; cursor++)
                {
                    Team team2 = teams[cursor];

                    var matchup = new HeadToHeadSeries
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = season.Id,
                        Season = season,
                        Team1Id = team1.Id,
                        Team1 = team1,
                        Team2Id = team2.Id,
                        Team2 = team2
                    };
                    var matchupDTO = new DTO_HeadToHeadSeries
                    {
                        Id = matchup.Id,
                        SeasonId = matchup.SeasonId,
                        Team1Id = matchup.Team1Id,
                        Team2Id = matchup.Team2Id
                    };
                    _localContext.HeadToHeadSeries.Add(matchup);
                    package.HeadToHeadSeries.Add(matchupDTO);
                }
            }

            await _localContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Seasons/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _localContext.Seasons == null)
            {
                return NotFound();
            }

            var season = await _localContext.Seasons
                .FirstOrDefaultAsync(m => m.Id == id);
            if (season == null)
            {
                return NotFound();
            }

            return View(season);
        }

        // POST: Seasons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_localContext.Seasons == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Season'  is null.");
            }
            var season = await _localContext.Seasons.FindAsync(id);
            if (season != null)
            {
                int year = season.Year;

                if (season.InTestMode)
                {
                    await RemoveScheduleAsync(year);
                    await RemoveStandingsAsync(year);
                    await RemoveAlignmentsAsync(year);
                    await RemovePlayoffSeriesAsync(year);
                    await RemovePlayoffRoundsAsync(year);
                    await RemoveChampionsRoundsAsync(year);
                    await RemoveChampionAsync(year);
                    _localContext.Seasons.Remove(season);
                    
                    await _localContext.SaveChangesAsync();
                }
                else
                    throw new InvalidOperationException($"The {year} season cannot be deleted.");
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool SeasonExists(Guid id)
        {
          return (_localContext.Seasons?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task RemoveScheduleAsync(int season)
        {
            var schedule = _localContext.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season);
            
            foreach (var game in schedule)
                _localContext.Schedule.Remove(game);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemoveStandingsAsync(int season)
        {
            var standings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);
            
            foreach (var statLine in standings)
                _localContext.Standings.Remove(statLine);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemoveAlignmentsAsync(int season)
        {
            var alignments = _localContext.Alignments
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            foreach (var alignment in alignments)
                _localContext.Alignments.Remove(alignment);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemovePlayoffSeriesAsync(int season)
        {
            var playoffSeries = _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season);

            foreach (var series in playoffSeries)
                _localContext.PlayoffSeries.Remove(series);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemovePlayoffRoundsAsync(int season)
        {
            var playoffRounds = _localContext.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season);

            foreach (var round in playoffRounds)
                _localContext.PlayoffRounds.Remove(round);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemoveChampionsRoundsAsync(int season)
        {
            var championsRounds = _localContext.ChampionsRounds
                .Include(r => r.Champion)
                .Include(r => r.Opponent)
                .Where(r => r.Champion.Season.Year == season);

            foreach (var round in championsRounds)
                _localContext.ChampionsRounds.Remove(round);

            await _localContext.SaveChangesAsync();
        }

        private async Task RemoveChampionAsync(int season)
        {
            var champion = _localContext.Champions
                .Include(c => c.Season)
                .Include(c => c.Team)
                .Where(c => c.Season.Year == season)
                .FirstOrDefault();

            if (champion != null)
                _localContext.Champions.Remove(champion);

            await _localContext.SaveChangesAsync();
        }

        private Guid GenerateGuid(IEnumerable<object> table)
        {
            Guid id;
            do
            {
                id = Guid.NewGuid();
            } while (table.Any(e => (Guid)e.GetType().GetProperty("Id")!.GetValue(e)! == id));
            
            return id;
        }
    }
}
