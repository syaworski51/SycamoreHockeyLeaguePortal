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
            var champions = _context.Champions
                .Include(c => c.Season)
                .Include(c => c.Team)
                .OrderByDescending(c => c.Season.Year);

            List<List<ChampionsRound>> rounds = new List<List<ChampionsRound>>();
            foreach (var champion in champions)
            {
                var championsRounds = _context.ChampionsRounds
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
            if (id == null || _context.Champions == null)
            {
                return NotFound();
            }

            var champion = await _context.Champions
                .Include(c => c.Season)
                .Include(c => c.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (champion == null)
            {
                return NotFound();
            }

            return View(champion);
        }

        private bool ChampionExists(Guid id)
        {
          return (_context.Champions?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
