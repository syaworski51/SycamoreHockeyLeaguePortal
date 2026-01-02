using Humanizer;
using Microsoft.Identity.Client;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels
{
    public class DTOConverter
    {
        public DTO_Alignment ConvertToDTO(Alignment alignment) => new()
        {
            Id = alignment.Id,
            SeasonId = alignment.SeasonId,
            ConferenceId = alignment.ConferenceId,
            DivisionId = alignment.DivisionId,
            TeamId = alignment.TeamId
        };

        public DTO_Champion ConvertToDTO(Champion champion) => new()
        {
            Id = champion.Id,
            SeasonId = champion.SeasonId,
            TeamId = champion.TeamId
        };

        public DTO_ChampionsRound ConvertToDTO(ChampionsRound round) => new()
        {
            Id = round.Id,
            ChampionId = round.ChampionId,
            RoundIndex = round.RoundIndex,
            OpponentId = round.OpponentId,
            SeriesLength = round.SeriesLength,
            BestOf = round.BestOf
        };

        public DTO_Game ConvertToDTO(Game game) => new()
        {
            Id = game.Id,
            SeasonId = game.SeasonId,
            Date = game.Date,
            GameIndex = game.GameIndex,
            Type = game.Type,
            PlayoffRoundId = game.PlayoffRoundId,
            PlayoffSeriesId = game.PlayoffSeriesId,
            PlayoffGameIndex = game.PlayoffGameIndex,
            PlayoffSeriesScore = game.PlayoffSeriesScore,
            AwayTeamId = game.AwayTeamId,
            AwayScore = game.AwayScore,
            HomeTeamId = game.HomeTeamId,
            HomeScore = game.HomeScore,
            Period = game.Period,
            IsConfirmed = game.IsConfirmed,
            Notes = game.Notes,
            LiveStatus = game.LiveStatus
        };

        public DTO_HeadToHeadSeries ConvertToDTO(HeadToHeadSeries matchup) => new()
        {
            Id = matchup.Id,
            SeasonId = matchup.SeasonId,
            Team1Id = matchup.Team1Id,
            Team1Wins = matchup.Team1Wins,
            Team1OTWins = matchup.Team1OTWins,
            Team1Points = matchup.Team1Points,
            Team1ROWs = matchup.Team1ROWs,
            Team1GoalsFor = matchup.Team1GoalsFor,
            Team2Id = matchup.Team2Id,
            Team2Wins = matchup.Team2Wins,
            Team2OTWins = matchup.Team2OTWins,
            Team2Points = matchup.Team2Points,
            Team2ROWs = matchup.Team2ROWs,
            Team2GoalsFor = matchup.Team2GoalsFor
        };

        public DTO_Standings ConvertToDTO(Standings teamStats) => new()
        {
            Id = teamStats.Id,
            SeasonId = teamStats.SeasonId,
            ConferenceId = teamStats.ConferenceId,
            DivisionId = teamStats.DivisionId,
            DivisionRanking = teamStats.DivisionRanking,
            ConferenceRanking = teamStats.ConferenceRanking,
            PlayoffRanking = teamStats.PlayoffRanking,
            LeagueRanking = teamStats.LeagueRanking,
            TeamId = teamStats.TeamId,
            PlayoffStatus = teamStats.PlayoffStatus,
            GamesPlayed = teamStats.GamesPlayed,
            Wins = teamStats.Wins,
            OTWins = teamStats.OTWins,
            OTLosses = teamStats.OTLosses,
            Losses = teamStats.Losses,
            RegulationWins = teamStats.RegulationWins,
            RegPlusOTWins = teamStats.RegPlusOTWins,
            Points = teamStats.Points,
            PointsCeiling = teamStats.PointsCeiling,
            WinPct = teamStats.WinPct,
            PointsPct = teamStats.PointsPct,
            DivisionGamesBehind = teamStats.DivisionGamesBehind,
            ConferenceGamesBehind = teamStats.ConferenceGamesBehind,
            LeagueGamesBehind = teamStats.LeagueGamesBehind,
            PlayoffsGamesBehind = teamStats.PlayoffsGamesBehind,
            GoalsFor = teamStats.GoalsFor,
            GoalsAgainst = teamStats.GoalsAgainst,
            GoalDifferential = teamStats.GoalDifferential,
            Streak = teamStats.Streak,
            StreakGoalsFor = teamStats.StreakGoalsFor,
            StreakGoalsAgainst = teamStats.StreakGoalsAgainst,
            StreakGoalDifferential = teamStats.StreakGoalDifferential,
            GamesPlayedVsDivision = teamStats.GamesPlayedVsDivision,
            WinsVsDivision = teamStats.WinsVsDivision,
            LossesVsDivision = teamStats.LossesVsDivision,
            OTLossesVsDivision = teamStats.OTLossesVsDivision,
            WinPctVsDivision = teamStats.WinPctVsDivision,
            GamesPlayedVsConference = teamStats.GamesPlayedVsConference,
            WinsVsConference = teamStats.WinsVsConference,
            OTWinsVsConference = teamStats.OTWinsVsConference,
            OTLossesVsConference = teamStats.OTLossesVsConference,
            LossesVsConference = teamStats.LossesVsConference,
            PointsVsConference = teamStats.PointsVsConference,
            WinPctVsConference = teamStats.WinPctVsConference,
            InterConfGamesPlayed = teamStats.InterConfGamesPlayed,
            InterConfWins = teamStats.InterConfWins,
            InterConfOTWins = teamStats.InterConfOTWins,
            InterConfOTLosses = teamStats.InterConfOTLosses,
            InterConfLosses = teamStats.InterConfLosses,
            InterConfPoints = teamStats.InterConfPoints,
            InterConfWinPct = teamStats.InterConfWinPct,
            GamesPlayedInLast10Games = teamStats.GamesPlayedInLast10Games,
            WinsInLast10Games = teamStats.WinsInLast10Games,
            OTWinsInLast10Games = teamStats.OTWinsInLast10Games,
            OTLossesInLast10Games = teamStats.OTLossesInLast10Games,
            LossesInLast10Games = teamStats.LossesInLast10Games,
            PointsInLast10Games = teamStats.PointsInLast10Games,
            WinPctInLast10Games = teamStats.WinPctInLast10Games
        };


        public List<DTO_Alignment> ConvertBatchToDTO(List<Alignment> alignments) =>
            alignments.Select(g => new DTO_Alignment
            {
                Id = g.Id,
                SeasonId = g.SeasonId,
                ConferenceId = g.ConferenceId,
                DivisionId = g.DivisionId,
                TeamId = g.TeamId
            }).ToList();

        public List<DTO_Champion> ConvertBatchToDTO(List<Champion> champions) =>
            champions.Select(c => new DTO_Champion
            {
                Id = c.Id,
                SeasonId = c.SeasonId,
                TeamId = c.TeamId
            }).ToList();

        public List<DTO_ChampionsRound> ConvertBatchToDTO(List<ChampionsRound> rounds) =>
            rounds.Select(r => new DTO_ChampionsRound
            {
                Id = r.Id,
                ChampionId = r.ChampionId,
                RoundIndex = r.RoundIndex,
                OpponentId = r.OpponentId,
                SeriesLength = r.SeriesLength,
                BestOf = r.BestOf
            }).ToList();
        
        public List<DTO_Game> ConvertBatchToDTO(List<Game> games) =>
            games.Select(g => new DTO_Game
            {
                Id = g.Id,
                SeasonId = g.SeasonId,
                Date = g.Date,
                GameIndex = g.GameIndex,
                Type = g.Type,
                PlayoffRoundId = g.PlayoffRoundId,
                PlayoffSeriesId = g.PlayoffSeriesId,
                PlayoffGameIndex = g.PlayoffGameIndex,
                PlayoffSeriesScore = g.PlayoffSeriesScore,
                AwayTeamId = g.AwayTeamId,
                AwayScore = g.AwayScore,
                HomeTeamId = g.HomeTeamId,
                HomeScore = g.HomeScore,
                Period = g.Period,
                IsConfirmed = g.IsConfirmed,
                LiveStatus = g.LiveStatus,
                Notes = g.Notes
            }).ToList();

        public List<DTO_HeadToHeadSeries> ConvertBatchToDTO(List<HeadToHeadSeries> matchups) =>
            matchups.Select(m => new DTO_HeadToHeadSeries
            {
                Id = m.Id,
                SeasonId = m.SeasonId,
                Team1Id = m.Team1Id,
                Team1Wins = m.Team1Wins,
                Team1OTWins = m.Team1OTWins,
                Team1Points = m.Team1Points,
                Team1ROWs = m.Team1ROWs,
                Team1GoalsFor = m.Team1GoalsFor,
                Team2Id = m.Team2Id,
                Team2Wins = m.Team2Wins,
                Team2OTWins = m.Team2OTWins,
                Team2Points = m.Team2Points,
                Team2ROWs = m.Team2ROWs,
                Team2GoalsFor = m.Team2GoalsFor
            }).ToList();

        public List<DTO_Standings> ConvertBatchToDTO(List<Standings> teamStats) =>
            teamStats.Select(s => new DTO_Standings
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
                OTWins = s.OTWins,
                OTLosses = s.OTLosses,
                Losses = s.Losses,
                RegulationWins = s.RegulationWins,
                RegPlusOTWins = s.RegPlusOTWins,
                Points = s.Points,
                PointsCeiling = s.PointsCeiling,
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
                OTWinsVsConference = s.OTWinsVsConference,
                OTLossesVsConference = s.OTLossesVsConference,
                LossesVsConference = s.LossesVsConference,
                PointsVsConference = s.PointsVsConference,
                WinPctVsConference = s.WinPctVsConference,
                InterConfGamesPlayed = s.InterConfGamesPlayed,
                InterConfWins = s.InterConfWins,
                InterConfOTWins = s.InterConfOTWins,
                InterConfOTLosses = s.InterConfOTLosses,
                InterConfLosses = s.InterConfLosses,
                InterConfPoints = s.InterConfPoints,
                InterConfWinPct = s.InterConfWinPct,
                GamesPlayedInLast10Games = s.GamesPlayedInLast10Games,
                WinsInLast10Games = s.WinsInLast10Games,
                OTWinsInLast10Games = s.OTWinsInLast10Games,
                OTLossesInLast10Games = s.OTLossesInLast10Games,
                LossesInLast10Games = s.LossesInLast10Games,
                PointsInLast10Games = s.PointsInLast10Games,
                WinPctInLast10Games = s.WinPctInLast10Games
            }).ToList();

        
        
        public Alignment ConvertToStandardModel(DTO_Alignment alignmentDTO) => new()
        {
            Id = alignmentDTO.Id,
            SeasonId = alignmentDTO.SeasonId,
            ConferenceId = alignmentDTO.ConferenceId,
            DivisionId = alignmentDTO.DivisionId,
            TeamId = alignmentDTO.TeamId
        };

        public Champion ConvertToStandardModel(DTO_Champion championDTO) => new()
        {
            Id = championDTO.Id,
            SeasonId = championDTO.SeasonId,
            TeamId = championDTO.TeamId
        };

        public ChampionsRound ConvertToStandardModel(DTO_ChampionsRound roundDTO) => new()
        {
            Id = roundDTO.Id,
            ChampionId = roundDTO.ChampionId,
            RoundIndex = roundDTO.RoundIndex,
            OpponentId = roundDTO.OpponentId,
            SeriesLength = roundDTO.SeriesLength,
            BestOf = roundDTO.BestOf
        };
        
        public Game ConvertToStandardModel(DTO_Game gameDTO) => new()
        {
            Id = gameDTO.Id,
            SeasonId = gameDTO.SeasonId,
            Date = gameDTO.Date,
            GameIndex = gameDTO.GameIndex,
            Type = gameDTO.Type,
            PlayoffRoundId = gameDTO.PlayoffRoundId,
            PlayoffSeriesId = gameDTO.PlayoffSeriesId,
            PlayoffGameIndex = gameDTO.PlayoffGameIndex,
            PlayoffSeriesScore = gameDTO.PlayoffSeriesScore,
            AwayTeamId = gameDTO.AwayTeamId,
            AwayScore = gameDTO.AwayScore,
            HomeTeamId = gameDTO.HomeTeamId,
            HomeScore = gameDTO.HomeScore,
            Period = gameDTO.Period,
            IsConfirmed = gameDTO.IsConfirmed,
            LiveStatus = gameDTO.LiveStatus,
            Notes = gameDTO.Notes
        };

        public HeadToHeadSeries ConvertToStandardModel(DTO_HeadToHeadSeries matchupDTO) => new()
        {
            Id = matchupDTO.Id,
            SeasonId = matchupDTO.SeasonId,
            Team1Id = matchupDTO.Team1Id,
            Team1Wins = matchupDTO.Team1Wins,
            Team1OTWins = matchupDTO.Team1OTWins,
            Team1Points = matchupDTO.Team1Points,
            Team1ROWs = matchupDTO.Team1ROWs,
            Team1GoalsFor = matchupDTO.Team1GoalsFor,
            Team2Id = matchupDTO.Team2Id,
            Team2Wins = matchupDTO.Team2Wins,
            Team2OTWins = matchupDTO.Team2OTWins,
            Team2Points = matchupDTO.Team2Points,
            Team2ROWs = matchupDTO.Team2ROWs,
            Team2GoalsFor = matchupDTO.Team2GoalsFor
        };

        public Standings ConvertToStandardModel(DTO_Standings teamStatsDTO) => new()
        {
            Id = teamStatsDTO.Id,
            SeasonId = teamStatsDTO.SeasonId,
            ConferenceId = teamStatsDTO.ConferenceId,
            DivisionId = teamStatsDTO.DivisionId,
            DivisionRanking = teamStatsDTO.DivisionRanking,
            ConferenceRanking = teamStatsDTO.ConferenceRanking,
            PlayoffRanking = teamStatsDTO.PlayoffRanking,
            LeagueRanking = teamStatsDTO.LeagueRanking,
            TeamId = teamStatsDTO.TeamId,
            PlayoffStatus = teamStatsDTO.PlayoffStatus,
            GamesPlayed = teamStatsDTO.GamesPlayed,
            Wins = teamStatsDTO.Wins,
            OTWins = teamStatsDTO.OTWins,
            OTLosses = teamStatsDTO.OTLosses,
            Losses = teamStatsDTO.Losses,
            RegulationWins = teamStatsDTO.RegulationWins,
            RegPlusOTWins = teamStatsDTO.RegPlusOTWins,
            Points = teamStatsDTO.Points,
            PointsCeiling = teamStatsDTO.PointsCeiling,
            WinPct = teamStatsDTO.WinPct,
            PointsPct = teamStatsDTO.PointsPct,
            DivisionGamesBehind = teamStatsDTO.DivisionGamesBehind,
            ConferenceGamesBehind = teamStatsDTO.ConferenceGamesBehind,
            LeagueGamesBehind = teamStatsDTO.LeagueGamesBehind,
            PlayoffsGamesBehind = teamStatsDTO.PlayoffsGamesBehind,
            GoalsFor = teamStatsDTO.GoalsFor,
            GoalsAgainst = teamStatsDTO.GoalsAgainst,
            GoalDifferential = teamStatsDTO.GoalDifferential,
            Streak = teamStatsDTO.Streak,
            StreakGoalsFor = teamStatsDTO.StreakGoalsFor,
            StreakGoalsAgainst = teamStatsDTO.StreakGoalsAgainst,
            StreakGoalDifferential = teamStatsDTO.StreakGoalDifferential,
            GamesPlayedVsDivision = teamStatsDTO.GamesPlayedVsDivision,
            WinsVsDivision = teamStatsDTO.WinsVsDivision,
            LossesVsDivision = teamStatsDTO.LossesVsDivision,
            OTLossesVsDivision = teamStatsDTO.OTLossesVsDivision,
            WinPctVsDivision = teamStatsDTO.WinPctVsDivision,
            GamesPlayedVsConference = teamStatsDTO.GamesPlayedVsConference,
            WinsVsConference = teamStatsDTO.WinsVsConference,
            OTWinsVsConference = teamStatsDTO.OTWinsVsConference,
            OTLossesVsConference = teamStatsDTO.OTLossesVsConference,
            LossesVsConference = teamStatsDTO.LossesVsConference,
            PointsVsConference = teamStatsDTO.PointsVsConference,
            WinPctVsConference = teamStatsDTO.WinPctVsConference,
            InterConfGamesPlayed = teamStatsDTO.InterConfGamesPlayed,
            InterConfWins = teamStatsDTO.InterConfWins,
            InterConfOTWins = teamStatsDTO.InterConfOTWins,
            InterConfOTLosses = teamStatsDTO.InterConfOTLosses,
            InterConfLosses = teamStatsDTO.InterConfLosses,
            InterConfPoints = teamStatsDTO.InterConfPoints,
            InterConfWinPct = teamStatsDTO.InterConfWinPct,
            GamesPlayedInLast10Games = teamStatsDTO.GamesPlayedInLast10Games,
            WinsInLast10Games = teamStatsDTO.WinsInLast10Games,
            OTWinsInLast10Games = teamStatsDTO.OTWinsInLast10Games,
            OTLossesInLast10Games = teamStatsDTO.OTLossesInLast10Games,
            LossesInLast10Games = teamStatsDTO.LossesInLast10Games,
            PointsInLast10Games = teamStatsDTO.PointsInLast10Games,
            WinPctInLast10Games = teamStatsDTO.WinPctInLast10Games
        };


        public List<Alignment> ConvertBatchToStandardModel(List<DTO_Alignment> DTOs) =>
            DTOs.Select(dto => new Alignment
            {
                Id = dto.Id,
                SeasonId = dto.SeasonId,
                ConferenceId = dto.ConferenceId,
                DivisionId = dto.DivisionId,
                TeamId = dto.TeamId
            }).ToList();

        public List<Champion> ConvertBatchToStandardModel(List<DTO_Champion> DTOs) =>
            DTOs.Select(dto => new Champion
            {
                Id = dto.Id,
                SeasonId = dto.SeasonId,
                TeamId = dto.TeamId
            }).ToList();

        public List<ChampionsRound> ConvertBatchToStandardModel(List<DTO_ChampionsRound> DTOs) =>
            DTOs.Select(dto => new ChampionsRound
            {
                Id = dto.Id,
                ChampionId = dto.ChampionId,
                RoundIndex = dto.RoundIndex,
                OpponentId = dto.OpponentId,
                SeriesLength = dto.SeriesLength,
                BestOf = dto.BestOf
            }).ToList();

        public List<Game> ConvertBatchToStandardModel(List<DTO_Game> DTOs) =>
            DTOs.Select(g => new Game
            {
                Id = g.Id,
                SeasonId = g.SeasonId,
                Date = g.Date,
                GameIndex = g.GameIndex,
                Type = g.Type,
                PlayoffRoundId = g.PlayoffRoundId,
                PlayoffSeriesId = g.PlayoffSeriesId,
                PlayoffGameIndex = g.PlayoffGameIndex,
                PlayoffSeriesScore = g.PlayoffSeriesScore,
                AwayTeamId = g.AwayTeamId,
                AwayScore = g.AwayScore,
                HomeTeamId = g.HomeTeamId,
                HomeScore = g.HomeScore,
                Period = g.Period,
                IsConfirmed = g.IsConfirmed,
                LiveStatus = g.LiveStatus,
                Notes = g.Notes
            }).ToList();

        public List<HeadToHeadSeries> ConvertBatchToStandardModel(List<DTO_HeadToHeadSeries> DTOs) =>
            DTOs.Select(dto => new HeadToHeadSeries
            {
                Id = dto.Id,
                SeasonId = dto.SeasonId,
                Team1Id = dto.Team1Id,
                Team1Wins = dto.Team1Wins,
                Team1OTWins = dto.Team1OTWins,
                Team1Points = dto.Team1Points,
                Team1ROWs = dto.Team1ROWs,
                Team1GoalsFor = dto.Team1GoalsFor,
                Team2Id = dto.Team2Id,
                Team2Wins = dto.Team2Wins,
                Team2OTWins = dto.Team2OTWins,
                Team2Points = dto.Team2Points,
                Team2ROWs = dto.Team2ROWs,
                Team2GoalsFor = dto.Team2GoalsFor
            }).ToList();

        public List<Standings> ConvertBatchToStandardModel(List<DTO_Standings> DTOs) =>
            DTOs.Select(dto => new Standings
            {
                Id = dto.Id,
                SeasonId = dto.SeasonId,
                ConferenceId = dto.ConferenceId,
                DivisionId = dto.DivisionId,
                DivisionRanking = dto.DivisionRanking,
                ConferenceRanking = dto.ConferenceRanking,
                PlayoffRanking = dto.PlayoffRanking,
                LeagueRanking = dto.LeagueRanking,
                TeamId = dto.TeamId,
                PlayoffStatus = dto.PlayoffStatus,
                GamesPlayed = dto.GamesPlayed,
                Wins = dto.Wins,
                OTWins = dto.OTWins,
                OTLosses = dto.OTLosses,
                Losses = dto.Losses,
                RegulationWins = dto.RegulationWins,
                RegPlusOTWins = dto.RegPlusOTWins,
                Points = dto.Points,
                PointsCeiling = dto.PointsCeiling,
                WinPct = dto.WinPct,
                PointsPct = dto.PointsPct,
                DivisionGamesBehind = dto.DivisionGamesBehind,
                ConferenceGamesBehind = dto.ConferenceGamesBehind,
                LeagueGamesBehind = dto.LeagueGamesBehind,
                PlayoffsGamesBehind = dto.PlayoffsGamesBehind,
                GoalsFor = dto.GoalsFor,
                GoalsAgainst = dto.GoalsAgainst,
                GoalDifferential = dto.GoalDifferential,
                Streak = dto.Streak,
                StreakGoalsFor = dto.StreakGoalsFor,
                StreakGoalsAgainst = dto.StreakGoalsAgainst,
                StreakGoalDifferential = dto.StreakGoalDifferential,
                GamesPlayedVsDivision = dto.GamesPlayedVsDivision,
                WinsVsDivision = dto.WinsVsDivision,
                LossesVsDivision = dto.LossesVsDivision,
                OTLossesVsDivision = dto.OTLossesVsDivision,
                WinPctVsDivision = dto.WinPctVsDivision,
                GamesPlayedVsConference = dto.GamesPlayedVsConference,
                WinsVsConference = dto.WinsVsConference,
                OTWinsVsConference = dto.OTWinsVsConference,
                OTLossesVsConference = dto.OTLossesVsConference,
                LossesVsConference = dto.LossesVsConference,
                PointsVsConference = dto.PointsVsConference,
                WinPctVsConference = dto.WinPctVsConference,
                InterConfGamesPlayed = dto.InterConfGamesPlayed,
                InterConfWins = dto.InterConfWins,
                InterConfOTWins = dto.InterConfOTWins,
                InterConfOTLosses = dto.InterConfOTLosses,
                InterConfLosses = dto.InterConfLosses,
                InterConfPoints = dto.InterConfPoints,
                InterConfWinPct = dto.InterConfWinPct,
                GamesPlayedInLast10Games = dto.GamesPlayedInLast10Games,
                WinsInLast10Games = dto.WinsInLast10Games,
                OTWinsInLast10Games = dto.OTWinsInLast10Games,
                OTLossesInLast10Games = dto.OTLossesInLast10Games,
                LossesInLast10Games = dto.LossesInLast10Games,
                PointsInLast10Games = dto.PointsInLast10Games,
                WinPctInLast10Games = dto.WinPctInLast10Games
            }).ToList();
    }
}
