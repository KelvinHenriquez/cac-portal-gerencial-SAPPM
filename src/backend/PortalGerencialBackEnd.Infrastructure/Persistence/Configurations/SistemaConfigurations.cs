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
            b.Property(x => x.Id).HasMaxLength(36).ValueGeneratedNever();
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

    public class AlertaEnviadaConfiguration : IEntityTypeConfiguration<AlertaEnviada>
    {
        public void Configure(EntityTypeBuilder<AlertaEnviada> b)
        {
            b.ToTable("ALERTA_ENVIADA_PGPM");
            b.HasKey(x => x.Id).HasName("PK_ALERTA_ENVIADA_PGPM");
            b.Property(x => x.Id).HasMaxLength(36).ValueGeneratedNever();
            b.Property(x => x.Tipo).HasMaxLength(40);
            b.Property(x => x.Destinatarios).HasMaxLength(1000);
            b.Property(x => x.Asunto).HasMaxLength(200);
            b.Property(x => x.Error).HasMaxLength(2000);
            b.HasIndex(x => new { x.Tipo, x.Fecha }).HasDatabaseName("IX_ALERTA_TIPO_FECHA_PGPM");
        }
    }

    public class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
    {
        private static readonly DateTime Sembrado = new(2026, 10, 1);

        private static Parametro P(string clave, string valor, string descripcion) =>
            new() { Clave = clave, Valor = valor, Descripcion = descripcion, ActualizadoEn = Sembrado };

        public void Configure(EntityTypeBuilder<Parametro> b)
        {
            b.ToTable("PARAMETRO_PGPM");
            b.HasKey(x => x.Clave).HasName("PK_PARAMETRO_PGPM");
            b.Property(x => x.Clave).HasMaxLength(60);
            b.Property(x => x.Valor).HasMaxLength(500);
            b.Property(x => x.ActualizadoPor).HasMaxLength(60);
            b.Property(x => x.Descripcion).HasMaxLength(500);
            b.Property(x => x.ActualizadoEn).HasColumnType("DATE");

            // Reglas de negocio que dependen de respuestas pendientes: se ajustan aquí, sin desplegar.
            b.HasData(
                P(ClavesParametro.TipoPorClaseOrden, "ZC01=CORRECTIVA,ZC02=PREVENTIVO,ZC03=PLANIFICADA",
                    "Clase de orden SAP -> tipo de horas (CORRECTIVA / PREVENTIVO / PLANIFICADA). Pendiente de confirmar con el ABAP (clases y ILART reales)."),
                P(ClavesParametro.EstadosCerrados, "Cerrada",
                    "Estados de orden que la cierran (fin de la indisponibilidad)."),
                P(ClavesParametro.EstadosExcluidos, "Anulada",
                    "Estados de orden que no cuentan ni para disponibilidad ni para avance."),
                P(ClavesParametro.FechaCargaInicial, "2026-06-01",
                    "Fecha (yyyy-MM-dd) desde la que se extrae de SAP la primera vez: salida en vivo de SAP PM."),
                P(ClavesParametro.ClasesOrdenDisponibilidad, "ZC01,ZC02",
                    "Clases de orden que dejan equipos fuera de servicio, separadas por coma; * = todas. Cuentan correctivas y preventivas; las predictivas del programa de reparación (ZC03) no (respuesta del 02/10/2026)."),
                P(ClavesParametro.InicioVentana, "CREACION",
                    "Desde cuándo cuenta fuera de servicio: CREACION de la orden (respuesta del 01/10/2026) o INICIO_REAL."),
                P(ClavesParametro.CorrectivasSegunAviso, "S",
                    "S = una correctiva con aviso solo deja el equipo fuera si el aviso indica parada, desde el inicio hasta el fin de la avería."),
                P(ClavesParametro.ClasesOrdenAvance, "ZC03",
                    "Clases de orden (separadas por coma) del programa de reparación que cuentan para Avance."),
                P(ClavesParametro.BasePorcentajeDisponibilidad, "REQUERIDOS",
                    "Base del % de disponibilidad: REQUERIDOS (disponibles / requeridos, topado en 100) o EQUIPOS (disponibles / equipos de la asignación, como el PL/SQL de Oracle PR_DISPONIBILIDAD_EQUIPOS)."),
                P(ClavesParametro.UsuariosHojaRuta, "-",
                    "Usuarios SAP (separados por coma) que crean las operaciones de la hoja de ruta (Real Programado); las creadas por otros usuarios son agregadas (Real No Programado). \"-\" = sin definir: se usa la marca que traiga SAP (respuesta del 02/10/2026)."),
                P(ClavesParametro.BasePlanAvance, "DIAS",
                    "Con qué se arma el % programado de cada semana: DIAS (respuesta del 02/10/2026: por actividades puede engañar si hay muchas actividades chicas) o ACTIVIDADES."),
                P(ClavesParametro.HoraCorridaDiaria, "05:00",
                    "Hora local (HH:mm) de la corrida diaria del job de extracción y recálculo."),
                P(ClavesParametro.DiasRecalculoDisponibilidad, "120",
                    "Días hacia atrás que se recalcula la disponibilidad en cada corrida."),
                P(ClavesParametro.HorasAlertaFrescura, "24",
                    "Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11)."));
        }
    }
}
