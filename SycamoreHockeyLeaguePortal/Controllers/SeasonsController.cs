using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.InputForms;
using System.Net;
using ZstdSharp.Unsafe;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeasonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeasonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Seasons
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var seasons = _context.Seasons.OrderByDescending(s => s.Year);

            Dictionary<int, DateTime?> firstDaysOfSeasons = new();
            foreach (var season in seasons)
            {
                DateTime? firstDay = _context.Schedule
                    .Where(s => s.Season.Year == season.Year)
                    .OrderBy(s => s.Date.Date)
                    .Select(s => s.Date)
                    .FirstOrDefault();

                firstDaysOfSeasons.Add(season.Year, firstDay);
            }
            ViewBag.FirstDaysOfSeasons = firstDaysOfSeasons;

            return View(await seasons.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> GoLive(int year)
        {
            var season = _context.Seasons.FirstOrDefault(s => s.Year == year)!;
            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == year)
                .OrderBy(s => s.Date.Date)
                .ThenBy(s => s.GameIndex);

            var seasonHasSchedule = schedule.Any();

            if (season.IsLive)
                throw new Exception($"The {year} season is already live.");

            if (season.IsComplete)
                throw new Exception($"The {year} season is already complete. It cannot go live again.");

            if (!seasonHasSchedule)
                throw new Exception($"The {year} season doesn't have a schedule uploaded. It cannot go live yet.");

            season.InTestMode = false;
            season.IsLive = true;
            _context.Seasons.Update(season);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Schedule", new { weekOf = schedule.Min(s => s.Date).ToShortDateString() });
        }

        // GET: Seasons/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Seasons == null)
            {
                return NotFound();
            }

            var season = await _context.Seasons
                .FirstOrDefaultAsync(m => m.Id == id);
            if (season == null)
            {
                return NotFound();
            }

            return View(season);
        }

        // GET: Seasons/Create
        public async Task<IActionResult> Create()
        {
            List<Season> seasons = _context.Seasons
                .OrderBy(s => s.Year)
                .ToList();

            int previousYear = seasons.Max(s => s.Year);
            int year = previousYear + 1;
            int gamesPerTeam = seasons
                .Where(s => s.Year == previousYear)
                .Select(s => s.GamesPerTeam)
                .FirstOrDefault();

            var conferences = _context.Conferences
                .OrderBy(c => c.Name)
                .ToDictionary(c => c.Code);

            var previousTeams = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == previousYear)
                .Select(s => s.Team);

            var easternReplacements = _context.Teams
                .Where(t => t.Conference == conferences["EAST"] && !previousTeams.Contains(t))
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);
            ViewBag.EasternReplacements = new SelectList(easternReplacements, "Id", "FullName");

            var westernReplacements = _context.Teams
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

            form.EasternReplacement = _context.Teams.FirstOrDefault(t => t.Id == form.EasternReplacementId);
            form.WesternReplacement = _context.Teams.FirstOrDefault(t => t.Id == form.WesternReplacementId);
            
            var previousYear = _context.Seasons.Max(s => s.Year);
            var conferences = _context.Conferences
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
            _context.Seasons.Add(season);

            var teams = _context.Standings
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
                _context.Alignments.Add(alignment);

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
                _context.Standings.Add(teamStats);
            }

            var playoffRounds = _context.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == previousYear)
                .OrderBy(r => r.Index);

            var lastYearsMatchups = _context.PlayoffSeries
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
                _context.PlayoffRounds.Add(newRound);

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
                    _context.PlayoffSeries.Add(matchup);

                    ascii++;
                }
            }
            await _context.SaveChangesAsync();

            for (int index = 0; index < teams.Count - 1; index++)
            {
                Team team1 = teams[index];
                for (int cursor = index + 1; cursor < teams.Count; cursor++)
                {
                    Team team2 = teams[cursor];

                    var headToHead = new HeadToHeadSeries
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = season.Id,
                        Season = season,
                        Team1Id = team1.Id,
                        Team1 = team1,
                        Team2Id = team2.Id,
                        Team2 = team2
                    };
                    _context.HeadToHeadSeries.Add(headToHead);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Seasons/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Seasons == null)
            {
                return NotFound();
            }

            var season = await _context.Seasons.FindAsync(id);
            if (season == null)
            {
                return NotFound();
            }
            return View(season);
        }

        // POST: Seasons/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Year,GamesPerTeam")] Season season)
        {
            if (id != season.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(season);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SeasonExists(season.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(season);
        }

        // GET: Seasons/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Seasons == null)
            {
                return NotFound();
            }

            var season = await _context.Seasons
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
            if (_context.Seasons == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Season'  is null.");
            }
            var season = await _context.Seasons.FindAsync(id);
            if (season != null)
            {
                var year = season.Year;
                
                await RemoveSchedule(year);
                await RemoveStandings(year);
                await RemoveAlignments(year);
                await RemovePlayoffSeries(year);
                await RemovePlayoffRounds(year);
                await RemoveChampionsRounds(year);
                await RemoveChampion(year);
                
                _context.Seasons.Remove(season);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SeasonExists(Guid id)
        {
          return (_context.Seasons?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task RemoveSchedule(int season)
        {
            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season);
            
            foreach (var game in schedule)
                _context.Schedule.Remove(game);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveStandings(int season)
        {
            var standings = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);
            
            foreach (var statLine in standings)
                _context.Standings.Remove(statLine);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveAlignments(int season)
        {
            var alignments = _context.Alignments
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            foreach (var alignment in alignments)
                _context.Alignments.Remove(alignment);

            await _context.SaveChangesAsync();
        }

        private async Task RemovePlayoffSeries(int season)
        {
            var playoffSeries = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season);

            foreach (var series in playoffSeries)
                _context.PlayoffSeries.Remove(series);

            await _context.SaveChangesAsync();
        }

        private async Task RemovePlayoffRounds(int season)
        {
            var playoffRounds = _context.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season);

            foreach (var round in playoffRounds)
                _context.PlayoffRounds.Remove(round);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveChampionsRounds(int season)
        {
            var championsRounds = _context.ChampionsRounds
                .Include(r => r.Champion)
                .Include(r => r.Opponent)
                .Where(r => r.Champion.Season.Year == season);

            foreach (var round in championsRounds)
                _context.ChampionsRounds.Remove(round);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveChampion(int season)
        {
            var champion = _context.Champions
                .Include(c => c.Season)
                .Include(c => c.Team)
                .Where(c => c.Season.Year == season)
                .FirstOrDefault();

            if (champion != null)
                _context.Champions.Remove(champion);

            await _context.SaveChangesAsync();
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
