using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Resultados materializados por el job (sección 3): la UI solo lee de aquí.

    public class CalcDisponibilidadDiariaConfiguration : IEntityTypeConfiguration<CalcDisponibilidadDiaria>
    {
        public void Configure(EntityTypeBuilder<CalcDisponibilidadDiaria> b)
        {
            b.ToTable("CALC_DISP_DIARIA_PGPM");
            b.HasKey(x => new { x.TipoReporte, x.FamiliaCodigo, x.Fecha }).HasName("PK_CALC_DISP_DIARIA_PGPM");
            b.Property(x => x.TipoReporte).HasMaxLength(60);
            b.Property(x => x.FamiliaCodigo).HasMaxLength(60);
            b.Property(x => x.Fecha).HasColumnType("DATE");
            b.Property(x => x.DispRealPct).HasPrecision(5, 2);
            b.Property(x => x.DispPotencialPct).HasPrecision(5, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasDefaultValueSql("SYSTIMESTAMP");
            b.HasIndex(x => new { x.TipoReporte, x.Fecha }).HasDatabaseName("IX_CALC_DISP_DIARIA_FEC_PGPM");
        }
    }

    public class CalcDisponibilidadSemanalConfiguration : IEntityTypeConfiguration<CalcDisponibilidadSemanal>
    {
        public void Configure(EntityTypeBuilder<CalcDisponibilidadSemanal> b)
        {
            b.ToTable("CALC_DISP_SEMANAL_PGPM");
            b.HasKey(x => new { x.TipoReporte, x.FamiliaCodigo, x.AnioIso, x.SemanaIso }).HasName("PK_CALC_DISP_SEMANAL_PGPM");
            b.Property(x => x.TipoReporte).HasMaxLength(60);
            b.Property(x => x.FamiliaCodigo).HasMaxLength(60);
            b.Property(x => x.FechaInicioSemana).HasColumnType("DATE");
            b.Property(x => x.FechaFinSemana).HasColumnType("DATE");
            b.Property(x => x.EqDisponibleProm).HasPrecision(10, 2);
            b.Property(x => x.DispRealPct).HasPrecision(5, 2);
            b.Property(x => x.DispRealMinPct).HasPrecision(5, 2);
            b.Property(x => x.DispPotencialPct).HasPrecision(5, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasDefaultValueSql("SYSTIMESTAMP");
            b.HasIndex(x => new { x.TipoReporte, x.FechaInicioSemana }).HasDatabaseName("IX_CALC_DISP_SEM_FEC_PGPM");
        }
    }

    public class CalcAvanceSemanalConfiguration : IEntityTypeConfiguration<CalcAvanceSemanal>
    {
        public void Configure(EntityTypeBuilder<CalcAvanceSemanal> b)
        {
            b.ToTable("CALC_AVANCE_SEMANAL_PGPM");
            b.HasKey(x => new { x.FamiliaCodigo, x.AnioPrograma, x.Semana }).HasName("PK_CALC_AVANCE_SEMANAL_PGPM");
            b.Property(x => x.FamiliaCodigo).HasMaxLength(60);
            b.Property(x => x.AnioPrograma).HasMaxLength(20);
            b.Property(x => x.FechaInicioSemana).HasColumnType("DATE");
            b.Property(x => x.FechaFinSemana).HasColumnType("DATE");
            b.Property(x => x.ProgPct).HasPrecision(5, 2);
            b.Property(x => x.RealPct).HasPrecision(5, 2);
            b.Property(x => x.PendPct).HasPrecision(6, 2); // puede ser negativo (adelanto)
            b.Property(x => x.HhPlan).HasPrecision(12, 2);
            b.Property(x => x.HhReal).HasPrecision(12, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasDefaultValueSql("SYSTIMESTAMP");
            b.HasIndex(x => x.AnioPrograma).HasDatabaseName("IX_CALC_AVANCE_PROG_PGPM");
        }
    }

    public class CalcEquipoSinClasificarConfiguration : IEntityTypeConfiguration<CalcEquipoSinClasificar>
    {
        public void Configure(EntityTypeBuilder<CalcEquipoSinClasificar> b)
        {
            b.ToTable("CALC_EQUIPO_SIN_CLASIF_PGPM");
            b.HasKey(x => x.EquipoSapId).HasName("PK_CALC_EQ_SIN_CLASIF_PGPM");
            b.Property(x => x.EquipoSapId).HasMaxLength(30);
            b.Property(x => x.CodigoLegado).HasMaxLength(30);
            b.Property(x => x.Descripcion).HasMaxLength(200);
            b.Property(x => x.PuestoTrabajo).HasMaxLength(60);
            b.Property(x => x.Motivo).HasMaxLength(30);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.DetectadoEn).HasDefaultValueSql("SYSTIMESTAMP");
        }
    }
}
