using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Entities;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    public class ResumenAvanceFilaConfiguration : IEntityTypeConfiguration<ResumenAvanceFila>
    {
        public void Configure(EntityTypeBuilder<ResumenAvanceFila> b)
        {
            b.ToTable("RESUMEN_AVANCE_FILA_PGPM");
            b.HasKey(x => new { x.Area, x.ReporteId, x.FamiliaCodigo }).HasName("PK_RESUMEN_AVANCE_FILA_PGPM");
            b.Property(x => x.Area).HasMaxLength(10);
            b.Property(x => x.FamiliaCodigo).HasMaxLength(30);
            b.Property(x => x.Grupo).HasMaxLength(100);
            b.Property(x => x.Nombre).HasMaxLength(100);
            b.Property(x => x.ActualizadoPor).HasMaxLength(60);
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");
        }
    }

    public class ResumenAvanceNotaConfiguration : IEntityTypeConfiguration<ResumenAvanceNota>
    {
        public void Configure(EntityTypeBuilder<ResumenAvanceNota> b)
        {
            b.ToTable("RESUMEN_AVANCE_NOTA_PGPM");
            b.HasKey(x => new { x.Area, x.ReporteId, x.FamiliaCodigo, x.SemanaFin }).HasName("PK_RESUMEN_AVANCE_NOTA_PGPM");
            b.Property(x => x.Area).HasMaxLength(10);
            b.Property(x => x.FamiliaCodigo).HasMaxLength(30);
            b.Property(x => x.SemanaFin).HasColumnType("DATE");
            b.Property(x => x.Texto).HasMaxLength(300);
            b.Property(x => x.ActualizadoPor).HasMaxLength(60);
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");
        }
    }
}
