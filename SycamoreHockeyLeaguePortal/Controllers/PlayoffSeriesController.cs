﻿using System;
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
        public async Task<IActionResult> Index(int season)
        {
            var series = _context.PlayoffSeries
                .Include(p => p.Round)
                .Include(p => p.Season)
                .Include(p => p.Team1)
                .Include(p => p.Team2)
                .Where(p => p.Season.Year == season)
                .OrderBy(p => p.Index);

            return View(await series.ToListAsync());
        }

        // GET: PlayoffSeries/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int season, string team1, string team2)
        {
            if (_context.PlayoffSeries == null)
                return NotFound();

            var series = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .FirstOrDefault(s => s.Season.Year == season &&
                                     ((s.Team1!.Code == team1 && s.Team2!.Code == team2) ||
                                      (s.Team1!.Code == team2 && s.Team2!.Code == team1)));

            var games = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Type == "Playoffs" && s.Season.Year == season &&
                            ((s.AwayTeam.Code == team1 && s.HomeTeam.Code == team2) ||
                             (s.AwayTeam.Code == team2 && s.HomeTeam.Code == team1)))
                .OrderBy(s => s.Date)
                .ToList();
            ViewBag.Games = games;

            if (series == null)
                return NotFound();

            return View(series);
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

        public async Task<IActionResult> ConfirmPlayoffMatchup(int season, string index)
        {
            IQueryable<PlayoffSeries> matchups = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.Index);

            var matchup = matchups.FirstOrDefault(s => s.Index == index)!;
            matchups = matchups.Where(m => m.Round == matchup.Round);

            if (matchup.IsConfirmed)
                return BadRequest("This matchup has already been confirmed.");

            matchup.IsConfirmed = true;
            _context.PlayoffSeries.Update(matchup);
            await _context.SaveChangesAsync();

            await GenerateSchedule(matchup);

            matchup.Round.MatchupsConfirmed = matchups.Count(m => m.IsConfirmed) == matchups.Count();
            if (matchup.Round.MatchupsConfirmed && matchup.Round.Index < 4)
                await ReindexPlayoffGames(matchup.Round);

            await _context.SaveChangesAsync();

            return RedirectToAction("PlayoffBracket", "Standings", new { season = season });
        }

        private async Task ReindexPlayoffGames(PlayoffRound round)
        {
            var schedule = _context.Schedule
                .Include(s => s.Season)
                .Include(s => s.PlayoffRound)
                .Include(s => s.PlayoffSeries)
                .Include(s => s.AwayTeam)
                .Include(s => s.HomeTeam)
                .Where(s => s.Season.Year == round.Season.Year &&
                            s.Type == "Playoffs" &&
                            s.PlayoffRound == round)
                .OrderBy(s => s.Date)
                .ThenByDescending(s => s.PlayoffSeries!.StartDate)
                .ThenBy(s => s.PlayoffSeries!.Index);

            int index = schedule.Min(s => s.GameIndex);
            foreach (var game in schedule)
            {
                game.GameIndex = index;
                index++;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> SetMatchups(int year, int index)
        {
            var season = await _context.Seasons.FirstOrDefaultAsync(s => s.Year == year);
            if (season == null)
                return ErrorMessage($"There is no {year} season in the database.");

            var matchups = _context.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Where(s => s.Season.Year == year && s.Round.Index == index)
                .OrderBy(s => s.Index)
                .ToList()!;
            if (matchups == null)
                return ErrorMessage($"There are no matchups for Round {index} of the {year} Sycamore Cup Playoffs.");

            var form = new PlayoffSeries_MatchupsForm { Matchups = matchups };
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMatchups(PlayoffSeries_MatchupsForm form)
        {
            await _context.SaveChangesAsync();
            return View(form);
        }

        public IActionResult IncorrectHomeIceAdvantageDisclaimer()
        {
            return View();
        }

        private async Task GenerateSchedule(PlayoffSeries series)
        {
            Schedule[] games = new Schedule[7];

            for (int index = 0; index < games.Length; index++)
            {
                int gameIndex = index + 1;
                bool team1IsHome = gameIndex == 1 || gameIndex == 2 || gameIndex == 5 || gameIndex == 7;

                DateTime gameDate = (DateTime)series.StartDate!;
                if (gameIndex > 1)
                {
                    DateTime previousGameDate = games[index - 1].Date;
                    int daysBetweenGames = DetermineDaysBetweenGames(series.Round.Index, gameIndex);
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
                    PlayoffGameIndex = gameIndex,
                    PlayoffRoundId = series.RoundId,
                    PlayoffRound = series.Round,
                    PlayoffSeriesId = series.Id,
                    PlayoffSeries = series,
                    AwayTeamId = (Guid)(team1IsHome ? series.Team2Id : series.Team1Id)!,
                    AwayTeam = (team1IsHome ? series.Team2 : series.Team1)!,
                    HomeTeamId = (Guid)(team1IsHome ? series.Team1Id : series.Team2Id)!,
                    HomeTeam = (team1IsHome ? series.Team1 : series.Team2)!,
                    IsConfirmed = gameIndex <= 4,
                    Notes = gameString,
                    PlayoffSeriesScore = gameString
                };

                games[index] = game;
                _context.Schedule.Add(game);
            }

            await _context.SaveChangesAsync();
        }

        private int DetermineDaysBetweenGames(int round, int game)
        {
            bool isBackToBack = game == 2 || game == 4;
            
            if (round == 4 && game == 5)
                return 2;

            return isBackToBack ? 0 : 1;
        }

        private IActionResult ErrorMessage(string message)
        {
            var errorMessage = new ErrorViewModel { Description = message };
            return View("Error", errorMessage);
        }
    }
}