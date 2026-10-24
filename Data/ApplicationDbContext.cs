using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Otium.Models;

namespace Otium.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Amistad> Amistades => Set<Amistad>();
    public DbSet<EntradaLectura> EntradasLectura => Set<EntradaLectura>();
    public DbSet<PalabraGlosario> PalabrasGlosario => Set<PalabraGlosario>();
    public DbSet<Bloqueo> Bloqueos => Set<Bloqueo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Libro>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Amistad>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Amistad>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.ReceptorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Amistad>()
            .HasIndex(a => a.ClavePar)
            .IsUnique();

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.CodigoAmigo)
            .IsUnique();

        builder.Entity<EntradaLectura>()
            .HasOne<Libro>()
            .WithMany()
            .HasForeignKey(e => e.LibroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bloqueo>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bloqueo>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.BloqueadoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bloqueo>()
            .HasIndex(b => new { b.UsuarioId, b.BloqueadoId })
            .IsUnique();
    }
}
