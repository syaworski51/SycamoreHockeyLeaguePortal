using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class ChampionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChampionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Champions
        public async Task<IActionResult> Index()
        {
            var champions = _context.Champion
                .Include(c => c.Season)
                .Include(c => c.Team)
                .OrderByDescending(c => c.Season.Year);

            List<List<ChampionsRound>> rounds = new List<List<ChampionsRound>>();
            foreach (var champion in champions)
            {
                var championsRounds = _context.ChampionsRound
                    .Include(r => r.Champion)
                    .Include(r => r.Opponent)
                    .Where(r => r.Champion.Season.Year == champion.Season.Year)
                    .OrderByDescending(r => r.RoundIndex);

                rounds.Add(championsRounds.ToList());
            }
            ViewBag.Rounds = rounds;
            
            return View(await champions.ToListAsync());
        }

        // GET: Champions/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Champion == null)
            {
                return NotFound();
            }

            var champion = await _context.Champion
                .Include(c => c.Season)
                .Include(c => c.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (champion == null)
            {
                return NotFound();
            }

            return View(champion);
        }

        // GET: Champions/Create
        public IActionResult Create()
        {
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);

            var teams = _context.Team
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year");
            ViewData["TeamId"] = new SelectList(teams, "Id", "FullName");
            return View();
        }

        // POST: Champions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,TeamId")] Champion champion)
        {
            champion.Id = Guid.NewGuid();
            champion.Season = _context.Season.FirstOrDefault(s => s.Id == champion.SeasonId);
            champion.Team = _context.Team.FirstOrDefault(t => t.Id == champion.TeamId);

            var rounds = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == champion.Season.Year &&
                            (champion.TeamId == s.Team1Id ||
                             champion.TeamId == s.Team2Id))
                .OrderBy(s => s.Round.Index);
            
            _context.Add(champion);

            foreach (var round in rounds)
            {
                var opponentId = round.Team2Id;
                if (round.Team2Id == champion.TeamId)
                    opponentId = round.Team1Id;

                var championsRound = new ChampionsRound
                {
                    Id = Guid.NewGuid(),
                    ChampionId = champion.Id,
                    Champion = champion,
                    RoundIndex = round.Round.Index,
                    OpponentId = opponentId,
                    Opponent = _context.Team.FirstOrDefault(t => t.Id == opponentId),
                    SeriesLength = round.Team1Wins + round.Team2Wins,
                    BestOf = 7
                };

                _context.ChampionsRound.Add(championsRound);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Champions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Champion == null)
            {
                return NotFound();
            }

            var champion = await _context.Champion.FindAsync(id);
            if (champion == null)
            {
                return NotFound();
            }
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id", champion.SeasonId);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id", champion.TeamId);
            return View(champion);
        }

        // POST: Champions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,TeamId")] Champion champion)
        {
            if (id != champion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(champion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChampionExists(champion.Id))
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
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id", champion.SeasonId);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id", champion.TeamId);
            return View(champion);
        }

        // GET: Champions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Champion == null)
            {
                return NotFound();
            }

            var champion = await _context.Champion
                .Include(c => c.Season)
                .Include(c => c.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (champion == null)
            {
                return NotFound();
            }

            return View(champion);
        }

        // POST: Champions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Champion == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Champion'  is null.");
            }
            var champion = await _context.Champion.FindAsync(id);
            if (champion != null)
            {
                _context.Champion.Remove(champion);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChampionExists(Guid id)
        {
          return (_context.Champion?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
