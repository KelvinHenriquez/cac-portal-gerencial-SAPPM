using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Estructura;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Vistas de estructura: sin llave y mapeadas con ToView, así que las migraciones las ignoran.
    // Se crean con los scripts de vistas de cada ambiente.

    public class VwEquipoFamiliaConfiguration : IEntityTypeConfiguration<VwEquipoFamilia>
    {
        public void Configure(EntityTypeBuilder<VwEquipoFamilia> b) => b.HasNoKey().ToView("VW_EQUIPOS_FAMILIA_PGPM");
    }

    public class VwDispReporteConfiguration : IEntityTypeConfiguration<VwDispReporte>
    {
        public void Configure(EntityTypeBuilder<VwDispReporte> b)
        {
            b.HasNoKey().ToView("VW_DISP_REPORTE_PGPM");
            b.Property(x => x.UmbralRojo).HasPrecision(5, 2);
            b.Property(x => x.UmbralNaranja).HasPrecision(5, 2);
        }
    }

    public class VwDispSemanaConfiguration : IEntityTypeConfiguration<VwDispSemana>
    {
        public void Configure(EntityTypeBuilder<VwDispSemana> b)
        {
            b.HasNoKey().ToView("VW_DISP_SEMANA_PGPM");
            b.Property(x => x.FechaInicio).HasColumnType("DATE");
            b.Property(x => x.FechaFin).HasColumnType("DATE");
        }
    }

    public class VwDispHoraConfiguration : IEntityTypeConfiguration<VwDispHora>
    {
        public void Configure(EntityTypeBuilder<VwDispHora> b) => b.HasNoKey().ToView("VW_DISP_HORA_PGPM");
    }

    public class VwDispAsignacionConfiguration : IEntityTypeConfiguration<VwDispAsignacion>
    {
        public void Configure(EntityTypeBuilder<VwDispAsignacion> b)
        {
            b.HasNoKey().ToView("VW_DISP_ASIGNACION_PGPM");
            b.Property(x => x.DispCompPct).HasPrecision(5, 2);
        }
    }

    public class VwDispAsigEquipoConfiguration : IEntityTypeConfiguration<VwDispAsigEquipo>
    {
        public void Configure(EntityTypeBuilder<VwDispAsigEquipo> b) => b.HasNoKey().ToView("VW_DISP_ASIG_EQUIPO_PGPM");
    }

    public class VwAvancePeriodoConfiguration : IEntityTypeConfiguration<VwAvancePeriodo>
    {
        public void Configure(EntityTypeBuilder<VwAvancePeriodo> b) => b.HasNoKey().ToView("VW_AVANCE_PERIODO_PGPM");
    }

    public class VwAvanceReporteConfiguration : IEntityTypeConfiguration<VwAvanceReporte>
    {
        public void Configure(EntityTypeBuilder<VwAvanceReporte> b)
        {
            b.HasNoKey().ToView("VW_AVANCE_REPORTE_PGPM");
            b.Property(x => x.FechaInicio).HasColumnType("DATE");
            b.Property(x => x.FechaFin).HasColumnType("DATE");
        }
    }

    public class VwAvanceFamiliaConfiguration : IEntityTypeConfiguration<VwAvanceFamilia>
    {
        public void Configure(EntityTypeBuilder<VwAvanceFamilia> b)
        {
            b.HasNoKey().ToView("VW_AVANCE_FAMILIA_PGPM");
            b.Property(x => x.FechaEstInicio).HasColumnType("DATE");
            b.Property(x => x.FechaEstFin).HasColumnType("DATE");
        }
    }

    public class VwAvanceSemanaConfiguration : IEntityTypeConfiguration<VwAvanceSemana>
    {
        public void Configure(EntityTypeBuilder<VwAvanceSemana> b)
        {
            b.HasNoKey().ToView("VW_AVANCE_SEMANA_PGPM");
            b.Property(x => x.FechaInicio).HasColumnType("DATE");
            b.Property(x => x.FechaFin).HasColumnType("DATE");
            b.Property(x => x.Actividad).HasPrecision(10, 2);
            b.Property(x => x.Dias).HasPrecision(10, 2);
        }
    }
}
