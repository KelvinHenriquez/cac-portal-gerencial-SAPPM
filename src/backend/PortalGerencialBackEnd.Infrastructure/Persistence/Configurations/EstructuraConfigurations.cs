using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities.Estructura;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    // Vistas de estructura (sección 4.1): sin llave y mapeadas con ToView, así que las
    // migraciones las ignoran. Se crean con los scripts de vistas de cada ambiente.

    public class VwUmbralSemaforoConfiguration : IEntityTypeConfiguration<VwUmbralSemaforo>
    {
        public void Configure(EntityTypeBuilder<VwUmbralSemaforo> b)
        {
            b.HasNoKey().ToView("VW_UMBRALES_SEMAFORO_PGPM");
            b.Property(x => x.UmbralRojo).HasPrecision(5, 2);
            b.Property(x => x.UmbralNaranja).HasPrecision(5, 2);
        }
    }

    public class VwEstructuraFamiliaConfiguration : IEntityTypeConfiguration<VwEstructuraFamilia>
    {
        public void Configure(EntityTypeBuilder<VwEstructuraFamilia> b)
        {
            b.HasNoKey().ToView("VW_ESTRUCTURA_FAMILIAS_PGPM");
            b.Property(x => x.FechaEstInicio).HasColumnType("DATE");
            b.Property(x => x.FechaEstFin).HasColumnType("DATE");
        }
    }

    public class VwCurvaPlaneadaConfiguration : IEntityTypeConfiguration<VwCurvaPlaneada>
    {
        public void Configure(EntityTypeBuilder<VwCurvaPlaneada> b)
        {
            b.HasNoKey().ToView("VW_CURVA_PLANEADA_PGPM");
            b.Property(x => x.ActividadPct).HasPrecision(5, 2);
            b.Property(x => x.FechaInicioSemana).HasColumnType("DATE");
            b.Property(x => x.FechaFinSemana).HasColumnType("DATE");
        }
    }

    public class VwEquipoRequeridoConfiguration : IEntityTypeConfiguration<VwEquipoRequerido>
    {
        public void Configure(EntityTypeBuilder<VwEquipoRequerido> b)
        {
            b.HasNoKey().ToView("VW_EQUIPOS_REQUERIDOS_PGPM");
        }
    }

    public class VwEquipoFamiliaConfiguration : IEntityTypeConfiguration<VwEquipoFamilia>
    {
        public void Configure(EntityTypeBuilder<VwEquipoFamilia> b)
        {
            b.HasNoKey().ToView("VW_EQUIPOS_FAMILIA_PGPM");
        }
    }
}
