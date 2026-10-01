using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Sap;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Staging de SAP (sección 3). Los nombres de constraints/índices son explícitos y de 30
    // caracteres o menos, por si el Oracle de producción es anterior a 12.2.

    public class StgSapEquipoConfiguration : IEntityTypeConfiguration<StgSapEquipo>
    {
        public void Configure(EntityTypeBuilder<StgSapEquipo> b)
        {
            b.ToTable("STG_SAP_EQUIPO_PGPM");
            b.HasKey(x => x.EquipoSapId).HasName("PK_STG_EQUIPO_PGPM");
            b.Property(x => x.EquipoSapId).HasMaxLength(30);
            b.Property(x => x.CodigoLegado).HasMaxLength(30);
            b.Property(x => x.Descripcion).HasMaxLength(200);
            b.Property(x => x.PuestoTrabajo).HasMaxLength(60);
            b.Property(x => x.ActualizadoEn).HasDefaultValueSql("SYSTIMESTAMP");
            b.HasIndex(x => x.CodigoLegado).HasDatabaseName("IX_STG_EQUIPO_LEGADO_PGPM");
        }
    }

    public class StgSapOrdenConfiguration : IEntityTypeConfiguration<StgSapOrden>
    {
        public void Configure(EntityTypeBuilder<StgSapOrden> b)
        {
            b.ToTable("STG_SAP_ORDEN_PGPM");
            b.HasKey(x => x.OrdenSapId).HasName("PK_STG_ORDEN_PGPM");
            b.Property(x => x.OrdenSapId).HasMaxLength(20);
            b.Property(x => x.ClaseOrden).HasMaxLength(60);
            b.Property(x => x.EquipoSapId).HasMaxLength(30);
            b.Property(x => x.Descripcion).HasMaxLength(200);
            b.Property(x => x.PuestoTrabajo).HasMaxLength(60);
            b.Property(x => x.EstadoOrden).HasMaxLength(60);
            b.Property(x => x.FechaCreacion).HasColumnType("DATE");
            b.Property(x => x.FechaInicioPlan).HasColumnType("DATE");
            b.Property(x => x.FechaFinPlan).HasColumnType("DATE");
            b.Property(x => x.FechaInicioReal).HasColumnType("DATE");
            b.Property(x => x.FechaFinReal).HasColumnType("DATE");
            b.Property(x => x.ActualizadoEn).HasDefaultValueSql("SYSTIMESTAMP");

            // Sin FK obligatoria a nivel de negocio: una orden puede venir sin equipo (p. ej.
            // sobre una ubicación técnica). Con equipo, debe existir en el staging de equipos.
            b.HasOne(x => x.Equipo).WithMany(e => e.Ordenes)
                .HasForeignKey(x => x.EquipoSapId)
                .HasConstraintName("FK_STG_ORDEN_EQUIPO_PGPM")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.EquipoSapId).HasDatabaseName("IX_STG_ORDEN_EQUIPO_PGPM");
            b.HasIndex(x => new { x.FechaInicioReal, x.FechaFinReal }).HasDatabaseName("IX_STG_ORDEN_FECHAS_PGPM");
            b.HasIndex(x => x.ClaseOrden).HasDatabaseName("IX_STG_ORDEN_CLASE_PGPM");
        }
    }

    public class StgSapOperacionHorasConfiguration : IEntityTypeConfiguration<StgSapOperacionHoras>
    {
        public void Configure(EntityTypeBuilder<StgSapOperacionHoras> b)
        {
            b.ToTable("STG_SAP_OPERACION_HORAS_PGPM");
            b.HasKey(x => x.Id).HasName("PK_STG_OPER_HORAS_PGPM");
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.OrdenSapId).HasMaxLength(20);
            b.Property(x => x.Operacion).HasMaxLength(10);
            b.Property(x => x.TrabajoPlanHh).HasPrecision(12, 2).HasDefaultValue(0m);
            b.Property(x => x.TrabajoRealHh).HasPrecision(12, 2).HasDefaultValue(0m);
            b.Property(x => x.ActualizadoEn).HasDefaultValueSql("SYSTIMESTAMP");

            b.HasIndex(x => new { x.OrdenSapId, x.Operacion }).IsUnique().HasDatabaseName("UQ_STG_OPER_HORAS_PGPM");
            b.HasOne(x => x.Orden).WithMany(o => o.Operaciones)
                .HasForeignKey(x => x.OrdenSapId)
                .HasConstraintName("FK_STG_OPER_ORDEN_PGPM")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StgSapHistorialEstadoConfiguration : IEntityTypeConfiguration<StgSapHistorialEstado>
    {
        public void Configure(EntityTypeBuilder<StgSapHistorialEstado> b)
        {
            b.ToTable("STG_SAP_HIST_ESTADO_PGPM");
            b.HasKey(x => new { x.OrdenSapId, x.EstadoCodigo, x.NumeroCambio }).HasName("PK_STG_HIST_ESTADO_PGPM");
            b.Property(x => x.OrdenSapId).HasMaxLength(20);
            b.Property(x => x.EstadoCodigo).HasMaxLength(10);
            b.Property(x => x.NumeroCambio).HasMaxLength(10);
            b.Property(x => x.EstadoTexto).HasMaxLength(60);
            b.Property(x => x.Indicador).HasMaxLength(1);
            b.Property(x => x.FechaHoraCambio).HasColumnType("DATE");
            b.Property(x => x.ActualizadoEn).HasDefaultValueSql("SYSTIMESTAMP");

            // Sin FK a la orden: el historial puede llegar en una corrida antes que la orden.
            b.HasIndex(x => new { x.OrdenSapId, x.FechaHoraCambio }).HasDatabaseName("IX_STG_HIST_ORDEN_PGPM");
        }
    }
}
