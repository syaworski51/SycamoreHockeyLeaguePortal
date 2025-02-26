using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SHLAPI.Models;

namespace SHLAPI.Data;

public partial class SHLPortalDbContext : DbContext
{
    public SHLPortalDbContext()
    {
    }

    public SHLPortalDbContext(DbContextOptions<SHLPortalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alignment> Alignments { get; set; }

    public virtual DbSet<Champion> Champions { get; set; }

    public virtual DbSet<ChampionsRound> ChampionsRounds { get; set; }

    public virtual DbSet<Conference> Conferences { get; set; }

    public virtual DbSet<Division> Divisions { get; set; }

    public virtual DbSet<GameType> GameTypes { get; set; }

    public virtual DbSet<PlayoffRound> PlayoffRounds { get; set; }

    public virtual DbSet<PlayoffSeries> PlayoffSeries { get; set; }

    public virtual DbSet<PlayoffStatus> PlayoffStatuses { get; set; }

    public virtual DbSet<ProgramFlag> ProgramFlags { get; set; }

    public virtual DbSet<Schedule> Schedule { get; set; }

    public virtual DbSet<Season> Seasons { get; set; }

    public virtual DbSet<Standings> Standings { get; set; }

    public virtual DbSet<StandingsSortOption> StandingsSortOptions { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=SHL_DB");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alignment>(entity =>
        {
            entity.HasIndex(e => e.ConferenceId, "IX_Alignments_ConferenceId");

            entity.HasIndex(e => e.DivisionId, "IX_Alignments_DivisionId");

            entity.HasIndex(e => e.SeasonId, "IX_Alignments_SeasonId");

            entity.HasIndex(e => e.TeamId, "IX_Alignments_TeamId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Conference).WithMany(p => p.Alignments).HasForeignKey(d => d.ConferenceId);

            entity.HasOne(d => d.Division).WithMany(p => p.Alignments).HasForeignKey(d => d.DivisionId);

            entity.HasOne(d => d.Season).WithMany(p => p.Alignments).HasForeignKey(d => d.SeasonId);

            entity.HasOne(d => d.Team).WithMany(p => p.Alignments).HasForeignKey(d => d.TeamId);
        });

        modelBuilder.Entity<Champion>(entity =>
        {
            entity.HasIndex(e => e.SeasonId, "IX_Champions_SeasonId");

            entity.HasIndex(e => e.TeamId, "IX_Champions_TeamId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Season).WithMany(p => p.Champions).HasForeignKey(d => d.SeasonId);

            entity.HasOne(d => d.Team).WithMany(p => p.Champions).HasForeignKey(d => d.TeamId);
        });

        modelBuilder.Entity<ChampionsRound>(entity =>
        {
            entity.HasIndex(e => e.ChampionId, "IX_ChampionsRounds_ChampionId");

            entity.HasIndex(e => e.OpponentId, "IX_ChampionsRounds_OpponentId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Champion).WithMany(p => p.ChampionsRounds)
                .HasForeignKey(d => d.ChampionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Opponent).WithMany(p => p.ChampionsRounds)
                .HasForeignKey(d => d.OpponentId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Conference>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Division>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<GameType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ParameterValue).HasDefaultValueSql("(N'')");
        });

        modelBuilder.Entity<PlayoffRound>(entity =>
        {
            entity.HasIndex(e => e.SeasonId, "IX_PlayoffRounds_SeasonId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Season).WithMany(p => p.PlayoffRounds).HasForeignKey(d => d.SeasonId);
        });

        modelBuilder.Entity<PlayoffSeries>(entity =>
        {
            entity.HasIndex(e => e.RoundId, "IX_PlayoffSeries_RoundId");

            entity.HasIndex(e => e.SeasonId, "IX_PlayoffSeries_SeasonId");

            entity.HasIndex(e => e.Team1Id, "IX_PlayoffSeries_Team1Id");

            entity.HasIndex(e => e.Team2Id, "IX_PlayoffSeries_Team2Id");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Round).WithMany(p => p.PlayoffSeries)
                .HasForeignKey(d => d.RoundId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Season).WithMany(p => p.PlayoffSeries)
                .HasForeignKey(d => d.SeasonId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Team1).WithMany(p => p.PlayoffSeriesTeam1s)
                .HasForeignKey(d => d.Team1Id)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Team2).WithMany(p => p.PlayoffSeriesTeam2s)
                .HasForeignKey(d => d.Team2Id)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PlayoffStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<ProgramFlag>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("Schedule");

            entity.HasIndex(e => e.AwayTeamId, "IX_Schedule_AwayTeamId");

            entity.HasIndex(e => e.HomeTeamId, "IX_Schedule_HomeTeamId");

            entity.HasIndex(e => e.PlayoffRoundId, "IX_Schedule_PlayoffRoundId");

            entity.HasIndex(e => e.SeasonId, "IX_Schedule_SeasonId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsFinalized)
                .IsRequired()
                .HasDefaultValueSql("(CONVERT([bit],(0)))");

            entity.HasOne(d => d.AwayTeam).WithMany(p => p.ScheduleAwayTeams)
                .HasForeignKey(d => d.AwayTeamId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.HomeTeam).WithMany(p => p.ScheduleHomeTeams)
                .HasForeignKey(d => d.HomeTeamId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PlayoffRound).WithMany(p => p.Schedules).HasForeignKey(d => d.PlayoffRoundId);

            entity.HasOne(d => d.Season).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.SeasonId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Standings>(entity =>
        {
            entity.HasIndex(e => e.ConferenceId, "IX_Standings_ConferenceId");

            entity.HasIndex(e => e.DivisionId, "IX_Standings_DivisionId");

            entity.HasIndex(e => e.SeasonId, "IX_Standings_SeasonId");

            entity.HasIndex(e => e.TeamId, "IX_Standings_TeamId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ConferenceGamesBehind).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.DivisionGamesBehind).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.InterConfOTLosses).HasColumnName("InterConfOTLosses");
            entity.Property(e => e.InterConfWinPct).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.LeagueGamesBehind).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.OTLosses).HasColumnName("OTLosses");
            entity.Property(e => e.OTLossesVsConference).HasColumnName("OTLossesVsConference");
            entity.Property(e => e.OTLossesVsDivision).HasColumnName("OTLossesVsDivision");
            entity.Property(e => e.PlayoffsGamesBehind).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.PointsPct).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.RegPlusOTWins).HasColumnName("RegPlusOTWins");
            entity.Property(e => e.WinPct).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.WinPctInLast10Games).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.WinPctVsConference).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.WinPctVsDivision).HasColumnType("decimal(4, 1)");

            entity.HasOne(d => d.Conference).WithMany(p => p.Standings).HasForeignKey(d => d.ConferenceId);

            entity.HasOne(d => d.Division).WithMany(p => p.Standings).HasForeignKey(d => d.DivisionId);

            entity.HasOne(d => d.Season).WithMany(p => p.Standings).HasForeignKey(d => d.SeasonId);

            entity.HasOne(d => d.Team).WithMany(p => p.Standings).HasForeignKey(d => d.TeamId);
        });

        modelBuilder.Entity<StandingsSortOption>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
