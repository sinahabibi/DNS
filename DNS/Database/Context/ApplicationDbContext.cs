   using Microsoft.EntityFrameworkCore;
   using DNS.Models;
   using SQLitePCL;

   namespace DNS.Data
   {
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext()
        {
            Batteries.Init();
        }
        public DbSet<Dns> Dns { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=app.db");

        }
    }
   }
   