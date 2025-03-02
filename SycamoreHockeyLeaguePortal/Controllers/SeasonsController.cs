using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

            int year = seasons.Max(s => s.Year) + 1;
            int gamesPerTeam = seasons
                .Where(s => s.Year == year - 1)
                .Select(s => s.GamesPerTeam)
                .FirstOrDefault();

            var teams = _context.Teams
                .Where(t => t.IsActive)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name)
                .ToList();
            ViewBag.Teams = teams;

            var conferences = _context.Conferences
                .OrderBy(c => c.Name)
                .ToList();

            var divisions = _context.Divisions
                .Where(d => !(d.Code == "ED" || d.Code == "WD" || d.Code == "MT"))
                .OrderBy(d => d.Name);

            var easternDivisions = divisions.Where(d => d.Code == "AT" || d.Code == "NE" || d.Code == "SE").ToList();
            var westernDivisions = divisions.Where(d => d.Code == "CE" || d.Code == "NW" || d.Code == "PA").ToList();
            ViewBag.DivisionLists = new List<List<Division>> { easternDivisions, westernDivisions };

            var teamAlignments = new Dictionary<string, List<string>>();
            foreach (var division in divisions)
                teamAlignments.Add(division.Code, new List<string>());

            Season_CreateForm form = new Season_CreateForm
            {
                Year = year,
                GamesPerTeam = gamesPerTeam,
                Conferences = conferences,
                TeamAlignments = teamAlignments
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
            var previousYear = _context.Seasons.Max(s => s.Year);
            var conferences = _context.Conferences.OrderBy(c => c.Name);

            if (form.Year <= previousYear || form.GamesPerTeam <= 0)
                return BadRequest();

            var season = new Season
            {
                Id = GenerateGuid(_context.Seasons),
                Year = form.Year,
                GamesPerTeam = form.GamesPerTeam
            };
            _context.Seasons.Add(season);

            string[] easternDivisionCodes = { "AT", "NE", "SE" };
            string[] westernDivisionCodes = { "CE", "NW", "PA" };
            foreach (var _division in form.TeamAlignments)
            {
                string divisionCode = _division.Key.Replace("\"", "");
                string conferenceCode = easternDivisionCodes.Contains(divisionCode) ? "EAST" : "WEST";
                Conference conference = conferences.First(c => c.Code == conferenceCode);
                Division division = _context.Divisions.FirstOrDefault(d => d.Code == divisionCode)!;

                foreach (var teamCode in _division.Value)
                {
                    var team = _context.Teams.First(t => t.Code == teamCode);

                    var alignment = new Alignment
                    {
                        Id = GenerateGuid(_context.Alignments),
                        SeasonId = season.Id,
                        Season = season,
                        ConferenceId = conference.Id,
                        Conference = conference,
                        DivisionId = division.Id,
                        Division = division,
                        TeamId = team.Id,
                        Team = team
                    };
                    _context.Alignments.Add(alignment);

                    var teamStats = new Standings
                    {
                        Id = GenerateGuid(_context.Standings),
                        SeasonId = season.Id,
                        Season = season,
                        ConferenceId = conference.Id,
                        Conference = conference,
                        DivisionId = division.Id,
                        Division = division,
                        TeamId = team.Id,
                        Team = team
                    };
                    _context.Standings.Add(teamStats);
                }
            }

            var playoffRounds = _context.PlayoffRounds
                .Include(r => r.Season)
                .Where(r => r.Season.Year == previousYear)
                .OrderBy(r => r.Index);

            int ascii = 65;
            foreach (var round in playoffRounds)
            {
                var newRound = new PlayoffRound
                {
                    Id = GenerateGuid(_context.PlayoffRounds),
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
                    var matchup = new PlayoffSeries
                    {
                        Id = GenerateGuid(_context.PlayoffSeries),
                        SeasonId = season.Id,
                        Season = season,
                        RoundId = newRound.Id,
                        Round = newRound,
                        Index = ((char)ascii).ToString(),
                        IsConfirmed = false,
                        HasEnded = false
                    };
                    _context.PlayoffSeries.Add(matchup);

                    ascii++;
                }
            }

            await _context.SaveChangesAsync();


            var teams = _context.Alignments
                .Where(a => a.Season.Year == season.Year)
                .Select(a => a.Team)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name)
                .ToList();
            for (int index = 0; index < teams.Count - 1; index++)
            {
                Team currentTeam = teams[index];
                for (int nextIndex = index + 1; nextIndex < teams.Count; nextIndex++)
                {
                    Team nextTeam = teams[nextIndex];

                    var headToHead = new HeadToHeadSeries
                    {
                        Id = GenerateGuid(_context.HeadToHeadSeries),
                        SeasonId = season.Id,
                        Season = season,
                        Team1Id = currentTeam.Id,
                        Team1 = currentTeam,
                        Team2Id = nextTeam.Id,
                        Team2 = nextTeam
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
