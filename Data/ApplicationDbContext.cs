using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Otium.Models;

namespace Otium.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Amistad> Amistades => Set<Amistad>();

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

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.CodigoAmigo)
            .IsUnique();
    }
}
