using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
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
        ///     Saves any changes made to both the local and live databases.
        /// </summary>
        /// <returns></returns>
        private async Task SaveChangesAsync()
        {
            await _localContext.SaveChangesAsync();
            await _liveContext.SaveChangesAsync();
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
            bool recordExists = table.Any(r => r == record);
            
            // If the record DOES NOT exist in the table, add it to the table
            if (!recordExists)
                table.Add(record);
            
            // If the record does exist, throw the specified exception
            else
                throw exception;

            await SaveChangesAsync();
        }

        /// <summary>
        ///     Add a list of records to a table.
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

            await SaveChangesAsync();
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

        public async Task NewChampionAsync(NewChampionPackage package)
        {
            var exception = new InvalidOperationException($"The {package.Champion.Season.Year} {package.Champion.Team.FullName} are already in the Champions table.");
            await AddRecordToTableAsync(_liveContext.Champions, package.Champion, exception);

            exception = new InvalidOperationException($"This opponent is already in the database.");
            await AddRecordsToTableAsync(_liveContext.ChampionsRounds, package.Rounds, exception);
        }

        /// <summary>
        ///     Write one game result to the Schedule table.
        /// </summary>
        /// <param name="game">The game to record the results of.</param>
        /// <returns></returns>
        public async Task WriteOneResultAsync(Game game)
        {
            // Get the season and teams
            int season = game!.Season.Year;
            Team awayTeam = game.AwayTeam;
            Team homeTeam = game.HomeTeam;

            // Wipe the navigation properties for the purpose of EF tracking
            game.Season = null;
            game.PlayoffRound = null;
            game.PlayoffSeries = null;
            game.AwayTeam = null;
            game.HomeTeam = null;

            // If the game already exists, update it
            bool gameExistsInLiveDB = _liveContext.Schedule.Any(s => s.Id == game.Id);
            if (gameExistsInLiveDB)
                _liveContext.Schedule.Update(game);
            
            // If not, add it to the Schedule table
            else
                _liveContext.Schedule.Add(game);

            // If it is a playoff game...
            if (game.Type == PLAYOFFS)
            {
                // Update the live playoff series using the local playoff series
                var playoffSeries = await GetLocalPlayoffSeriesAsync(season, awayTeam, homeTeam);
                await UpdatePlayoffSeriesAsync(playoffSeries);
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

            await SaveChangesAsync();
        }

        /// <summary>
        ///     Write many results to the Schedule table.
        /// </summary>
        /// <param name="games">The results to write to the Schedule table.</param>
        /// <returns></returns>
        public async Task WriteManyResultsAsync(List<Game> games)
        {
            // Write each result to the table one at a time
            foreach (var game in games)
                await WriteOneResultAsync(game);

            await SaveChangesAsync();
        }

        /// <summary>
        ///     Add one game to the Schedule table.
        /// </summary>
        /// <param name="game">The game to add to the Schedule table.</param>
        /// <returns></returns>
        public async Task AddOneGameAsync(Game game)
        {
            var exception = new InvalidOperationException($"{game.Date:yyyy-MM-dd} - {game.AwayTeam.Code} @ {game.HomeTeam.Code} already exists in the database.");
            await AddRecordToTableAsync(_liveContext.Schedule, game, exception);
        }

        /// <summary>
        ///     Add many games to the Schedule table.
        /// </summary>
        /// <param name="schedule">The games to add to the Schedule table.</param>
        /// <returns></returns>
        public async Task AddManyGamesAsync(List<Game> schedule)
        {
            var exception = new InvalidOperationException($"This game already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.Schedule, schedule, exception);
        }

        /// <summary>
        ///     Delete a game from the Schedule table. <strong>Note:</strong> This can only be done if the game has not 
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

            await SaveChangesAsync();
        }

        /// <summary>
        ///     Delete many games from the Schedule table. <strong>Note:</strong> Games can only be deleted if they have 
        ///     not been confirmed yet.
        /// </summary>
        /// <param name="games">The games to be deleted.</param>
        /// <returns></returns>
        public async Task DeleteManyGamesAsync(List<Game> games)
        {
            // Delete each game one at a time.
            foreach (var game in games)
                await DeleteOneGameAsync(game);

            await SaveChangesAsync();
        }

        /// <summary>
        ///     Add new alignment records.
        /// </summary>
        /// <param name="alignments">The alignment records to add.</param>
        /// <returns></returns>
        public async Task AddAlignmentRecordsAsync(List<Alignment> alignments)
        {
            var exception = new InvalidOperationException("The current team already has an alignment for this season.");
            await AddRecordsToTableAsync(_liveContext.Alignments, alignments, exception);
        }

        /// <summary>
        ///     Add new standings records.
        /// </summary>
        /// <param name="standings">The standings records to add.</param>
        /// <returns></returns>
        public async Task AddStandingsRecordsAsync(List<Standings> standings)
        {
            var exception = new InvalidOperationException("The current team already has a standings record for the current season.");
            await AddRecordsToTableAsync(_liveContext.Standings, standings, exception);
        }

        /// <summary>
        ///     Add new head-to-head matchups.
        /// </summary>
        /// <param name="matchups">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddH2HMatchupsAsync(List<HeadToHeadSeries> matchups)
        {
            var exception = new InvalidOperationException("A matchup between the current teams already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.HeadToHeadSeries, matchups, exception);
        }

        /// <summary>
        ///     Add new playoff rounds.
        /// </summary>
        /// <param name="rounds">The playoff rounds to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffRoundsAsync(List<PlayoffRound> rounds)
        {
            var exception = new InvalidOperationException("This round already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffRounds, rounds, exception);
        }

        /// <summary>
        ///     Add new playoff matchups.
        /// </summary>
        /// <param name="matchups">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffMatchupsAsync(List<PlayoffSeries> matchups)
        {
            var exception = new InvalidOperationException("The current matchup already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffSeries, matchups, exception);
        }

        /// <summary>
        ///     Update a head-to-head matchup.
        /// </summary>
        /// <param name="localH2HSeries">The matchup to update.</param>
        /// <returns></returns>
        private async Task UpdateH2HSeriesAsync(HeadToHeadSeries localH2HSeries)
        {
            var series = _liveContext.HeadToHeadSeries
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == localH2HSeries.Id);

            series = localH2HSeries;
            _liveContext.HeadToHeadSeries.Update(series);
            await SaveChangesAsync();
        }

        /// <summary>
        ///     Get a head-to-head matchup from the local database.
        /// </summary>
        /// <param name="season"></param>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<HeadToHeadSeries> GetLocalH2HSeriesAsync(int season, Team team1, Team team2)
        {
            return await _localContext.HeadToHeadSeries
                .Include(s => s.Season)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Season.Year == season &&
                                     ((s.Team1 == team1 && s.Team2 == team2) ||
                                      (s.Team1 == team2 && s.Team2 == team1))) ??
            throw new InvalidOperationException("Head-to-head series could not be found.");
        }

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

        private async Task UpdateStandingsAsync(int season)
        {
            var localStandings = _localContext.Standings
                .Include(s => s.Season)
                .Include(s => s.Conference)
                .Include(s => s.Division)
                .Include(s => s.Team)
                .Where(s => s.Season.Year == season)
                .OrderBy(s => s.LeagueRanking)
                .AsNoTracking()
                .ToList();

            _liveContext.ChangeTracker.Clear();

            foreach (var team in localStandings)
            {
                team.Season = null;
                team.Conference = null;
                team.Division = null;
                team.Team = null;
                
                _liveContext.Standings.Update(team);
            }

            await SaveChangesAsync();
        }

        private async Task UpdatePlayoffSeriesAsync(PlayoffSeries localPlayoffSeries)
        {
            int season = localPlayoffSeries.Season.Year;
            Team team1 = localPlayoffSeries.Team1!;
            Team team2 = localPlayoffSeries.Team2!;

            var series = _liveContext.PlayoffSeries
                .Include(s => s.Season)
                .Include(s => s.Round)
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefault(s => s.Season.Year == season &&
                                     ((s.Team1 == team1 && s.Team2 == team2) ||
                                      (s.Team1 == team2 && s.Team2 == team1)));

            series = localPlayoffSeries;
            _liveContext.PlayoffSeries.Update(series);
            await SaveChangesAsync();
        }
    }
}
