using ItaliaTreni.Models;
using Microsoft.EntityFrameworkCore;
using ItaliaTreni.Interfaces;

namespace ItaliaTreni.Data
{
    public class ItaliaTreniContext : DbContext
    {
        public DbSet<Measurement> Measurement { get; set; }

        public DbSet<OutOfScaleMeasurement> OutOfScaleMeasurement { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ItaliaTreniDB;Integrated Security=True; Trusted_Connection=True; TrustServerCertificate=True;");
        }
    }
}
