using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Resultados materializados por el job: la UI solo lee de aquí.

    public class CalcDisponibilidadDiariaConfiguration : IEntityTypeConfiguration<CalcDisponibilidadDiaria>
    {
        public void Configure(EntityTypeBuilder<CalcDisponibilidadDiaria> b)
        {
            b.ToTable("CALC_DISP_DIARIA_PGPM");
            b.HasKey(x => new { x.ReporteId, x.AsignacionId, x.Fecha }).HasName("PK_CALC_DISP_DIARIA_PGPM");
            b.Property(x => x.Fecha).HasColumnType("DATE");
            b.Property(x => x.EqDisponibleProm).HasPrecision(10, 2);
            b.Property(x => x.DispRealPct).HasPrecision(6, 2);
            b.Property(x => x.PotencialPct).HasPrecision(7, 2);
            b.Property(x => x.DispCompPct).HasPrecision(6, 2);
            b.Property(x => x.HrsDia).HasPrecision(12, 2);
            b.Property(x => x.HrsOtPlanificada).HasPrecision(12, 2);
            b.Property(x => x.HrsOtPreventivo).HasPrecision(12, 2);
            b.Property(x => x.HrsOtCorrectiva).HasPrecision(12, 2);
            b.Property(x => x.HrsRealEfec).HasPrecision(12, 2);
            b.Property(x => x.HrsDiaDispComp).HasPrecision(12, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasColumnType("DATE");
            b.HasIndex(x => new { x.ReporteId, x.Fecha }).HasDatabaseName("IX_CALC_DISP_DIARIA_FEC_PGPM");
        }
    }

    public class CalcDisponibilidadSemanalConfiguration : IEntityTypeConfiguration<CalcDisponibilidadSemanal>
    {
        public void Configure(EntityTypeBuilder<CalcDisponibilidadSemanal> b)
        {
            b.ToTable("CALC_DISP_SEMANAL_PGPM");
            b.HasKey(x => new { x.ReporteId, x.AsignacionId, x.NoSemana }).HasName("PK_CALC_DISP_SEMANAL_PGPM");
            b.Property(x => x.FechaInicio).HasColumnType("DATE");
            b.Property(x => x.FechaFin).HasColumnType("DATE");
            b.Property(x => x.EqDisponibleProm).HasPrecision(10, 2);
            b.Property(x => x.DispRealPct).HasPrecision(6, 2);
            b.Property(x => x.DispRealMinPct).HasPrecision(6, 2);
            b.Property(x => x.PotencialPct).HasPrecision(7, 2);
            b.Property(x => x.DispCompPct).HasPrecision(6, 2);
            b.Property(x => x.HrsDia).HasPrecision(12, 2);
            b.Property(x => x.HrsOtPlanificada).HasPrecision(12, 2);
            b.Property(x => x.HrsOtPreventivo).HasPrecision(12, 2);
            b.Property(x => x.HrsOtCorrectiva).HasPrecision(12, 2);
            b.Property(x => x.HrsRealEfec).HasPrecision(12, 2);
            b.Property(x => x.HrsDiaDispComp).HasPrecision(12, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasColumnType("DATE");
        }
    }

    public class CalcAvanceSemanalConfiguration : IEntityTypeConfiguration<CalcAvanceSemanal>
    {
        public void Configure(EntityTypeBuilder<CalcAvanceSemanal> b)
        {
            b.ToTable("CALC_AVANCE_SEMANAL_PGPM");
            b.HasKey(x => new { x.ReporteId, x.FamiliaCodigo, x.NoSemana }).HasName("PK_CALC_AVANCE_SEMANAL_PGPM");
            b.Property(x => x.FamiliaCodigo).HasMaxLength(60);
            b.Property(x => x.FechaInicio).HasColumnType("DATE");
            b.Property(x => x.FechaFin).HasColumnType("DATE");
            b.Property(x => x.ProgPct).HasPrecision(6, 2);
            b.Property(x => x.RealOrdenesPct).HasPrecision(6, 2);
            b.Property(x => x.DiasActTotal).HasPrecision(12, 2);
            b.Property(x => x.DiasActTerminadas).HasPrecision(12, 2);
            b.Property(x => x.RealActPct).HasPrecision(6, 2);
            b.Property(x => x.RealActProgPct).HasPrecision(6, 2);
            b.Property(x => x.RealActNoProgPct).HasPrecision(6, 2);
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.GeneradoEn).HasColumnType("DATE");
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
            b.Property(x => x.DetectadoEn).HasColumnType("DATE");
        }
    }
}
