using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IQueryable<Game> schedule;

        public TeamsController(ApplicationDbContext context)
        {
            _context = context;
            schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam);
        }

        // GET: Teams
        public async Task<IActionResult> Index()
        {
            var teams = _context.Teams
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            List<int> mostRecentSeasons = new List<int>();
            foreach (var team in teams)
            {
                try
                {
                    var mostRecentSeason = _context.Standings
                        .Include(s => s.Season)
                        .Include(s => s.Conference)
                        .Include(s => s.Division)
                        .Include(s => s.Team)
                        .Where(s => s.TeamId == team.Id)
                        .Select(s => s.Season.Year)
                        .Max();

                    mostRecentSeasons.Add(mostRecentSeason);
                }
                catch (InvalidOperationException ex)
                {
                    mostRecentSeasons.Add(0);
                }
            }
            ViewBag.MostRecentSeasons = mostRecentSeasons;

            return View(await teams.AsNoTracking().ToListAsync());
        }

        // GET: Teams/Details/5
        [Route("Teams/{id}")]
        public async Task<IActionResult> Details(string? id, int season, string gameType, string? opponent)
        {
            if (id == null || _context.Teams == null)
            {
                return NotFound();
            }

            var team = await _context.Teams.FirstOrDefaultAsync(m => m.Code == id);
            ViewBag.Team = team;

            if (team == null)
            {
                return NotFound();
            }

            var seasons = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Team.Code == id)
                .Select(s => s.Season)
                .OrderByDescending(s => s.Year);
            ViewData["Seasons"] = new SelectList(seasons, "Year", "Year");
            ViewBag.Season = season;

            var teamStats = _context.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season &&
                            s.TeamId == team.Id)
                .FirstOrDefault()!;
            ViewBag.TeamStats = teamStats;

            switch (gameType)
            {
                case "regular-season":
                    gameType = "Regular Season";
                    break;

                case "tiebreaker":
                    gameType = "Tiebreaker";
                    break;

                case "playoffs":
                    gameType = "Playoffs";
                    break;
            }
            ViewBag.GameType = gameType;


            var opponents = _context.Alignments
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season &&
                            a.Team.Code != id)
                .Select(a => a.Team)
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);
            ViewBag.Opponents = new SelectList(opponents, "Code", "FullName");

            schedule = schedule
                .Where(s => s.Season.Year == season &&
                            (s.AwayTeam.Code == id ||
                             s.HomeTeam.Code == id))
                .OrderBy(s => s.Date);

            var teamGameTypes = schedule
                .Select(s => s.Type)
                .Distinct();

            var gameTypes = _context.GameTypes
                .Where(t => teamGameTypes.Contains(t.Name))
                .OrderBy(t => t.Index);
            ViewData["GameTypes"] = new SelectList(gameTypes, "ParameterValue", "Name");

            schedule = schedule.Where(s => s.Type == gameType);

            if (opponent != null)
            {
                schedule = schedule
                    .Where(s => s.AwayTeam.Code == opponent ||
                                s.HomeTeam.Code == opponent);
            }
            ViewBag.Opponent = opponent;

            ViewData["Schedule"] = schedule;

            List<HeadToHeadSeries> h2hResults = _context.HeadToHeadSeries
                .Include(r => r.Season)
                .Include(r => r.Team1)
                .Include(r => r.Team2)
                .Where(r => r.Season.Year == season &&
                            (r.Team1 == team || r.Team2 == team) &&
                            (r.Team1Wins + r.Team1OTWins + r.Team2OTWins + r.Team2Wins) > 0)
                .OrderBy(r => r.Team1.City)
                .ThenBy(r => r.Team1.Name)
                .ThenBy(r => r.Team2.City)
                .ThenBy(r => r.Team2.Name)
                .ToList();            
            ViewBag.H2HResults = h2hResults;

            return View(team);
        }

        // GET: Teams/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teams/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,City,Name,AlternateName,LogoPath")] Team team)
        {
            if (ModelState.IsValid)
            {
                team.Id = Guid.NewGuid();
                _context.Add(team);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(team);
        }

        // GET: Teams/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Teams == null)
            {
                return NotFound();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }
            return View(team);
        }

        // POST: Teams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Code,City,Name,AlternateName,LogoPath")] Team team)
        {
            if (id != team.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(team);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeamExists(team.Id))
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
            return View(team);
        }

        // GET: Teams/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Teams == null)
            {
                return NotFound();
            }

            var team = await _context.Teams
                .FirstOrDefaultAsync(m => m.Id == id);
            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        // POST: Teams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Teams == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Team'  is null.");
            }
            var team = await _context.Teams.FindAsync(id);
            if (team != null)
            {
                _context.Teams.Remove(team);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeamExists(Guid id)
        {
          return (_context.Teams?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
