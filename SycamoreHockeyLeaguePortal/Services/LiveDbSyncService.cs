﻿using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.DataTransferPackages;
using SycamoreHockeyLeaguePortal.Models.DbSyncPackages;

namespace SycamoreHockeyLeaguePortal.Services
{
    public class LiveDbSyncService
    {
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;

        private const string REGULAR_SEASON = "Regular Season";
        private const string TIEBREAKER = "Tiebreaker";
        private const string PLAYOFFS = "Playoffs";

        public LiveDbSyncService(ApplicationDbContext local, LiveDbContext live)
        {
            _localContext = local;
            _liveContext = live;
        }

        /// <summary>
        ///     Adds a record to a table in the live database.
        /// </summary>
        /// <typeparam name="TRecord">The type of record to insert.</typeparam>
        /// <param name="table">The table to insert a record into.</param>
        /// <param name="record">The record to insert into the table.</param>
        /// <param name="exception">The exception to throw in case the record already exists in the table.</param>
        /// <returns></returns>
        private async Task AddRecordToTableAsync<TRecord>(DbSet<TRecord> table, 
                                                          TRecord record, 
                                                          Exception exception) where TRecord : class
        {
            // Check to see if the record to be inserted already exists in the specified table
            bool recordExists = table.Any(r => r == record);
            
            // If the record DOES NOT exist in the table, add it to the table
            if (!recordExists)
                table.Add(record);
            
            // If the record DOES exist, throw the specified exception
            else
                throw exception;

            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Adds a list of records to a table in the live database.
        /// </summary>
        /// <typeparam name="TRecord">The type of records to insert.</typeparam>
        /// <param name="table">The table to insert the records into.</param>
        /// <param name="records">The records to insert into the table.</param>
        /// <param name="exception">The exception to throw in case any of the records are already in the table.</param>
        /// <returns></returns>
        private async Task AddRecordsToTableAsync<TRecord>(DbSet<TRecord> table, 
                                                           List<TRecord> records, 
                                                           Exception exception) where TRecord : class
        {
            // Insert each record into the table one at a time
            foreach (var record in records)
                await AddRecordToTableAsync(table, record, exception);

            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Creates a new season, along with its accompanying tables.
        /// </summary>
        /// <param name="package">Contains all the information necessary to create a new season.</param>
        /// <returns></returns>
        public async Task NewSeasonAsync(NewSeasonPackage package)
        {
            // Add a new season record
            await AddRecordToTableAsync(_liveContext.Seasons, package.Season, new InvalidOperationException($"The {package.Season.Year} season already exists in the database;"));
            
            // Create new standings records
            await AddStandingsRecordsAsync(package.Standings);

            // Create new alignment records
            await AddAlignmentRecordsAsync(package.Alignments);

            // Create new head-to-head matchups
            await AddH2HMatchupsAsync(package.HeadToHeadSeries);

            // Create new playoff rounds
            await AddPlayoffRoundsAsync(package.PlayoffRounds);

            // Create new playoff matchups
            await AddPlayoffMatchupsAsync(package.PlayoffSeries);
        }

        /// <summary>
        ///     Adds a new champion to the database.
        /// </summary>
        /// <param name="package">Contains the necessary data to add a new champion.</param>
        /// <returns></returns>
        public async Task NewChampionAsync(NewChampionPackage package)
        {
            // Add the new champion to the Champions table
            var exception = new InvalidOperationException($"The {package.Champion.Season.Year} {package.Champion.Team.FullName} are already in the Champions table.");
            await AddRecordToTableAsync(_liveContext.Champions, package.Champion, exception);

            // Add the opponents defeated by the champion to the ChampionsRounds table
            exception = new InvalidOperationException($"This opponent is already in the database.");
            await AddRecordsToTableAsync(_liveContext.ChampionsRounds, package.Rounds, exception);
        }

        /// <summary>
        ///     Writes one game result to the Schedule table.
        /// </summary>
        /// <param name="game">The game to record the results of.</param>
        /// <returns></returns>
        public async Task WriteOneResultAsync(Game game)
        {
            // Strip the navigation properties if any of them have not been stripped yet
            if (game.Season != null || game.PlayoffRound != null || game.PlayoffSeries != null ||
                game.AwayTeam != null || game.HomeTeam != null)
            {
                game.Season = null;
                game.PlayoffRound = null;
                game.PlayoffSeries = null;
                game.AwayTeam = null;
                game.HomeTeam = null;
            }
            
            // If the game already exists, update it
            bool gameExistsInLiveDB = await _liveContext.Schedule.AnyAsync(s => s.Id == game.Id);
            if (gameExistsInLiveDB)
                _liveContext.Schedule.Update(game);
            // If not, add it to the Schedule table
            else
                _liveContext.Schedule.Add(game);

            await _liveContext.SaveChangesAsync();

            // After saving the changes to the live database, restore the navigation properties asynchronously
            game.Season = await _liveContext.Seasons
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == game.SeasonId) ??
            throw new Exception("There was an error restoring the Season property.");

            game.PlayoffRound = await _liveContext.PlayoffRounds
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == game.PlayoffRoundId);

            game.PlayoffSeries = await _liveContext.PlayoffSeries
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == game.PlayoffSeriesId);

