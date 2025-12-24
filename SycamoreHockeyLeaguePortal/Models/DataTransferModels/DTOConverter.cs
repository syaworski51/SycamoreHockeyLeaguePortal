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
            IsLive = game.IsLive,
            IsFinalized = game.IsFinalized,
            Notes = game.Notes
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
            rounds.Select(r => new DTO_ChampionsRound()
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
                IsLive = g.IsLive,
                IsFinalized = g.IsFinalized,
                Notes = g.Notes
            }).ToList();

        public List<DTO_HeadToHeadSeries> ConvertBatchToDTO(List<HeadToHeadSeries> matchups) =>
            matchups.Select(m => new DTO_HeadToHeadSeries
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
            Notes = gameDTO.Notes
        };

        public HeadToHeadSeries ConvertToStandardModel(DTO_HeadToHeadSeries matchupDTO) => new()
        {
            Id = matchupDTO.Id,
            SeasonId = matchupDTO.SeasonId,
            Team1Id = matchupDTO.Team1Id,
            Team1Wins = matchupDTO.Team1Wins,
            Team1GoalsFor = matchupDTO.Team1GoalsFor,
            Team2Id = matchupDTO.Team2Id,
            Team2Wins = matchupDTO.Team2Wins,
            Team2GoalsFor = matchupDTO.Team2GoalsFor
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
                Notes = g.Notes
            }).ToList();

        public List<HeadToHeadSeries> ConvertBatchToStandardModel(List<DTO_HeadToHeadSeries> DTOs) =>
            DTOs.Select(dto => new HeadToHeadSeries
            {
                Id = dto.Id,
                SeasonId = dto.SeasonId,
                Team1Id = dto.Team1Id,
                Team1Wins = dto.Team1Wins,
                Team1GoalsFor = dto.Team1GoalsFor,
                Team2Id = dto.Team2Id,
                Team2Wins = dto.Team2Wins,
                Team2GoalsFor = dto.Team2GoalsFor
            }).ToList();
    }
}
