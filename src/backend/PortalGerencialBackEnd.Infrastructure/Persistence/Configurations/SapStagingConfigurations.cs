using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Sap;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Staging de SAP (Anexo A v2). Nombres de constraints/índices de 30 caracteres o menos y
    // fechas como DATE (guarda la hora): Oracle 11g.

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
            b.Property(x => x.TipoEquipo).HasMaxLength(20);
            b.Property(x => x.TipoEquipoDesc).HasMaxLength(60);
            b.Property(x => x.GrupoPlanificacion).HasMaxLength(10);
            b.Property(x => x.GrupoPlanificacionDesc).HasMaxLength(60);
            b.Property(x => x.UbicacionTecnica).HasMaxLength(40);
            b.Property(x => x.UbicacionTecnicaDesc).HasMaxLength(100);
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");
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
            b.Property(x => x.ClaseOrden).HasMaxLength(10);
            b.Property(x => x.ClaseActividad).HasMaxLength(10);
            b.Property(x => x.EquipoSapId).HasMaxLength(30);
            b.Property(x => x.Descripcion).HasMaxLength(200);
            b.Property(x => x.PuestoTrabajo).HasMaxLength(60);
            b.Property(x => x.AvisoSapId).HasMaxLength(20);
            b.Property(x => x.EstadoOrden).HasMaxLength(30);
            b.Property(x => x.StatusSistema).HasMaxLength(100);
            b.Property(x => x.StatusUsuario).HasMaxLength(100);
            b.Property(x => x.FechaCreacion).HasColumnType("DATE");
            b.Property(x => x.FechaInicioPlan).HasColumnType("DATE");
            b.Property(x => x.FechaFinPlan).HasColumnType("DATE");
            b.Property(x => x.FechaInicioReal).HasColumnType("DATE");
            b.Property(x => x.FechaFinReal).HasColumnType("DATE");
            b.Property(x => x.FechaCierre).HasColumnType("DATE");
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");

            // Una orden puede venir sin equipo (sobre ubicación técnica); con equipo, debe existir.
            b.HasOne(x => x.Equipo).WithMany(e => e.Ordenes)
                .HasForeignKey(x => x.EquipoSapId)
                .HasConstraintName("FK_STG_ORDEN_EQUIPO_PGPM")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.EquipoSapId).HasDatabaseName("IX_STG_ORDEN_EQUIPO_PGPM");
            b.HasIndex(x => new { x.FechaCreacion, x.FechaCierre }).HasDatabaseName("IX_STG_ORDEN_FECHAS_PGPM");
            b.HasIndex(x => x.ClaseOrden).HasDatabaseName("IX_STG_ORDEN_CLASE_PGPM");
        }
    }

    public class StgSapOperacionConfiguration : IEntityTypeConfiguration<StgSapOperacion>
    {
        public void Configure(EntityTypeBuilder<StgSapOperacion> b)
        {
            b.ToTable("STG_SAP_OPERACION_PGPM");
            b.HasKey(x => new { x.OrdenSapId, x.Operacion }).HasName("PK_STG_OPERACION_PGPM");
            b.Property(x => x.OrdenSapId).HasMaxLength(20);
            b.Property(x => x.Operacion).HasMaxLength(10);
            b.Property(x => x.Texto).HasMaxLength(200);
            b.Property(x => x.PuestoTrabajo).HasMaxLength(60);
            b.Property(x => x.Sistema).HasMaxLength(100);
            b.Property(x => x.Subsistema).HasMaxLength(100);
            b.Property(x => x.Componente).HasMaxLength(100);
            b.Property(x => x.UsuarioCreacion).HasMaxLength(12);
            b.Property(x => x.TrabajoPlanHh).HasPrecision(12, 2);
            b.Property(x => x.TrabajoRealHh).HasPrecision(12, 2);
            b.Property(x => x.FechaInicioPlan).HasColumnType("DATE");
            b.Property(x => x.FechaFinPlan).HasColumnType("DATE");
            b.Property(x => x.FechaPrimeraConf).HasColumnType("DATE");
            b.Property(x => x.FechaUltimaConf).HasColumnType("DATE");
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");

            b.HasOne(x => x.Orden).WithMany(o => o.Operaciones)
                .HasForeignKey(x => x.OrdenSapId)
                .HasConstraintName("FK_STG_OPERACION_ORDEN_PGPM")
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.FechaUltimaConf).HasDatabaseName("IX_STG_OPERACION_CONF_PGPM");
        }
    }

    public class StgSapAvisoConfiguration : IEntityTypeConfiguration<StgSapAviso>
    {
        public void Configure(EntityTypeBuilder<StgSapAviso> b)
        {
            b.ToTable("STG_SAP_AVISO_PGPM");
            b.HasKey(x => x.AvisoSapId).HasName("PK_STG_AVISO_PGPM");
            b.Property(x => x.AvisoSapId).HasMaxLength(20);
            b.Property(x => x.ClaseAviso).HasMaxLength(10);
            b.Property(x => x.Texto).HasMaxLength(200);
            b.Property(x => x.EquipoSapId).HasMaxLength(30);
            b.Property(x => x.OrdenSapId).HasMaxLength(20);
            b.Property(x => x.StatusSistema).HasMaxLength(100);
            b.Property(x => x.FechaAviso).HasColumnType("DATE");
            b.Property(x => x.InicioAveria).HasColumnType("DATE");
            b.Property(x => x.FinAveria).HasColumnType("DATE");
            b.Property(x => x.FechaCierre).HasColumnType("DATE");
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");
            b.HasIndex(x => x.OrdenSapId).HasDatabaseName("IX_STG_AVISO_ORDEN_PGPM");
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
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");
            b.HasIndex(x => new { x.OrdenSapId, x.FechaHoraCambio }).HasDatabaseName("IX_STG_HIST_ORDEN_PGPM");
        }
    }
}
