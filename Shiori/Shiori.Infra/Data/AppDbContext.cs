using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Infra.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Anime> Animes { get; set; }
        public DbSet<UserAnime> UserAnimes { get; set; }
        public DbSet<AnimeMetric> AnimeMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserAnime>()
                .HasIndex(ua => new { ua.UserId, ua.AnimeId })
                .IsUnique();

            builder.Entity<Anime>()
                .HasIndex(a => a.JikanId)
                .IsUnique();

            builder.Entity<AnimeMetric>()
                .HasIndex(m => new { m.AnimeId, m.SnapshotDate });

            builder.Entity<User>(u => u.Property("Role"));
        }
    }
}
