using Microsoft.EntityFrameworkCore;
using InstaFakeAnalyzer.Models;

namespace InstaFakeAnalyzer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Noticia> Noticia { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
    }
}