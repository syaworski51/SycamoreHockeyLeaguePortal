using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AlignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Alignments
        [AllowAnonymous]
        public async Task<IActionResult> Index(int season)
        {
            ViewBag.Season = season;
            
            var seasons = _context.Seasons
                .OrderByDescending(s => s.Year);
            ViewBag.Seasons = new SelectList(seasons, "Year", "Year");
            
            var alignments = _context.Alignments
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
    }
}
