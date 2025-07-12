using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.ConstantGroups;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    public class HeadToHeadController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HeadToHeadController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HeadToHead
        public async Task<IActionResult> Index(int? season, string? team)
        {
            IQueryable<HeadToHeadSeries> h2hSeries = _context.HeadToHeadSeries
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .OrderBy(s => s.Season.Year)
                .ThenBy(s => s.Team1.City)
                .ThenBy(s => s.Team1.Name)
                .ThenBy(s => s.Team2.City)
                .ThenBy(s => s.Team2.Name);

            if (season != null)
                h2hSeries = h2hSeries.Where(s => s.Season.Year == season);

            if (team != null)
                h2hSeries = h2hSeries.Where(s => s.Team1.Code == team || s.Team2.Code == team);

            return View(await h2hSeries.AsNoTracking().ToListAsync());
        }

        // GET: HeadToHead/Details/5
        public async Task<IActionResult> Details(int? season, string team1, string team2)
        {
            ViewBag.Season = season;
            
            IQueryable<HeadToHeadSeries> headToHeadSeries = _context.HeadToHeadSeries
                .Include(h => h.Season)
                .Include(h => h.Team1)
                .Include(h => h.Team2)
                .Where(m => (m.Team1.Code == team1 && m.Team2.Code == team2) ||
                            (m.Team1.Code == team2 && m.Team2.Code == team1))
                .OrderBy(h => h.Season.Year);

            if (season != null)
                headToHeadSeries = headToHeadSeries.Where(s => s.Season.Year == season);

            var games = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == GameTypes.REGULAR_SEASON &&
                            ((s.AwayTeam.Code == team1 && s.HomeTeam.Code == team2) ||
                             (s.AwayTeam.Code == team2 && s.HomeTeam.Code == team1)))
                .OrderBy(s => s.Date.Date)
                .ToListAsync();
            ViewBag.Games = games;

            if (headToHeadSeries == null)
            {
                return NotFound();
            }

            return View(await headToHeadSeries.AsNoTracking().ToListAsync());
        }
    }
}
