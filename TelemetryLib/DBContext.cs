using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace TelemetryLib
{
    class DBContext : DbContext
    {
        private static bool _created = false;
        public DBContext()
        {
            if (!_created)
            {
                _created = true;
                Database.EnsureCreated();
            }
        }
        public DbSet<Models.Event> Events { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.UseSqlite("Data Source=Events.db");

        }

    }
}
