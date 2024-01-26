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
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedule
        public async Task<IActionResult> Index(int season, string? team)
        {
            ViewBag.Season = season;
            ViewBag.Team = team;

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .OrderBy(a => a.Team.City)
                .ThenBy(a => a.Team.Name)
                .Select(a => a.Team);
            ViewBag.Teams = new SelectList(teams, "Code", "FullName");

            IOrderedQueryable<Schedule> schedule;
            if (team != null)
            {
                schedule = _context.Schedule
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Include(s => s.Season)
                    .Where(s => s.Season.Year == season &&
                                s.Type == "Regular Season" &&
                                (s.AwayTeam.Code == team ||
                                 s.HomeTeam.Code == team))
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }
            else
            {
                schedule = _context.Schedule
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Include(s => s.Season)
                .Where(s => s.Season.Year == season &&
                            s.Type == "Regular Season")
                .OrderBy(s => s.Date)
                .ThenBy(s => s.GameIndex);
            }

            return View(await schedule.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Playoffs(int season, int round, string? team)
        {
            ViewBag.Season = season;
            ViewBag.Team = team;
            ViewBag.Round = round;

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");

            var rounds = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season)
                .OrderBy(r => r.Index);
            ViewBag.Rounds = new SelectList(rounds, "Index", "Name");

            var teams = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.PlayoffRound.Index == round)
                .Select(s => s.HomeTeam)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Teams = new SelectList(teams, "Code", "FullName");

            IOrderedQueryable<Schedule> playoffs;
            if (team != null)
            {
                playoffs = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == "Playoffs" &&
                                s.PlayoffRound.Index == round &&
                                (s.AwayTeam.Code == team ||
                                 s.HomeTeam.Code == team))
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }
            else
            {
                playoffs = _context.Schedule
                    .Include(s => s.Season)
                    .Include(s => s.AwayTeam)
                    .Include(s => s.HomeTeam)
                    .Where(s => s.Season.Year == season &&
                                s.Type == "Playoffs" &&
                                s.PlayoffRound.Index == round)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.GameIndex);
            }

            return View(await playoffs.AsNoTracking().ToListAsync());
        }

        // GET: Schedule/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedule
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Include(s => s.Season)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // GET: Schedule/Create
        public IActionResult Create()
        {
            var teams = _context.Team
                .OrderBy(t => t.City)
                .ThenBy(t => t.Name);

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            
            ViewData["AwayTeamId"] = new SelectList(teams, "Id", "FullName");
            ViewData["HomeTeamId"] = new SelectList(teams, "Id", "FullName");
            ViewData["SeasonId"] = new SelectList(seasons, "Id", "Year");
            return View();
        }

        // POST: Schedule/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,Notes")] Schedule game)
        {
            //if (ModelState.IsValid)
            //{
            game.Id = Guid.NewGuid();
            game.GameIndex = (_context.Schedule.Any()) ?
                _context.Schedule
                    .Select(s => s.GameIndex)
                    .Max() + 1 :
                1;
            game.Season = _context.Season.FirstOrDefault(s => s.Id == game.SeasonId);
            game.AwayTeam = _context.Team.FirstOrDefault(t => t.Id == game.AwayTeamId);
            game.HomeTeam = _context.Team.FirstOrDefault(t => t.Id == game.HomeTeamId);

            _context.Add(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { season = game.Season.Year });
            //}
            /*ViewData["AwayTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.AwayTeam.FullName);
            ViewData["HomeTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.HomeTeam.FullName);
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", schedule.SeasonId);
            return View(schedule);*/
        }

        // GET: Schedule/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Id == id)
                .FirstOrDefault();

            if (schedule == null)
            {
                return NotFound();
            }
            ViewData["AwayTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.AwayTeam.FullName);
            ViewData["HomeTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.HomeTeam.FullName);
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", schedule.Season.Year);
            return View(schedule);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,Date,Type,AwayTeamId,AwayScore,HomeTeamId,HomeScore,Period,IsLive,Notes")] Schedule game)
        {
            if (id != game.Id)
            {
                return NotFound();
            }

            game.Season = _context.Season.FirstOrDefault(s => s.Id == game.SeasonId);
            game.AwayTeam = _context.Team.FirstOrDefault(t => t.Id == game.AwayTeamId);
            game.HomeTeam = _context.Team.FirstOrDefault(t => t.Id == game.HomeTeamId);

            //if (ModelState.IsValid)
            //{
            //    try
            //    {
            _context.Update(game);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { season = game.Season.Year });
            //    }
            //    catch (DbUpdateConcurrencyException)
            /*    {
                    if (!ScheduleExists(schedule.Id))
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
            ViewData["AwayTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.AwayTeam.FullName);
            ViewData["HomeTeamId"] = new SelectList(_context.Team, "Id", "FullName", schedule.HomeTeam.FullName);
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Year", schedule.Season.Year);
            return View(schedule);*/
        }

        // GET: Schedule/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedule
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Include(s => s.Season)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Schedule == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Schedule'  is null.");
            }
            
            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefault();

            if (schedule != null)
            {
                _context.Schedule.Remove(schedule);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { season = schedule.Season.Year });
        }

        private bool ScheduleExists(Guid id)
        {
          return (_context.Schedule?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<IActionResult> StartGame(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.StartGame();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> EndGame(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.EndGame();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> NextPeriod(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.NextPeriod();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> PreviousPeriod(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.PreviousPeriod();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> AwayGoal(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.AwayGoal();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> RemoveAwayGoal(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.RemoveAwayGoal();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> HomeGoal(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.HomeGoal();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }

        private async Task<IActionResult> RemoveHomeGoal(Guid gameId)
        {
            var game = _context.Schedule
                .Where(s => s.Id == gameId)
                .FirstOrDefault();

            game.RemoveHomeGoal();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = gameId });
        }
    }
}
