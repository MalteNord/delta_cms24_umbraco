using cms24_delta_umbraco.Models;
using Microsoft.EntityFrameworkCore;

namespace cms24_delta_umbraco.Contexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Player> Players { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
			
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Players)
                .WithOne(p => p.Room)
                .HasForeignKey(p => p.RoomId);

                
        }

        
    }
}
