using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SHLAPI.Data;
using SHLAPI.Models;

namespace SHLAPI.Controllers
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

        private async Task<Schedule> GetGameAsync(Guid id)
        {
            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Id == id);

            return game!;
        }

        private async Task UpdateGameAsync(Schedule game)
        {
            _context.Schedule.Update(game);
            await _context.SaveChangesAsync();
        }

        [Route("NextPeriod/{id}")]
        [HttpPost]
        public async Task<IActionResult> NextPeriod(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive)
            {
                game.Period++;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }

        [Route("PreviousPeriod/{id}")]
        [HttpPost]
        public async Task<IActionResult> PreviousPeriod(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive && game.Period > 1)
            {
                game.Period--;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }

        [Route("AwayGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> AwayGoal(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive)
            {
                game.AwayScore++;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }

        [Route("RemoveAwayGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> RemoveAwayGoal(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive && game.AwayScore > 0)
            {
                game.AwayScore--;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }

        [Route("HomeGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> HomeGoal(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive)
            {
                game.HomeScore++;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }

        [Route("RemoveHomeGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> RemoveHomeGoal(Guid id)
        {
            var game = await GetGameAsync(id);

            if (game.IsLive && game.HomeScore > 0)
            {
                game.HomeScore--;
                await UpdateGameAsync(game);
            }

            return Ok(game);
        }
    }
}
