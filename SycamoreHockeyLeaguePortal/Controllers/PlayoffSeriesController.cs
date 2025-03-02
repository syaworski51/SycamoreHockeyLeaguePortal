using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.ProjectModel;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Data.Migrations;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.InputForms;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PlayoffSeriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayoffSeriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlayoffSeries
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        // GET: PlayoffSeries/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.PlayoffSeries == null)
            {
                return NotFound();
            }

            var playoffSeries = await _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (playoffSeries == null)
                return NotFound();

            if (playoffSeries.IsConfirmed)
                return BadRequest();

            var form = new PlayoffSeries_EditForm
            {
                Id = playoffSeries.Id,
                StartDate = playoffSeries.StartDate,
                Team1Id = playoffSeries.Team1Id,
                Team1 = playoffSeries.Team1,
                Team2Id = playoffSeries.Team2Id,
                Team2 = playoffSeries.Team2
            };

            var teams = _context.Standings
                .Include(s => s.Team)
                .Where(s => s.Season.Year == playoffSeries.Season.Year)
                .Select(s => s.Team)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Team1Options = new SelectList(teams, "Id", "FullName");
            ViewBag.Team2Options = new SelectList(teams, "Id", "FullName");

            return View(form);
        }

        // POST: PlayoffSeries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PlayoffSeries_EditForm form)
        {
            form.Team1 = await _context.Teams.FindAsync(form.Team1Id);
            form.Team2 = await _context.Teams.FindAsync(form.Team2Id);
            
            var series = _context.PlayoffSeries
                .Include(s => s.Round)
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .FirstOrDefault(s => s.Id == id)!;

            if (series == null)
                return NotFound();

            if (series.IsConfirmed)
                return BadRequest();
            
            series.StartDate = form.StartDate;
            series.Team1Id = form.Team1Id;
            series.Team1 = form.Team1;
            series.Team2Id = form.Team2Id;
            series.Team2 = form.Team2;

            if (series.Team1 != series.Team2 || (series.Team1 == null && series.Team2 == null))
            {
                _context.PlayoffSeries.Update(series);
                await _context.SaveChangesAsync();

                return RedirectToAction("PlayoffBracket", "Standings", new { season = series.Season.Year });
            }

            var teams = _context.Standings
                .Include(s => s.Team)
                .Where(s => s.Season.Year == series.Season.Year)
                .Select(s => s.Team)
                .Distinct()
                .OrderBy(s => s.City)
                .ThenBy(s => s.Name);
            ViewBag.Team1Options = new SelectList(teams, "Id", "FullName", series.Team1);
            ViewBag.Team2Options = new SelectList(teams, "Id", "FullName", series.Team2);

            return View(form);
        }

        private bool PlayoffSeriesExists(Guid id)
        {
          return (_context.PlayoffSeries?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task GenerateSchedule(PlayoffSeries series)
        {
            Schedule[] games = new Schedule[7];
            
            for (int index = 0; index < games.Length; index++)
            {
                int gameIndex = index + 1;
                bool team1IsHome = gameIndex == 1 || gameIndex == 2 || gameIndex == 5 || gameIndex == 7;
                bool isBackToBack = gameIndex == 2 || gameIndex == 4;

                DateTime gameDate = (DateTime)series.StartDate!;
                DateTime previousGameDate;
                int daysBetweenGames = 1;
                if (gameIndex > 1)
                {
                    previousGameDate = games[index - 1].Date;
                    daysBetweenGames = isBackToBack ? 0 : 1;
                    gameDate = previousGameDate.AddDays(1 + daysBetweenGames);
                }

                string gameString = $"Game {gameIndex}";
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
                    AwayTeamId = (Guid)(team1IsHome ? series.Team2Id : series.Team1Id)!,
                    AwayTeam = (team1IsHome ? series.Team2 : series.Team1)!,
                    HomeTeamId = (Guid)(team1IsHome ? series.Team1Id : series.Team2Id)!,
                    HomeTeam = (team1IsHome ? series.Team1 : series.Team2)!,
                    Notes = gameString,
                    PlayoffSeriesScore = gameString
                };

                games[index] = game;
                _context.Schedule.Add(game);
            }

            await _context.SaveChangesAsync();
        }
    }
}
