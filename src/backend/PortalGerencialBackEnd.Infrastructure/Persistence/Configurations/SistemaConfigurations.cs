using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Entities;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Configurations
{
    public class LogExtraccionConfiguration : IEntityTypeConfiguration<LogExtraccion>
    {
        public void Configure(EntityTypeBuilder<LogExtraccion> b)
        {
            b.ToTable("LOG_EXTRACCION_PGPM");
            b.HasKey(x => x.Id).HasName("PK_LOG_EXTRACCION_PGPM");
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.CorridaId).HasMaxLength(36);
            b.Property(x => x.Fuente).HasMaxLength(30);
            b.Property(x => x.Estado).HasMaxLength(20);
            b.Property(x => x.FechaCorteDesde).HasColumnType("DATE");
            b.Property(x => x.Origen).HasMaxLength(20);
            b.Property(x => x.ErrorMensaje).HasColumnType("CLOB");

            // "¿Cuándo corrió por última vez con éxito cada proceso?" (sección 11).
            b.HasIndex(x => new { x.Fuente, x.Estado, x.Inicio }).HasDatabaseName("IX_LOG_FUENTE_ESTADO_PGPM");
            b.HasIndex(x => x.CorridaId).HasDatabaseName("IX_LOG_CORRIDA_PGPM");
        }
    }

    public class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
    {
        private static readonly DateTime Sembrado = new(2026, 9, 30);

        public void Configure(EntityTypeBuilder<Parametro> b)
        {
            b.ToTable("PARAMETRO_PGPM");
            b.HasKey(x => x.Clave).HasName("PK_PARAMETRO_PGPM");
            b.Property(x => x.Clave).HasMaxLength(60);
            b.Property(x => x.Valor).HasMaxLength(500);
            b.Property(x => x.Descripcion).HasMaxLength(500);
            b.Property(x => x.ActualizadoEn).HasDefaultValueSql("SYSTIMESTAMP");

            // Supuestos iniciales: los marcados "pendiente" se ajustan cuando taller los confirme.
            b.HasData(
                new Parametro
                {
                    Clave = ClavesParametro.EstadosFueraServicio,
                    Valor = "Liberado,En Proceso",
                    Descripcion = "Estados de orden que dejan al equipo fuera de servicio (pendiente de confirmar con taller, sección 6.1).",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.EstadosCerrados,
                    Valor = "Cerrado tecnicamente,Cerrado",
                    Descripcion = "Estados de orden que cuentan como orden terminada para el avance de reparación.",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.ClasesOrdenAvance,
                    Valor = "ZC03",
                    Descripcion = "Clases de orden (separadas por coma) que cuentan para Avance de Reparación (sección 4.2).",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.HoraCorridaDiaria,
                    Valor = "05:00",
                    Descripcion = "Hora local (HH:mm) de la corrida diaria del job de extracción y recálculo.",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.DiasRecalculoDisponibilidad,
                    Valor = "120",
                    Descripcion = "Días hacia atrás que se recalcula la disponibilidad diaria en cada corrida.",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.HorasAlertaFrescura,
                    Valor = "24",
                    Descripcion = "Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11).",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.ProgramaVigente,
                    Valor = "2026",
                    Descripcion = "Programa de reparación que se abre por defecto en Avance de Reparación.",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.FormulaAvanceReal,
                    Valor = "HH_ORDENES_CERRADAS",
                    Descripcion = "Fórmula provisional del % real de avance (pendiente 'Total Ejec. %'): HH_ORDENES_CERRADAS u ORDENES_CERRADAS.",
                    ActualizadoEn = Sembrado
                },
                new Parametro
                {
                    Clave = ClavesParametro.FechaCargaInicial,
                    Valor = "2026-01-01",
                    Descripcion = "Fecha (yyyy-MM-dd) desde la que se extrae de SAP en la primera corrida.",
                    ActualizadoEn = Sembrado
                });
        }
    }
}
