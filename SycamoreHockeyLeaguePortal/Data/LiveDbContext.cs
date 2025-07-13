using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Data
{
    public class LiveDbContext : IdentityDbContext<ApplicationUser>
    {
        public LiveDbContext(DbContextOptions<LiveDbContext> options) : base(options) { }
        public DbSet<Team> Teams { get; set; } = default!;
        public DbSet<Season> Seasons { get; set; } = default!;
        public DbSet<Conference> Conferences { get; set; } = default!;
        public DbSet<Division> Divisions { get; set; } = default!;
        public DbSet<Alignment> Alignments { get; set; } = default!;
        public DbSet<Game> Schedule { get; set; } = default!;
        public DbSet<Standings> Standings { get; set; } = default!;
        public DbSet<StandingsSortOption> StandingsSortOptions { get; set; } = default!;
        public DbSet<PlayoffRound> PlayoffRounds { get; set; } = default!;
        public DbSet<PlayoffSeries> PlayoffSeries { get; set; } = default!;
        public DbSet<Champion> Champions { get; set; } = default!;
        public DbSet<ChampionsRound> ChampionsRounds { get; set; } = default!;
        public DbSet<ProgramFlag> ProgramFlags { get; set; } = default!;
        public DbSet<PlayoffStatus> PlayoffStatuses { get; set; } = default!;
        public DbSet<GameType> GameTypes { get; set; } = default!;
        public DbSet<TeamBrandingHistory> TeamBrandingHistory { get; set; } = default!;
        public DbSet<HeadToHeadSeries> HeadToHeadSeries { get; set; } = default!;
        public DbSet<RankedPlayoffSeries> RankedPlayoffSeries { get; set; } = default!;
    }
}
