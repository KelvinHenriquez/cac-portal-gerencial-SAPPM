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
                P(ClavesParametro.ClasesOrdenDisponibilidad, "ZC01",
                    "Clases de orden que dejan equipos fuera de servicio, separadas por coma; * = todas. Como Oracle, solo las correctivas (ZC01) (decisión del 06/10/2026; antes ZC01,ZC02)."),
                P(ClavesParametro.InicioVentana, "CREACION",
                    "Desde cuándo cuenta fuera de servicio: CREACION de la orden (respuesta del 01/10/2026) o INICIO_REAL."),
                P(ClavesParametro.CorrectivasSegunAviso, "N",
                    "S = una correctiva con aviso solo deja el equipo fuera si el aviso indica parada, desde el inicio hasta el fin de la avería. N = como Oracle: toda correctiva abierta lo deja fuera (decisión del 06/10/2026)."),
                P(ClavesParametro.ClasesOrdenAvance, "ZC03",
                    "Clases de orden (separadas por coma) del programa de reparación que cuentan para Avance."),
                P(ClavesParametro.BasePorcentajeDisponibilidad, "EQUIPOS",
                    "Base del % de disponibilidad: EQUIPOS (disponibles / equipos de la asignación, como Oracle; decisión del 06/10/2026) o REQUERIDOS (disponibles / requeridos, topado en 100)."),
                P(ClavesParametro.UsuariosHojaRuta, "-",
                    "Usuarios SAP (separados por coma) que crean las operaciones de la hoja de ruta (Real Programado); las creadas por otros usuarios son agregadas (Real No Programado). \"-\" = sin definir: se usa la marca que traiga SAP (respuesta del 02/10/2026)."),
                P(ClavesParametro.ClasesOrdenPermanencia, "ZC01,ZI01",
                    "Clases de orden correctivas del reporte Tiempo de Permanencia: ZC01 (Centro de Servicios) y ZI01 (Industrial) (06/10/2026)."),
                P(ClavesParametro.AreasPermanencia, "CDS=Centro de Servicios,IND=Industrial",
                    "Áreas del reporte de permanencia por grupo de planificación (INGRP) del equipo en SAP: CODIGO=Nombre, separadas por coma."),
                P(ClavesParametro.EstadosUsuarioPermanencia, "ENPR=E0002,FINL=E0003",
                    "Estados de usuario de las órdenes PM: ENPR = en proceso, FINL = finalizada (código del perfil de estados en JCDS)."),
                P(ClavesParametro.EstadoEsperaRepuestos, "-",
                    "Código del estado de usuario 'en espera de repuestos' (p. ej. E0004). \"-\" = todavía no existe en SAP: el tiempo de espera queda dentro de la ejecución."),
                P(ClavesParametro.PesoActividadAvance, "CANTIDAD",
                    "Avance por actividades: cuánto vale cada actividad terminada. CANTIDAD = 1 cada una, como Oracle; HORAS_PLAN = su trabajo planificado; DIAS = duración entre sus fechas plan (en SAP suelen ser iguales: daría 0) (07/10/2026)."),
                P(ClavesParametro.FechaOrdenTerminadaAvance, "FIN_REAL",
                    "Avance por órdenes: una orden cerrada cuenta en la semana de su FIN_REAL (cuando terminó el trabajo) o de su CIERRE_TECNICO en SAP (decisión del 06/10/2026: fin real)."),
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
