using Microsoft.EntityFrameworkCore;
using InstaFakeAnalyzer.Models;
using System;

namespace InstaFakeAnalyzer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Noticia> Noticia { get; set; }
        public DbSet<Usuario> Usuario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed: usuário administrador padrão
            modelBuilder.Entity<Usuario>().HasData(new Usuario
            {
                Id = 1,
                Nome = "Administrador",
                NomeUsuario = "admin",
                // Senha MD5 de "admin" (valor gerado pelo utilitário MD5 do projeto)
                Senha = "21232F297A57A5A743894A0E4A801FC3",
                DataCadastro = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                tipoUsuario = InstaFakeAnalyzer.Models.Usuario.TipoUsuario.Administrador,
                snAtivo = true
            });
        }
    }
}