using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using SycamoreHockeyLeaguePortal.Models.ConstantGroups;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages;

namespace SycamoreHockeyLeaguePortal.Services
{
    public class LiveDbSyncService
    {
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;

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
        public async Task NewSeasonAsync(DTP_NewSeason package)
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
        public async Task NewChampionAsync(DTP_NewChampion package)
        {
            // Add the new champion to the Champions table
            var exception = new InvalidOperationException($"The {package.Champion.Season.Year} {package.Champion.Team.FullName} are already in the Champions table.");
            await AddRecordToTableAsync(_liveContext.Champions, package.Champion, exception);

            // Add the opponents defeated by the champion to the ChampionsRounds table
            var rounds = package.Rounds
                .Select(r => new ChampionsRound
                {
                    Id = r.Id,
                    ChampionId = r.ChampionId,
                    RoundIndex = r.RoundIndex,
                    OpponentId = r.OpponentId,
                    SeriesLength = r.SeriesLength,
                    BestOf = r.BestOf
                }).ToList();
            exception = new InvalidOperationException($"This opponent is already in the database.");
            await AddRecordsToTableAsync(_liveContext.ChampionsRounds, rounds, exception);
        }

        /// <summary>
        ///     Writes one game result to the Schedule table.
        /// </summary>
        /// <param name="gameDTO">The game to record the results of.</param>
        /// <returns></returns>
        public async Task WriteOneResultAsync(DTO_Game gameDTO)
        {
            var game = _liveContext.Schedule.FirstOrDefault(s => s.Id == gameDTO.Id);
            if (game == null)
                throw new InvalidOperationException("The game could not be found in the live database.");

            game.AwayScore = gameDTO.AwayScore;
            game.HomeScore = gameDTO.HomeScore;
            game.Period = gameDTO.Period;
            game.IsLive = gameDTO.IsLive;
            game.IsFinalized = gameDTO.IsFinalized;
            await _liveContext.SaveChangesAsync();


            var season = await _liveContext.Seasons
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == game.SeasonId) ??
            throw new Exception("There was an error restoring the Season property.");

            var round = await _liveContext.PlayoffRounds
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == game.PlayoffRoundId);

