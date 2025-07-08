using Microsoft.Identity.Client;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels
{
    public class DTOConverter
    {
        private readonly ApplicationDbContext _localContext;
        private readonly LiveDbContext _liveContext;

        public DTOConverter(ApplicationDbContext local, LiveDbContext live)
        {
            _localContext = local;
            _liveContext = live;
        }


        public DTO_Alignment ConvertToDTO(Alignment alignment) => new()
        {
            Id = alignment.Id,
            SeasonId = alignment.SeasonId,
            ConferenceId = alignment.ConferenceId,
            DivisionId = alignment.DivisionId,
            TeamId = alignment.TeamId
        };

        public DTO_Standings ConvertToDTO(Standings standings) => new()
        {
            Id = standings.Id,
            SeasonId = standings.SeasonId,
            ConferenceId = standings.ConferenceId,
            DivisionId = standings.DivisionId,
            DivisionRanking = standings.DivisionRanking,
            ConferenceRanking = standings.ConferenceRanking,
            PlayoffRanking = standings.PlayoffRanking,
            LeagueRanking = standings.LeagueRanking,
            TeamId = standings.TeamId,
            PlayoffStatus = standings.PlayoffStatus,
            GamesPlayed = standings.GamesPlayed,
            Wins = standings.Wins,
            Losses = standings.Losses,
            OTLosses = standings.OTLosses,
            RegulationWins = standings.RegulationWins,
            RegPlusOTWins = standings.RegPlusOTWins,
            Points = standings.Points,
            MaximumPossiblePoints = standings.MaximumPossiblePoints,
            WinPct = standings.WinPct,
            PointsPct = standings.PointsPct,
            DivisionGamesBehind = standings.DivisionGamesBehind,
            ConferenceGamesBehind = standings.ConferenceGamesBehind,
            LeagueGamesBehind = standings.LeagueGamesBehind,
            PlayoffsGamesBehind = standings.PlayoffsGamesBehind,
            GoalsFor = standings.GoalsFor,
            GoalsAgainst = standings.GoalsAgainst,
            GoalDifferential = standings.GoalDifferential,
            Streak = standings.Streak,
            StreakGoalsFor = standings.StreakGoalsFor,
            StreakGoalsAgainst = standings.StreakGoalsAgainst,
            StreakGoalDifferential = standings.StreakGoalDifferential,
            GamesPlayedVsDivision = standings.GamesPlayedVsDivision,
            WinsVsDivision = standings.WinsVsDivision,
            LossesVsDivision = standings.LossesVsDivision,
            OTLossesVsDivision = standings.OTLossesVsDivision,
            WinPctVsDivision = standings.WinPctVsDivision,
            GamesPlayedVsConference = standings.GamesPlayedVsConference,
            WinsVsConference = standings.WinsVsConference,
            LossesVsConference = standings.LossesVsConference,
            OTLossesVsConference = standings.OTLossesVsConference,
            WinPctVsConference = standings.WinPctVsConference,
            InterConfGamesPlayed = standings.InterConfGamesPlayed,
            InterConfWins = standings.InterConfWins,
            InterConfLosses = standings.InterConfLosses,
            InterConfOTLosses = standings.InterConfOTLosses,
            InterConfWinPct = standings.InterConfWinPct,
            GamesPlayedInLast10Games = standings.GamesPlayedInLast10Games,
            WinsInLast10Games = standings.WinsInLast10Games,
            LossesInLast10Games = standings.LossesInLast10Games,
            WinPctInLast10Games = standings.WinPctInLast10Games,
            NextGameId = standings.NextGameId
        };

        public DTO_HeadToHeadSeries ConvertToDTO(HeadToHeadSeries matchup) => new()
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

        public DTO_PlayoffRound ConvertToDTO(PlayoffRound round) => new()
        {
            Id = round.Id,
            SeasonId = round.SeasonId,
            Index = round.Index,
            Name = round.Name,
            MatchupsConfirmed = round.MatchupsConfirmed
        };

        public DTO_PlayoffSeries ConvertToDTO(PlayoffSeries matchup) => new()
        {
            Id = matchup.Id,
            SeasonId = matchup.SeasonId,
            RoundId = matchup.RoundId,
            Team1Id = matchup.Team1Id,
            Team1Wins = matchup.Team1Wins,
            Team1Placeholder = matchup.Team1Placeholder,
            Team2Id = matchup.Team2Id,
            Team2Wins = matchup.Team2Wins,
            Team2Placeholder = matchup.Team2Placeholder,
            Index = matchup.Index,
            Description = matchup.Description,
            IsConfirmed = matchup.IsConfirmed,
            HasEnded = matchup.HasEnded
        };

        public DTO_ChampionsRound ConvertToDTO(ChampionsRound opponent) => new()
        {
            Id = opponent.Id,
            ChampionId = opponent.ChampionId,
            RoundIndex = opponent.RoundIndex,
            OpponentId = opponent.OpponentId,
            SeriesLength = opponent.SeriesLength,
            BestOf = opponent.BestOf
        };



        public Alignment ConvertFromDTO(DTO_Alignment alignment) => new()
        {
            Id = alignment.Id,
            SeasonId = alignment.SeasonId,
            Season = _localContext.Seasons.FirstOrDefault(s => s.Id == alignment.SeasonId)!,
            ConferenceId = alignment.ConferenceId,
            Conference = _localContext.Conferences.FirstOrDefault(c => c.Id == alignment.ConferenceId),
            DivisionId = alignment.DivisionId,
            Division = _localContext.Divisions.FirstOrDefault(d => d.Id == alignment.DivisionId),
            TeamId = alignment.TeamId,
            Team = _localContext.Teams.FirstOrDefault(t => t.Id == alignment.TeamId)!
        };

        public Standings ConvertFromDTO(DTO_Standings standings) => new()
        {
            Id = standings.Id,
            SeasonId = standings.SeasonId,
            Season = _localContext.Seasons.FirstOrDefault(s => s.Id == standings.SeasonId)!,
            ConferenceId = standings.ConferenceId,
            Conference = _localContext.Conferences.FirstOrDefault(c => c.Id == standings.ConferenceId),
            DivisionId = standings.DivisionId,
            Division = _localContext.Divisions.FirstOrDefault(d => d.Id == standings.DivisionId),
            DivisionRanking = standings.DivisionRanking,
            ConferenceRanking = standings.ConferenceRanking,
            PlayoffRanking = standings.PlayoffRanking,
            LeagueRanking = standings.LeagueRanking,
            TeamId = standings.TeamId,
            Team = _localContext.Teams.FirstOrDefault(t => t.Id == standings.TeamId)!,
            PlayoffStatus = standings.PlayoffStatus,
            GamesPlayed = standings.GamesPlayed,
            Wins = standings.Wins,
            Losses = standings.Losses,
            OTLosses = standings.OTLosses,
            RegulationWins = standings.RegulationWins,
            RegPlusOTWins = standings.RegPlusOTWins,
            Points = standings.Points,
            MaximumPossiblePoints = standings.MaximumPossiblePoints,
            WinPct = standings.WinPct,
            PointsPct = standings.PointsPct,
            DivisionGamesBehind = standings.DivisionGamesBehind,
            ConferenceGamesBehind = standings.ConferenceGamesBehind,
            LeagueGamesBehind = standings.LeagueGamesBehind,
            PlayoffsGamesBehind = standings.PlayoffsGamesBehind,
            GoalsFor = standings.GoalsFor,
            GoalsAgainst = standings.GoalsAgainst,
            GoalDifferential = standings.GoalDifferential,
            Streak = standings.Streak,
            StreakGoalsFor = standings.StreakGoalsFor,
            StreakGoalsAgainst = standings.StreakGoalsAgainst,
            StreakGoalDifferential = standings.StreakGoalDifferential,
            GamesPlayedVsDivision = standings.GamesPlayedVsDivision,
            WinsVsDivision = standings.WinsVsDivision,
            LossesVsDivision = standings.LossesVsDivision,
            OTLossesVsDivision = standings.OTLossesVsDivision,
            WinPctVsDivision = standings.WinPctVsDivision,
            GamesPlayedVsConference = standings.GamesPlayedVsConference,
            WinsVsConference = standings.WinsVsConference,
            LossesVsConference = standings.LossesVsConference,
            OTLossesVsConference = standings.OTLossesVsConference,
            WinPctVsConference = standings.WinPctVsConference,
            InterConfGamesPlayed = standings.InterConfGamesPlayed,
            InterConfWins = standings.InterConfWins,
            InterConfLosses = standings.InterConfLosses,
            InterConfOTLosses = standings.InterConfOTLosses,
            InterConfWinPct = standings.InterConfWinPct,
            GamesPlayedInLast10Games = standings.GamesPlayedInLast10Games,
            WinsInLast10Games = standings.WinsInLast10Games,
            LossesInLast10Games = standings.LossesInLast10Games,
            WinPctInLast10Games = standings.WinPctInLast10Games,
            NextGameId = standings.NextGameId,
            NextGame = _localContext.Schedule.FirstOrDefault(s => s.Id == standings.NextGameId)
        };

        public HeadToHeadSeries ConvertFromDTO(DTO_HeadToHeadSeries matchup) => new()
        {
            Id = matchup.Id,
            SeasonId = matchup.SeasonId,
            Season = _localContext.Seasons.FirstOrDefault(s => s.Id == matchup.SeasonId)!,
            Team1Id = matchup.Team1Id,
            Team1 = _localContext.Teams.FirstOrDefault(t => t.Id == matchup.Team1Id)!,
            Team1Wins = matchup.Team1Wins,
            Team1GoalsFor = matchup.Team1GoalsFor,
            Team2Id = matchup.Team2Id,
            Team2 = _localContext.Teams.FirstOrDefault(t => t.Id == matchup.Team2Id)!,
            Team2Wins = matchup.Team2Wins,
            Team2GoalsFor = matchup.Team2GoalsFor
        };

        public PlayoffRound ConvertFromDTO(DTO_PlayoffRound round) => new()
        {
            Id = round.Id,
            SeasonId = round.SeasonId,
            Season = _localContext.Seasons.FirstOrDefault(s => s.Id == round.SeasonId)!,
            Index = round.Index,
            Name = round.Name,
            MatchupsConfirmed = round.MatchupsConfirmed
        };

        public PlayoffSeries ConvertFromDTO(DTO_PlayoffSeries matchup) => new()
        {
            Id = matchup.Id,
            SeasonId = matchup.SeasonId,
            Season = _localContext.Seasons.FirstOrDefault(s => s.Id == matchup.SeasonId)!,
            RoundId = matchup.RoundId,
            Round = _localContext.PlayoffRounds.FirstOrDefault(r => r.Id == matchup.RoundId)!,
            Team1Id = matchup.Team1Id,
            Team1 = _localContext.Teams.FirstOrDefault(t => t.Id == matchup.Team1Id),
            Team1Wins = matchup.Team1Wins,
            Team1Placeholder = matchup.Team1Placeholder,
            Team2Id = matchup.Team2Id,
            Team2 = _localContext.Teams.FirstOrDefault(t => t.Id == matchup.Team2Id),
            Team2Wins = matchup.Team2Wins,
            Team2Placeholder = matchup.Team2Placeholder,
            Index = matchup.Index,
            Description = matchup.Description,
            IsConfirmed = matchup.IsConfirmed,
            HasEnded = matchup.HasEnded
        };

        public ChampionsRound ConvertFromDTO(DTO_ChampionsRound opponent) => new()
        {
            Id = opponent.Id,
            ChampionId = opponent.ChampionId,
            Champion = _localContext.Champions.FirstOrDefault(c => c.Id == opponent.ChampionId)!,
            RoundIndex = opponent.RoundIndex,
            OpponentId = opponent.OpponentId,
            SeriesLength = opponent.SeriesLength,
            BestOf = opponent.BestOf
        };
    }
}