            game.AwayTeam = await _liveContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == game.AwayTeamId) ??
            throw new Exception("There was an error restoring the AwayTeam property.");

            game.HomeTeam = await _liveContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == game.HomeTeamId) ??
            throw new Exception("There was an error restoring the HomeTeam property.");

            
            // Get the season and teams
            int season = game.Season.Year;
            Team awayTeam = game.AwayTeam;
            Team homeTeam = game.HomeTeam;

            // If it is a playoff game...
            if (game.Type == PLAYOFFS)
            {
                // Update the live playoff series using the local playoff series
                var playoffSeries = await GetLocalPlayoffSeriesAsync(season, awayTeam, homeTeam);
                await UpdatePlayoffMatchupAsync(playoffSeries);
            }
            // If it is a regular season game or tiebreaker game...
            else
            {
                // Get the head-to-head matchup between the two teams
                var h2hSeries = await GetLocalH2HSeriesAsync(season, awayTeam, homeTeam);
                await UpdateH2HSeriesAsync(h2hSeries);

                // If it is a regular season game, update the standings
                if (game.Type == REGULAR_SEASON)
                    await UpdateStandingsAsync(season);
            }
        }

        /// <summary>
        ///     Writes many results to the Schedule table.
        /// </summary>
        /// <param name="games">The results to write to the Schedule table.</param>
        /// <returns></returns>
        public async Task WriteManyResultsAsync(List<Game> games)
        {
            // Write each result to the table one at a time
            foreach (var game in games)
                await WriteOneResultAsync(game);

            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Adds one game to the Schedule table.
        /// </summary>
        /// <param name="game">The game to add to the Schedule table.</param>
        /// <returns></returns>
        public async Task AddOneGameAsync(Game game)
        {
            var exception = new InvalidOperationException($"{game.Date:yyyy-MM-dd} - {game.AwayTeam.Code} @ {game.HomeTeam.Code} already exists in the database.");
            await AddRecordToTableAsync(_liveContext.Schedule, game, exception);
        }

        /// <summary>
        ///     Adds many games to the Schedule table.
        /// </summary>
        /// <param name="schedule">The games to add to the Schedule table.</param>
        /// <returns></returns>
        public async Task AddManyGamesAsync(List<Game> schedule)
        {
            var exception = new InvalidOperationException($"This game already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.Schedule, schedule, exception);
        }

        /// <summary>
        ///     Deletes a game from the Schedule table. <strong>Note:</strong> This can only be done if the game has not 
        ///     been confirmed yet.
        /// </summary>
        /// <param name="game">The game to be deleted.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the game to be deleted has been confirmed.</exception>
        public async Task DeleteOneGameAsync(Game game)
        {
            // If the game has NOT been confirmed, delete it
            if (!game.IsConfirmed)
                _liveContext.Schedule.Remove(game);

            // If it HAS been confirmed, throw the exception
            else
                throw new InvalidOperationException("This game is confirmed and cannot be deleted.");

            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Deletes many games from the Schedule table. <strong>Note:</strong> Games can only be deleted if they have 
        ///     not been confirmed yet.
        /// </summary>
        /// <param name="games">The games to be deleted.</param>
        /// <returns></returns>
        public async Task DeleteManyGamesAsync(List<Game> games)
        {
            // Delete each game one at a time.
            foreach (var game in games)
                await DeleteOneGameAsync(game);

            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Adds new alignment records.
        /// </summary>
        /// <param name="alignments">The alignment records to add.</param>
        /// <returns></returns>
        public async Task AddAlignmentRecordsAsync(List<Alignment> alignments)
        {
            var exception = new InvalidOperationException("The current team already has an alignment for this season.");
            await AddRecordsToTableAsync(_liveContext.Alignments, alignments, exception);
        }

        /// <summary>
        ///     Adds new standings records.
        /// </summary>
        /// <param name="standings">The standings records to add.</param>
        /// <returns></returns>
        public async Task AddStandingsRecordsAsync(List<Standings> standings)
        {
            var exception = new InvalidOperationException("The current team already has a standings record for this season.");
            await AddRecordsToTableAsync(_liveContext.Standings, standings, exception);
        }

        public async Task UpdatePlayoffMatchupAsync(PlayoffSeries localPlayoffMatchup)
        {
            var matchup = new PlayoffSeries
            {
                Id = localPlayoffMatchup.Id,
                SeasonId = localPlayoffMatchup.SeasonId,
                RoundId = localPlayoffMatchup.RoundId,
                StartDate = localPlayoffMatchup.StartDate,
                Index = localPlayoffMatchup.Index,
                Team1Id = localPlayoffMatchup.Team1Id,
                Team1Wins = localPlayoffMatchup.Team1Wins,
                Team1Placeholder = localPlayoffMatchup.Team1Placeholder,
                Team2Id = localPlayoffMatchup.Team2Id,
                Team2Wins = localPlayoffMatchup.Team2Wins,
                Team2Placeholder = localPlayoffMatchup.Team2Placeholder,
                IsConfirmed = localPlayoffMatchup.IsConfirmed,
                HasEnded = localPlayoffMatchup.HasEnded,
                Description = localPlayoffMatchup.Description
            };

            _liveContext.PlayoffSeries.Update(matchup);
            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Adds a new head-to-head matchups.
        /// </summary>
        /// <param name="matchups">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddH2HMatchupsAsync(List<HeadToHeadSeries> matchups)
        {
            var exception = new InvalidOperationException("A matchup between the current teams already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.HeadToHeadSeries, matchups, exception);
        }

        /// <summary>
        ///     Adds new playoff rounds.
        /// </summary>
        /// <param name="rounds">The playoff rounds to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffRoundsAsync(List<PlayoffRound> rounds)
        {
            var exception = new InvalidOperationException("This round already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffRounds, rounds, exception);
        }

        /// <summary>
        ///     Adds a new playoff matchups.
        /// </summary>
        /// <param name="matchups">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffMatchupsAsync(List<PlayoffSeries> matchups)
        {
            var exception = new InvalidOperationException("The current matchup already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffSeries, matchups, exception);
        }

        /// <summary>
        ///     Updates a head-to-head matchup.
        /// </summary>
        /// <param name="localH2HSeries">The matchup to update.</param>
        /// <returns></returns>
        private async Task UpdateH2HSeriesAsync(HeadToHeadSeries localH2HSeries)
        {
            localH2HSeries.Season = null;
            localH2HSeries.Team1 = null;
            localH2HSeries.Team2 = null;
            
            _liveContext.HeadToHeadSeries.Update(localH2HSeries);
            await _liveContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Gets a head-to-head matchup from the local database.
        /// </summary>
        /// <param name="season"></param>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        /// <returns>The specified head-to-head matchup.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified matchup could not be found.</exception>
        private async Task<HeadToHeadSeries> GetLocalH2HSeriesAsync(int season, Team team1, Team team2)
        {
            return await _localContext.HeadToHeadSeries
                .Include(m => m.Season)
                .Include(m => m.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Season.Year == season &&
                                     ((m.Team1 == team1 && m.Team2 == team2) ||
                                      (m.Team1 == team2 && m.Team2 == team1))) ??
            throw new InvalidOperationException("Head-to-head series could not be found.");
        }

        /// <summary>
        ///     Gets a playoff series from the local database.
        /// </summary>
        /// <param name="season"></param>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        /// <returns>The specified playoff series from the local database.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified playoff series could not be found.</exception>
        private async Task<PlayoffSeries> GetLocalPlayoffSeriesAsync(int season, Team team1, Team team2)
        {
            return await _localContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Season.Year == season &&
                                          ((s.Team1 == team1 && s.Team2 == team2) ||
                                           (s.Team1 == team2 && s.Team2 == team1))) ??
            throw new InvalidOperationException("Playoff series could not be found.");
        }

        /// <summary>
        ///     Updates the standings in the live database according to the local standings.
        /// </summary>
        /// <param name="season"></param>
        /// <returns></returns>
        private async Task UpdateStandingsAsync(int season)
        {
            // Get the updated standings from the local database
            var localStandings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.LeagueRanking)
                .AsNoTracking()
                .ToList();

            // Clear the changes from the change tracker in the live database
            _liveContext.ChangeTracker.Clear();

            // Update each team's stats in order of their league ranking
            foreach (var team in localStandings)
            {
                team.Season = null;
                team.Conference = null;
                team.Division = null;
                team.Team = null;
                
                _liveContext.Standings.Update(team);
            }

            await _liveContext.SaveChangesAsync();
        }
    }
}