            var series = await _liveContext.PlayoffSeries
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == game.PlayoffSeriesId);

            var awayTeam = await _liveContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == game.AwayTeamId) ??
            throw new Exception("There was an error restoring the AwayTeam property.");

            var homeTeam = await _liveContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == game.HomeTeamId) ??
            throw new Exception("There was an error restoring the HomeTeam property.");

            // If it is a playoff game...
            if (game.Type == GameTypes.PLAYOFFS)
            {
                if (series == null)
                    throw new Exception("The playoff series could not be found.");
                
                // Update the appropriate playoff series in the live database
                await UpdatePlayoffMatchupAsync(series);
            }   
            // If it is a regular season game or tiebreaker game...
            else
            {
                // Get the head-to-head matchup between the two teams
                var h2hSeries = await GetH2HSeriesDTOAsync(season.Year, awayTeam, homeTeam);
                await UpdateH2HSeriesAsync(h2hSeries);

                // If it is a regular season game, update the standings
                if (game.Type == GameTypes.REGULAR_SEASON)
                    await UpdateStandingsAsync(season.Year);
            }
        }

        /// <summary>
        ///     Writes many results to the Schedule table.
        /// </summary>
        /// <param name="games">The results to write to the Schedule table.</param>
        /// <returns></returns>
        public async Task WriteManyResultsAsync(List<DTO_Game> games)
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
        /// <param name="alignmentDTOs">The alignment records to add.</param>
        /// <returns></returns>
        public async Task AddAlignmentRecordsAsync(List<DTO_Alignment> alignmentDTOs)
        {
            var alignments = alignmentDTOs.Select(a => new Alignment
            {
                Id = a.Id,
                SeasonId = a.SeasonId,
                ConferenceId = a.ConferenceId,
                DivisionId = a.DivisionId,
                TeamId = a.TeamId
            }).ToList();
            
            var exception = new InvalidOperationException("The current team already has an alignment for this season.");
            await AddRecordsToTableAsync(_liveContext.Alignments, alignments, exception);
        }

        /// <summary>
        ///     Adds new standings records.
        /// </summary>
        /// <param name="standingsDTOs">The standings records to add.</param>
        /// <returns></returns>
        public async Task AddStandingsRecordsAsync(List<DTO_Standings> standingsDTOs)
        {
            var standings = standingsDTOs.Select(s => new Standings
            {
                Id = s.Id,
                SeasonId = s.SeasonId,
                ConferenceId = s.ConferenceId,
                DivisionId = s.DivisionId,
                DivisionRanking = s.DivisionRanking,
                ConferenceRanking = s.ConferenceRanking,
                PlayoffRanking = s.PlayoffRanking,
                LeagueRanking = s.LeagueRanking,
                TeamId = s.TeamId,
                PlayoffStatus = s.PlayoffStatus,
                GamesPlayed = s.GamesPlayed,
                Wins = s.Wins,
                Losses = s.Losses,
                OTLosses = s.OTLosses,
                RegulationWins = s.RegulationWins,
                RegPlusOTWins = s.RegPlusOTWins,
                Points = s.Points,
                MaximumPossiblePoints = s.MaximumPossiblePoints,
                WinPct = s.WinPct,
                PointsPct = s.PointsPct,
                DivisionGamesBehind = s.DivisionGamesBehind,
                ConferenceGamesBehind = s.ConferenceGamesBehind,
                LeagueGamesBehind = s.LeagueGamesBehind,
                PlayoffsGamesBehind = s.PlayoffsGamesBehind,
                GoalsFor = s.GoalsFor,
                GoalsAgainst = s.GoalsAgainst,
                GoalDifferential = s.GoalDifferential,
                Streak = s.Streak,
                StreakGoalsFor = s.StreakGoalsFor,
                StreakGoalsAgainst = s.StreakGoalsAgainst,
                StreakGoalDifferential = s.StreakGoalDifferential,
                GamesPlayedVsDivision = s.GamesPlayedVsDivision,
                WinsVsDivision = s.WinsVsDivision,
                LossesVsDivision = s.LossesVsDivision,
                OTLossesVsDivision = s.OTLossesVsDivision,
                WinPctVsDivision = s.WinPctVsDivision,
                GamesPlayedVsConference = s.GamesPlayedVsConference,
                WinsVsConference = s.WinsVsConference,
                LossesVsConference = s.LossesVsConference,
                OTLossesVsConference = s.OTLossesVsConference,
                WinPctVsConference = s.WinPctVsConference,
                InterConfGamesPlayed = s.InterConfGamesPlayed,
                InterConfWins = s.InterConfWins,
                InterConfLosses = s.InterConfLosses,
                InterConfOTLosses = s.InterConfOTLosses,
                InterConfWinPct = s.InterConfWinPct,
                GamesPlayedInLast10Games = s.GamesPlayedInLast10Games,
                WinsInLast10Games = s.WinsInLast10Games,
                LossesInLast10Games = s.LossesInLast10Games,
                WinPctInLast10Games = s.WinPctInLast10Games,
                NextGameId = s.NextGameId,
            }).ToList();
            
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
        /// <param name="matchupDTOs">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddH2HMatchupsAsync(List<DTO_HeadToHeadSeries> matchupDTOs)
        {
            var matchups = matchupDTOs.Select(m => new HeadToHeadSeries
            {
                Id = m.Id,
                SeasonId = m.SeasonId,
                Team1Id = m.Team1Id,
                Team1Wins = m.Team1Wins,
                Team1GoalsFor = m.Team1GoalsFor,
                Team2Id = m.Team2Id,
                Team2Wins = m.Team2Wins,
                Team2GoalsFor = m.Team2GoalsFor
            }).ToList();
            
            var exception = new InvalidOperationException("A matchup between the current teams already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.HeadToHeadSeries, matchups, exception);
        }

        /// <summary>
        ///     Adds new playoff rounds.
        /// </summary>
        /// <param name="roundDTOs">The playoff rounds to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffRoundsAsync(List<DTO_PlayoffRound> roundDTOs)
        {
            var rounds = roundDTOs.Select(r => new PlayoffRound
            {
                Id = r.Id,
                SeasonId = r.SeasonId,
                Index = r.Index,
                Name = r.Name,
                MatchupsConfirmed = r.MatchupsConfirmed
            }).ToList();
            
            var exception = new InvalidOperationException("This round already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffRounds, rounds, exception);
        }

        /// <summary>
        ///     Adds a new playoff matchups.
        /// </summary>
        /// <param name="matchupDTOs">The matchups to add.</param>
        /// <returns></returns>
        private async Task AddPlayoffMatchupsAsync(List<DTO_PlayoffSeries> matchupDTOs)
        {
            var matchups = matchupDTOs.Select(m => new PlayoffSeries
            {
                Id = m.Id,
                SeasonId = m.SeasonId,
                RoundId = m.RoundId,
                Team1Id = m.Team1Id,
                Team1Wins = m.Team1Wins,
                Team1Placeholder = m.Team1Placeholder,
                Team2Id = m.Team2Id,
                Team2Wins = m.Team2Wins,
                Team2Placeholder = m.Team2Placeholder,
                Index = m.Index,
                Description = m.Description,
                IsConfirmed = m.IsConfirmed,
                HasEnded = m.HasEnded
            }).ToList();
            
            var exception = new InvalidOperationException("The current matchup already exists in the database.");
            await AddRecordsToTableAsync(_liveContext.PlayoffSeries, matchups, exception);
        }

        /// <summary>
        ///     Updates a head-to-head matchup.
        /// </summary>
        /// <param name="matchupDTO">The matchup to update.</param>
        /// <returns></returns>
        private async Task UpdateH2HSeriesAsync(DTO_HeadToHeadSeries matchupDTO)
        {
            var matchup = _liveContext.HeadToHeadSeries.FirstOrDefault(m => m.Id == matchupDTO.Id)!;

            matchup.Team1Wins = matchupDTO.Team1Wins;
            matchup.Team1GoalsFor = matchupDTO.Team1GoalsFor;
            matchup.Team2Wins = matchupDTO.Team2Wins;
            matchup.Team2GoalsFor = matchupDTO.Team2GoalsFor;
            
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
        private async Task<DTO_HeadToHeadSeries> GetH2HSeriesDTOAsync(int season, Team team1, Team team2)
        {
            var matchup = await _localContext.HeadToHeadSeries
                .Include(m => m.Season)
                .Include(m => m.Team1)
                .Include(s => s.Team2)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Season.Year == season &&
                                     ((m.Team1 == team1 && m.Team2 == team2) ||
                                      (m.Team1 == team2 && m.Team2 == team1))) ??
            throw new InvalidOperationException("Head-to-head series could not be found.");

            return new DTO_HeadToHeadSeries
            {
                Id = matchup.Id,
                SeasonId = matchup.SeasonId,
                Team1Id = matchup.Team1Id,
                Team1Wins = matchup.Team1Wins,
                Team1GoalsFor = matchup.Team1GoalsFor,
                Team2Id = matchup.Team2Id,
                Team2Wins = matchup.Team2Wins,
                Team2GoalsFor = matchup.Team2GoalsFor
            };
        }
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
