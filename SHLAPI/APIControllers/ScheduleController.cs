using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHLAPI.Data;
using SHLAPI.Models;

namespace SHLAPI.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly SHLPortalDbContext _context;

        public ScheduleController(SHLPortalDbContext context)
        {
            _context = context;
        }

        // GET: api/Schedule
        [HttpGet("{season}/{gameType}")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedule(int season, string gameType)
        {
            if (_context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Type == gameType);
            
            return await schedule.ToListAsync();
        }

        // GET: api/Schedule/5
        [HttpGet("{season}/{type}/{date}/{awayCode}/{homeCode}")]
        public async Task<ActionResult<Schedule>> GetGame(int season, DateTime date, string awayCode, string homeCode)
        {
            if (_context.Schedule == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Date == date &&
                            s.AwayTeam.Code == awayCode &&
                            s.HomeTeam.Code == homeCode)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            return schedule;
        }

        // PUT: api/Schedule/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSchedule(Guid id, Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return BadRequest();
            }

            _context.Entry(schedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScheduleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPut("{season}/{date}/{awayCode}/{homeCode}")]
        public async Task<IActionResult> AwayGoal(int season, DateTime date, string awayCode, string homeCode)
        {
            var game = await RetrieveGame(season, date, awayCode, homeCode);

            if (game == null)
                return NotFound();

            await ChangeScore(game, game.AwayTeam);
            return NoContent();
        }

        [HttpPut("{season}/{date}/{awayCode}/{homeCode}")]
        public async Task<IActionResult> RemoveAwayGoal(int season, DateTime date, string awayCode, string homeCode)
        {
            var game = await RetrieveGame(season, date, awayCode, homeCode);

            if (game == null)
                return NotFound();

            await ChangeScore(game, game.AwayTeam, -1);
            return NoContent();
        }

        [HttpPut("{season}/{date}/{awayCode}/{homeCode}")]
        public async Task<IActionResult> HomeGoal(int season, DateTime date, string awayCode, string homeCode)
        {
            var game = await RetrieveGame(season, date, awayCode, homeCode);

            if (game == null)
                return NotFound();

            await ChangeScore(game, game.HomeTeam);
            return NoContent();
        }

        [HttpPut("{season}/{date}/{awayCode}/{homeCode}")]
        public async Task<IActionResult> RemoveHomeGoal(int season, DateTime date, string awayCode, string homeCode)
        {
            var game = await RetrieveGame(season, date, awayCode, homeCode);

            if (game == null)
                return NotFound();

            await ChangeScore(game, game.HomeTeam, -1);
            return NoContent();
        }

        // POST: api/Schedule
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Schedule>> PostGame(Schedule game)
        {
          if (_context.Schedule == null)
          {
              return Problem("Entity set 'SHLPortalDbContext.Schedules'  is null.");
          }
            _context.Schedule.Add(game);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ScheduleExists(game.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSchedule", new { id = game.Id }, game);
        }

        // DELETE: api/Schedule/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(Guid id)
        {
            if (_context.Schedule == null)
            {
                return NotFound();
            }
            var schedule = await _context.Schedule.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            _context.Schedule.Remove(schedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ScheduleExists(Guid id)
        {
            return (_context.Schedule?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<Schedule> RetrieveGame(int season, DateTime date, string awayCode, string homeCode)
        {
            return await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == season &&
                            s.Date == date &&
                            s.AwayTeam.Code == awayCode &&
                            s.HomeTeam.Code == homeCode)
                .FirstOrDefaultAsync();
        }

        private async Task ChangeScore(Schedule game, Team team, int changeInScore = 1)
        {
            if (game.HomeTeam == team)
                game.HomeScore += changeInScore;
            else
                game.AwayScore += changeInScore;

            _context.Update(game);
            await _context.SaveChangesAsync();
        }
    }
}
