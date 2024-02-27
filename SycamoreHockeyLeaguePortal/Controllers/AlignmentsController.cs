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
    public class AlignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Alignments
        public async Task<IActionResult> Index(int season)
        {
            ViewBag.Season = season;
            
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");
            
            var alignments = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Conference.Name)
                .ThenBy(a => a.Division.Name)
                .ThenBy(a => a.Team.City)
                .ThenBy(a => a.Team.Name);

            return View(await alignments.ToListAsync());
        }

        // GET: Alignments/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Alignment == null)
            {
                return NotFound();
            }

            var alignment = await _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alignment == null)
            {
                return NotFound();
            }

            return View(alignment);
        }

        // GET: Alignments/Create
        public IActionResult Create()
        {
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);

            var conferences = _context.Conference
                .OrderBy(c => c.Name);

            var divisions = _context.Division
                .OrderBy(d => d.Name);

            var teams = _context.Team
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year");
            ViewData["ConferenceId"] = new SelectList(conferences, "Id", "Name");
            ViewData["DivisionId"] = new SelectList(divisions, "Id", "Name");
            ViewData["TeamId"] = new SelectList(teams, "Id", "FullName");
            return View();
        }

        // POST: Alignments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,ConferenceId,DivisionId,TeamId")] Alignment alignment)
        {
            //if (ModelState.IsValid)
            //{
            alignment.Id = Guid.NewGuid();
            alignment.Season = _context.Season.FirstOrDefault(s => s.Id == alignment.SeasonId);
            alignment.Conference = _context.Conference.FirstOrDefault(c => c.Id == alignment.ConferenceId);
            alignment.Division = _context.Division.FirstOrDefault(d => d.Id == alignment.DivisionId);
            alignment.Team = _context.Team.FirstOrDefault(t => t.Id == alignment.TeamId);

            _context.Add(alignment);
            await _context.SaveChangesAsync();

            var teamStats = new Standings
            {
                Id = Guid.NewGuid(),
                SeasonId = alignment.SeasonId,
                Season = alignment.Season,
                ConferenceId = alignment.ConferenceId,
                Conference = alignment.Conference,
                DivisionId = alignment.DivisionId,
                Division = alignment.Division,
                TeamId = alignment.TeamId,
                Team = alignment.Team
            };
            _context.Standings.Add(teamStats);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { season = alignment.Season.Year });
            //}
            /*ViewData["ConferenceId"] = new SelectList(_context.Conference, "Id", "Id", alignment.ConferenceId);
            ViewData["DivisionId"] = new SelectList(_context.Division, "Id", "Id", alignment.DivisionId);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id", alignment.TeamId);
            return View(alignment);*/
        }

        // GET: Alignments/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Alignment == null)
            {
                return NotFound();
            }

            var alignment = await _context.Alignment.FindAsync(id);
            if (alignment == null)
            {
                return NotFound();
            }

            var seasons = _context.Season
                .OrderBy(s => s.Year);

            var conferences = _context.Conference
                .OrderBy(c => c.Name);

            var divisions = _context.Division
                .OrderBy(d => d.Name);

            var teams = _context.Team
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year", alignment.Season.Year);
            ViewData["ConferenceId"] = new SelectList(conferences, "Id", "Name", alignment.Conference.Name);
            ViewData["DivisionId"] = new SelectList(divisions, "Id", "Name", alignment.Division.Name);
            ViewData["TeamId"] = new SelectList(teams, "Id", "FullName", alignment.Team.FullName);
            return View(alignment);
        }

        // POST: Alignments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,ConferenceId,DivisionId,TeamId")] Alignment alignment)
        {
            if (id != alignment.Id)
            {
                return NotFound();
            }

            /*if (ModelState.IsValid)
            {
                try
                {*/
            alignment.Season = _context.Season.FirstOrDefault(s => s.Id == alignment.SeasonId);
            alignment.Conference = _context.Conference.FirstOrDefault(c => c.Id == alignment.ConferenceId);
            alignment.Division = _context.Division.FirstOrDefault(d => d.Id == alignment.DivisionId);
            alignment.Team = _context.Team.FirstOrDefault(t => t.Id == alignment.TeamId);

            _context.Update(alignment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { season = alignment.Season.Year });
            /*    }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlignmentExists(alignment.Id))
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
            ViewData["ConferenceId"] = new SelectList(_context.Conference, "Id", "Id", alignment.ConferenceId);
            ViewData["DivisionId"] = new SelectList(_context.Division, "Id", "Id", alignment.DivisionId);
            ViewData["TeamId"] = new SelectList(_context.Team, "Id", "Id", alignment.TeamId);
            return View(alignment);*/
        }

        // GET: Alignments/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Alignment == null)
            {
                return NotFound();
            }

            var alignment = await _context.Alignment
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alignment == null)
            {
                return NotFound();
            }

            return View(alignment);
        }

        // POST: Alignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Alignment == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Alignment'  is null.");
            }
            var alignment = await _context.Alignment.FindAsync(id);
            if (alignment != null)
            {
                _context.Alignment.Remove(alignment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AlignmentExists(Guid id)
        {
          return (_context.Alignment?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
