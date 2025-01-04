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
    public class PlayoffRoundsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayoffRoundsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlayoffRounds
        public async Task<IActionResult> Index(int season)
        {
            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");
            
            var playoffRounds = _context.PlayoffRounds
                .Include(p => p.Season)
                .Where(p => p.Season.Year == season)
                .OrderBy(p => p.Index);
            
            return View(await playoffRounds.ToListAsync());
        }

        // GET: PlayoffRounds/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.PlayoffRounds == null)
            {
                return NotFound();
            }

            var playoffRound = await _context.PlayoffRounds
                .Include(p => p.Season)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playoffRound == null)
            {
                return NotFound();
            }

            return View(playoffRound);
        }

        // GET: PlayoffRounds/Create
        public IActionResult Create()
        {
            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);

            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year");
            return View();
        }

        // POST: PlayoffRounds/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,Index,Name")] PlayoffRound playoffRound)
        {
            playoffRound.Id = Guid.NewGuid();
            playoffRound.Season = _context.Seasons.FirstOrDefault(s => s.Id == playoffRound.SeasonId);
            
            _context.Add(playoffRound);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // GET: PlayoffRounds/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.PlayoffRounds == null)
            {
                return NotFound();
            }

            var playoffRound = await _context.PlayoffRounds.FindAsync(id);
            if (playoffRound == null)
            {
                return NotFound();
            }
            ViewData["SeasonId"] = new SelectList(_context.Seasons, "Id", "Id", playoffRound.SeasonId);
            return View(playoffRound);
        }

        // POST: PlayoffRounds/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,Index,Name")] PlayoffRound playoffRound)
        {
            playoffRound.Season = _context.Seasons.FirstOrDefault(s => s.Id == playoffRound.SeasonId);

            _context.Update(playoffRound);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // GET: PlayoffRounds/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.PlayoffRounds == null)
            {
                return NotFound();
            }

            var playoffRound = await _context.PlayoffRounds
                .Include(p => p.Season)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playoffRound == null)
            {
                return NotFound();
            }

            return View(playoffRound);
        }

        // POST: PlayoffRounds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.PlayoffRounds == null)
            {
                return Problem("Entity set 'ApplicationDbContext.PlayoffRound'  is null.");
            }
            var playoffRound = await _context.PlayoffRounds.FindAsync(id);
            if (playoffRound != null)
            {
                _context.PlayoffRounds.Remove(playoffRound);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlayoffRoundExists(Guid id)
        {
          return (_context.PlayoffRounds?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
