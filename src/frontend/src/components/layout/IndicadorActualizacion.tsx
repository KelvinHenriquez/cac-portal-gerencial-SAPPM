"use client";
import { useEffect, useState } from "react";
import { Chip, Tooltip, CircularProgress, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { IconAlertTriangle, IconCircleCheck } from "@tabler/icons-react";
import { EVENTO_DATOS_ACTUALIZADOS, sistemaService } from "@/services/sistema/sistemaService";
import { EstadoActualizacionDto } from "@/types";

const REFRESCO_MS = 5 * 60 * 1000;

const formatoFecha = new Intl.DateTimeFormat("es-DO", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

// "Última actualización" del encabezado común (sección 8.4). Si la última corrida falló o los
// datos tienen más de MONITOREO.HORAS_ALERTA horas, lo dice: el portal sigue mostrando el último
// dato bueno, pero el usuario tiene que saber que no es de hoy (sección 10).
export default function IndicadorActualizacion() {
  const [estado, setEstado] = useState<EstadoActualizacionDto | null>(null);
  const theme = useTheme();
  // En el teléfono la etiqueta completa se come el título de la página.
  const compacto = useMediaQuery(theme.breakpoints.down("sm"));

  useEffect(() => {
    let activo = true;
    const cargar = () =>
      sistemaService
        .getEstadoActualizacion()
        .then((e) => activo && setEstado(e))
        .catch(() => activo && setEstado(null));
    cargar();
    const intervalo = setInterval(cargar, REFRESCO_MS);
    // Tras "Actualizar ahora" no hay que esperar los 5 minutos del refresco.
    window.addEventListener(EVENTO_DATOS_ACTUALIZADOS, cargar);
    return () => {
      activo = false;
      clearInterval(intervalo);
      window.removeEventListener(EVENTO_DATOS_ACTUALIZADOS, cargar);
    };
  }, []);

  if (!estado) return null;

  if (estado.corridaEnCurso) {
    return (
      <Chip
        size="small"
        variant="outlined"
        color="info"
        icon={<CircularProgress size={12} sx={{ ml: 1 }} />}
        label={compacto ? "Actualizando…" : "Actualizando datos…"}
      />
    );
  }

  if (!estado.ultimaActualizacion) {
    return (
      <Chip
        size="small"
        variant="outlined"
        color="warning"
        icon={<IconAlertTriangle size={16} />}
        label="Sin datos todavía"
      />
    );
  }

  const fecha = formatoFecha.format(new Date(estado.ultimaActualizacion));
  const conProblema = estado.desactualizado || !!estado.ultimoErrorFecha;

  const detalle = [
    estado.desactualizado &&
      `Los datos tienen más de ${estado.horasAlerta} horas: la actualización automática no ha corrido con éxito.`,
    estado.ultimoErrorFecha &&
      `La última actualización (${formatoFecha.format(new Date(estado.ultimoErrorFecha))}) falló; se muestran los datos anteriores. ${estado.ultimoErrorMensaje ?? ""}`,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <Tooltip title={conProblema ? detalle : "Datos de SAP PM procesados en esta fecha"} arrow>
      <Chip
        size="small"
        variant="outlined"
        color={conProblema ? "warning" : "success"}
        icon={conProblema ? <IconAlertTriangle size={16} /> : <IconCircleCheck size={16} />}
        label={compacto ? fecha.slice(0, 5) + fecha.slice(10) : `Última actualización: ${fecha}`}
      />
    </Tooltip>
  );
}
