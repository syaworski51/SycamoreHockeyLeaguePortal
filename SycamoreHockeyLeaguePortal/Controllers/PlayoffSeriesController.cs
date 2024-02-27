using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.ProjectModel;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class PlayoffSeriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayoffSeriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlayoffSeries
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PlayoffSeries
                .Include(p => p.Round)
                .Include(p => p.Season)
                .Include(p => p.Team1)
                .Include(p => p.Team2);

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PlayoffSeries/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.PlayoffSeries == null)
            {
                return NotFound();
            }

            var playoffSeries = await _context.PlayoffSeries
                .Include(p => p.Round)
                .Include(p => p.Season)
                .Include(p => p.Team1)
                .Include(p => p.Team2)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playoffSeries == null)
            {
                return NotFound();
            }

            return View(playoffSeries);
        }

        // GET: PlayoffSeries/Create
        public IActionResult Create(int season)
        {
            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Id", "Year");

            var rounds = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season)
                .OrderBy(r => r.Index);
            ViewBag.Rounds = new SelectList(rounds, "Id", "Name");

            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season)
                .Select(a => a.Team)
                .OrderBy(a => a.City)
                .ThenBy(a => a.Name);
            ViewBag.Teams = new SelectList(teams, "Id", "FullName");

            return View();
        }

        // POST: PlayoffSeries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeasonId,RoundId,StartDate,Team1Id,Team2Id")] PlayoffSeries playoffSeries, bool complete)
        {
            playoffSeries.Id = Guid.NewGuid();
            playoffSeries.Season = _context.Season.FirstOrDefault(s => s.Id == playoffSeries.SeasonId);
            playoffSeries.Round = _context.PlayoffRound.FirstOrDefault(r => r.Id == playoffSeries.RoundId);
            playoffSeries.Team1 = _context.Team.FirstOrDefault(t => t.Id == playoffSeries.Team1Id);
            playoffSeries.Team2 = _context.Team.FirstOrDefault(t => t.Id == playoffSeries.Team2Id);
            
            if (complete)
            {
                _context.Add(playoffSeries);
                await _context.SaveChangesAsync();

                await GenerateSchedule(playoffSeries);

                return RedirectToAction("Playoffs", "Schedule", new { season = playoffSeries.Season.Year, round = playoffSeries.Round.Index });
            }

            var seasons = _context.Season
                .OrderByDescending(s => s.Year);
            var season = _context.Season
                .Where(s => s.Id == playoffSeries.SeasonId)
                .FirstOrDefault();
            ViewBag.Seasons = new SelectList(seasons, "Id", "Year", season.Year);

            var rounds = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Season.Year == season.Year)
                .OrderBy(r => r.Index);
            var round = _context.PlayoffRound
                .Include(r => r.Season)
                .Where(r => r.Id == playoffSeries.RoundId)
                .FirstOrDefault();
            ViewBag.Rounds = new SelectList(rounds, "Id", "Name", round.Name);

            var teams = _context.Alignment
                .Include(a => a.Season)
                .Include(a => a.Conference)
                .Include(a => a.Division)
                .Include(a => a.Team)
                .Where(a => a.Season.Year == season.Year)
                .Select(a => a.Team)
                .OrderBy(a => a.City)
                .ThenBy(a => a.Name);
            ViewBag.Teams = new SelectList(teams, "Id", "FullName");

            return View(playoffSeries);
        }

        // GET: PlayoffSeries/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.PlayoffSeries == null)
            {
                return NotFound();
            }

            var playoffSeries = await _context.PlayoffSeries.FindAsync(id);
            if (playoffSeries == null)
            {
                return NotFound();
            }
            ViewData["RoundId"] = new SelectList(_context.PlayoffRound, "Id", "Id", playoffSeries.RoundId);
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id", playoffSeries.SeasonId);
            ViewData["Team1Id"] = new SelectList(_context.Team, "Id", "Id", playoffSeries.Team1Id);
            ViewData["Team2Id"] = new SelectList(_context.Team, "Id", "Id", playoffSeries.Team2Id);
            return View(playoffSeries);
        }

        // POST: PlayoffSeries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SeasonId,RoundId,StartDate,Team1Id,Team1Wins,Team2Id,Team2Wins")] PlayoffSeries playoffSeries)
        {
            if (id != playoffSeries.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(playoffSeries);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlayoffSeriesExists(playoffSeries.Id))
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
            ViewData["RoundId"] = new SelectList(_context.PlayoffRound, "Id", "Id", playoffSeries.RoundId);
            ViewData["SeasonId"] = new SelectList(_context.Season, "Id", "Id", playoffSeries.SeasonId);
            ViewData["Team1Id"] = new SelectList(_context.Team, "Id", "Id", playoffSeries.Team1Id);
            ViewData["Team2Id"] = new SelectList(_context.Team, "Id", "Id", playoffSeries.Team2Id);
            return View(playoffSeries);
        }

        // GET: PlayoffSeries/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.PlayoffSeries == null)
            {
                return NotFound();
            }

            var playoffSeries = await _context.PlayoffSeries
                .Include(p => p.Round)
                .Include(p => p.Season)
                .Include(p => p.Team1)
                .Include(p => p.Team2)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (playoffSeries == null)
            {
                return NotFound();
            }

            return View(playoffSeries);
        }

        // POST: PlayoffSeries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.PlayoffSeries == null)
            {
                return Problem("Entity set 'ApplicationDbContext.PlayoffSeries'  is null.");
            }
            var playoffSeries = await _context.PlayoffSeries.FindAsync(id);
            if (playoffSeries != null)
            {
                _context.PlayoffSeries.Remove(playoffSeries);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlayoffSeriesExists(Guid id)
        {
          return (_context.PlayoffSeries?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task GenerateSchedule(PlayoffSeries series)
        {
            Schedule[] games = new Schedule[7];
            
            if (series.Season.Year >= 2024 && series.Round.Name == "Sycamore Cup Final")
            {
                for (int index = 0; index < games.Length; index++)
                {
                    int gameIndex = index + 1;
                    bool team1IsHome = gameIndex == 1 || gameIndex == 2 || gameIndex == 5 || gameIndex == 7;

                    var game = new Schedule
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = series.SeasonId,
                        Season = series.Season,
                        Date = series.StartDate.AddDays(2 * index),
                        GameIndex = _context.Schedule.Max(s => s.GameIndex) + 1,
                        Type = "Playoffs",
                        PlayoffRoundId = series.RoundId,
                        PlayoffRound = series.Round,
                        AwayTeamId = team1IsHome ? series.Team2Id : series.Team1Id,
                        AwayTeam = team1IsHome ? series.Team2 : series.Team1,
                        HomeTeamId = team1IsHome ? series.Team1Id : series.Team2Id,
                        HomeTeam = team1IsHome ? series.Team1 : series.Team2,
                        Notes = $"Game {gameIndex}"
                    };

                    games[index] = game;
                    _context.Schedule.Add(game);
                }
            }
            else
            {
                for (int index = 0; index < games.Length; index++)
                {
                    int gameIndex = index + 1;
                    bool team1IsHome = gameIndex == 1 || gameIndex == 2 || gameIndex == 5 || gameIndex == 7;
                    bool isBackToBack = gameIndex == 2 || gameIndex == 4;

                    DateTime gameDate = series.StartDate;
                    DateTime previousGameDate;
                    int daysBetweenGames = 1;
                    if (gameIndex > 1)
                    {
                        previousGameDate = games[index - 1].Date;
                        daysBetweenGames = isBackToBack ? 0 : 1;
                        gameDate = previousGameDate.AddDays(1 + daysBetweenGames);
                    }

                    var game = new Schedule
                    {
                        Id = Guid.NewGuid(),
                        SeasonId = series.SeasonId,
                        Season = series.Season,
                        Date = gameDate,
                        GameIndex = _context.Schedule.Max(s => s.GameIndex) + 1,
                        Type = "Playoffs",
                        PlayoffRoundId = series.RoundId,
                        PlayoffRound = series.Round,
                        AwayTeamId = team1IsHome ? series.Team2Id : series.Team1Id,
                        AwayTeam = team1IsHome ? series.Team2 : series.Team1,
                        HomeTeamId = team1IsHome ? series.Team1Id : series.Team2Id,
                        HomeTeam = team1IsHome ? series.Team1 : series.Team2,
                        Notes = $"Game {gameIndex}"
                    };

                    games[index] = game;
                    _context.Schedule.Add(game);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
