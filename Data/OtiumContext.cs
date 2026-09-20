using Microsoft.EntityFrameworkCore;
using Otium.Models;

namespace Otium.Data;

public class OtiumContext : DbContext
{
    public OtiumContext(DbContextOptions<OtiumContext> opciones) : base(opciones) { }

    public DbSet<Libro> Libros => Set<Libro>();
}
