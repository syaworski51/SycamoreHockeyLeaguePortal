using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

        private const string REGULAR_SEASON = "Regular Season";
        private const string PLAYOFFS = "Playoffs";

        private const string DIVISION = "division";
        private const string CONFERENCE = "conference";
        private const string INTER_CONFERENCE = "inter-conference";

        public ScheduleController(SHLPortalDbContext context)
        {
            _context = context;
        }

        private async Task<Schedule?> GetGame(Guid id)
        {
            var game = await _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .FirstOrDefaultAsync(s => s.Id == id);

            return game;
        }

        private async Task UpdateGameAsync(Schedule game)
        {
            _context.Schedule.Update(game);
            await _context.SaveChangesAsync();
        }

        [Route("StartOrResumeGame/{id}")]
        [HttpPost]
        public async Task<IActionResult> StartOrResumeGame(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (DateTime.Now.Date < game.Date.Date && game.Season.Year != 2026)
                    return BadRequest("This game cannot be started yet.");

                Schedule? previousGame = _context.Schedule.FirstOrDefault(g => g.GameIndex == game.GameIndex - 1);
                if (previousGame == null && game.GameIndex > 2)
                {
                    for (int index = game.GameIndex - 2; index >= 1; index--)
                    {
                        previousGame = _context.Schedule.FirstOrDefault(g => g.GameIndex == index);

                        if (previousGame != null)
                            break;
                    }
                }
                
                if ((game.GameIndex > 1 && (previousGame == null || !previousGame.IsFinalized)) && game.Season.Year != 2026)
                    return BadRequest();

                if (!game.IsLive && !game.IsFinalized)
                {
                    game.IsLive = true;

                    if (game.Period <= 0)
                        game.Period = 1;

                    await UpdateGameAsync(game);
                }

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error starting or resuming the game.\n" +
                    $"Descrption: {ex.Message}");
            }
        }

        [Route("NextPeriod/{id}")]
        [HttpPost]
        public async Task<IActionResult> NextPeriod(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanPeriodAdvance(game))
                {
                    game.Period++;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error advancing to the next period.\n" +
                    $"Description: {ex.Message}");
            }
        }

        [Route("PreviousPeriod/{id}")]
        [HttpPost]
        public async Task<IActionResult> PreviousPeriod(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanPeriodMoveBack(game))
                {
                    game.Period--;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error moving back to the previous period.\n" +
                    $"Description: {ex.Message}");
            }
        }

        [Route("AwayGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> AwayGoal(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanScoresBeIncremented(game))
                {
                    game.AwayScore++;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error adding 1 point to the away score.\n" +
                    $"Description: {ex.Message}");
            }
        }

        [Route("RemoveAwayGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> RemoveAwayGoal(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanScoreBeDecremented(game, game.AwayScore))
                {
                    game.AwayScore--;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error removing 1 point from the away score.\n" +
                    $"Description: {ex.Message}");
            }
        }

        [Route("HomeGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> HomeGoal(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanScoresBeIncremented(game))
                {
                    game.HomeScore++;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error adding 1 point to the home score.\n" +
                    $"Description: {ex.Message}");
            }
        }

        [Route("RemoveHomeGoal/{id}")]
        [HttpPost]
        public async Task<IActionResult> RemoveHomeGoal(Guid id)
        {
            try
            {
                var game = await GetGame(id);
                if (game == null)
                    return NotFound();

                if (CanScoreBeDecremented(game, game.HomeScore))
                {
                    game.HomeScore--;
                    await UpdateGameAsync(game);
                }
                else
                    return BadRequest();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error removing 1 point from the home score.\n" +
                    $"Description: {ex.Message}");
            }
        }

        /// <summary>
        ///     A game is considered live if its IsLive flag is True and it has not yet been finalized.
        /// </summary>
        /// <param name="game">The game to evaluate.</param>
        /// <returns>True if the above condition is met, otherwise False.</returns>
        private bool IsGameLive(Schedule game)
        {
            return game.IsLive && !game.IsFinalized;
        }

        /// <summary>
        ///     A period can advance if
        ///     <ul>
        ///         <li>it is currently live, AND</li>
        ///         <li>has not yet reached the 3rd period OR the score is tied, AND</li>
        ///         <li>
        ///             it is either a regular season game that has not yet reached the shootout OR 
        ///             it is a playoff game in any period.
        ///         </li>
        ///     </ul>
        /// </summary>
        /// <param name="game">The game to evaluate.</param>
        /// <returns>True if the above conditions are met, otherwise False.</returns>
        private bool CanPeriodAdvance(Schedule game)
        {
            return IsGameLive(game) && (game.Period < 3 || game.AwayScore == game.HomeScore) &&
                   ((game.Type == REGULAR_SEASON && game.Period < 5) || game.Type == PLAYOFFS);
        }

        /// <summary>
        ///     A period can move back if the game is currently live AND has passed the 1st period.
        /// </summary>
        /// <param name="game">The game to evaluate.</param>
        /// <returns>True if the above condition is met, otherwise False.</returns>
        private bool CanPeriodMoveBack(Schedule game)
        {
            return IsGameLive(game) && game.Period > 1;
        }

        /// <summary>
        ///     Either score can be incremented if the game is live, AND 
        ///     either in the 3rd period or earlier OR if the score is tied.
        /// </summary>
        /// <param name="game">The game to evaluate.</param>
        /// <returns>True if the above condition is met, otherwise False.</returns>
        private bool CanScoresBeIncremented(Schedule game)
        {
            return IsGameLive(game) && (game.Period <= 3 || game.AwayScore == game.HomeScore);
        }

        /// <summary>
        ///     A score can be decremented if the game is live and the score is greater than 0.
        /// </summary>
        /// <param name="game">The game to evaluate.</param>
        /// <param name="score">The score to decrement, whether it is the home score or away score.</param>
        /// <returns>True if the above condition is met, otherwise False.</returns>
        private bool CanScoreBeDecremented(Schedule game, int score)
        {
            return IsGameLive(game) && score > 0;
        }
    }
}
