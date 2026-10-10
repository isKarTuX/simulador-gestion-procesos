using Microsoft.EntityFrameworkCore;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data;

/// <summary>Esquema de BD congelado (CONTEXT §5.1). Cambia solo con la skill contrato-xml.</summary>
public sealed class SimuladorDbContext(DbContextOptions<SimuladorDbContext> options) : DbContext(options)
{
    public DbSet<Catalogo> Catalogos => Set<Catalogo>();

    public DbSet<ProcesoCatalogo> Procesos => Set<ProcesoCatalogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Catalogo>(catalogo =>
        {
            catalogo.ToTable("Catalogo");
            catalogo.HasKey(c => c.Id);
            catalogo.HasIndex(c => c.Numero).IsUnique();
            catalogo.Property(c => c.Nombre).IsRequired();
            catalogo.Property(c => c.Criterio)
                .HasConversion(c => c == CriterioSeleccion.Cpu ? "CPU" : "MEMORIA",
                               t => t == "CPU" ? CriterioSeleccion.Cpu : CriterioSeleccion.Memoria);
            catalogo.HasMany(c => c.Procesos)
                .WithOne()
                .HasForeignKey(p => p.CatalogoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcesoCatalogo>(proceso =>
        {
            proceso.ToTable("ProcesoCatalogo");
            proceso.HasKey(p => p.Id);
            proceso.Property(p => p.CatalogoId).HasColumnName("catalogo_id");
            proceso.Property(p => p.Prioridad).HasConversion<int>();
        });
    }
}
