using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class SeasonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeasonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Seasons
        public async Task<IActionResult> Index()
        {
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);

            return View(await seasons.AsNoTracking().ToListAsync());
        }

        // GET: Seasons/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Season == null)
            {
                return NotFound();
            }

            var season = await _context.Season
                .FirstOrDefaultAsync(m => m.Id == id);
            if (season == null)
            {
                return NotFound();
            }

            return View(season);
        }

        // GET: Seasons/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Seasons/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Year,GamesPerTeam")] Season season)
        {
            if (ModelState.IsValid)
            {
                season.Id = Guid.NewGuid();
                _context.Add(season);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(season);
        }

        // GET: Seasons/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Season == null)
            {
                return NotFound();
            }

            var season = await _context.Season.FindAsync(id);
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
            if (id == null || _context.Season == null)
            {
                return NotFound();
            }

            var season = await _context.Season
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
            if (_context.Season == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Season'  is null.");
            }
            var season = await _context.Season.FindAsync(id);
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
                
                _context.Season.Remove(season);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SeasonExists(Guid id)
        {
          return (_context.Season?.Any(e => e.Id == id)).GetValueOrDefault();
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
            var alignments = _context.Alignment
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season);

            foreach (var alignment in alignments)
                _context.Alignment.Remove(alignment);

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
            var playoffRounds = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season);

            foreach (var round in playoffRounds)
                _context.PlayoffRound.Remove(round);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveChampionsRounds(int season)
        {
            var championsRounds = _context.ChampionsRound
                .Include(r => r.Champion)
                .Include(r => r.Opponent)
                .Where(r => r.Champion.Season.Year == season);

            foreach (var round in championsRounds)
                _context.ChampionsRound.Remove(round);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveChampion(int season)
        {
            var champion = _context.Champion
                .Include(c => c.Season)
                .Include(c => c.Team)
                .Where(c => c.Season.Year == season)
                .FirstOrDefault();

            if (champion != null)
                _context.Champion.Remove(champion);

            await _context.SaveChangesAsync();
        }
    }
}
