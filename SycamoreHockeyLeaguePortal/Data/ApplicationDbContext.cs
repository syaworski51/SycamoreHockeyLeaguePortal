using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<SycamoreHockeyLeaguePortal.Models.Team> Team { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Season> Season { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Conference> Conference { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Division> Division { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Alignment> Alignment { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Schedule> Schedule { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.Standings> Standings { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.StandingsSortOption> StandingsSortOption { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.PlayoffRound> PlayoffRound { get; set; } = default!;
        public DbSet<SycamoreHockeyLeaguePortal.Models.PlayoffSeries> PlayoffSeries { get; set; } = default!;
    }
}
