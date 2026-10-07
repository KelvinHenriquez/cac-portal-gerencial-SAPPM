"use client";
import { Box, Stack, Tooltip, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { PasosPermanenciaDto } from "@/types";

/**
 * Pasos que suman la permanencia (aviso → orden finalizada), en el orden del proceso. Colores:
 * primeros cuatro de la paleta categórica de referencia (dataviz), en orden fijo; el modo oscuro
 * usa los pasos oscuros de las mismas tonalidades. El cierre administrativo NO suma: va aparte.
 */
export const PASOS: { clave: keyof PasosPermanenciaDto; nombre: string; detalle: string; claro: string; oscuro: string }[] = [
  { clave: "respuesta", nombre: "Respuesta", detalle: "Aviso abierto → orden creada", claro: "#2a78d6", oscuro: "#3987e5" },
  { clave: "liberacion", nombre: "Liberación", detalle: "Orden creada → liberada", claro: "#eb6834", oscuro: "#d95926" },
  { clave: "esperaInicio", nombre: "Espera de inicio", detalle: "Liberada → en proceso (ENPR)", claro: "#1baf7a", oscuro: "#199e70" },
  { clave: "ejecucion", nombre: "Ejecución", detalle: "En proceso → finalizada (FINL)", claro: "#eda100", oscuro: "#c98500" },
];

/** Horas → días con un decimal ("6.5"). */
export const dias = (horas: number | null | undefined) =>
  horas == null ? "—" : (horas / 24).toLocaleString("es-DO", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** Horas → texto legible: "3.2 h" o "4.5 d". */
export const duracion = (horas: number | null | undefined) =>
  horas == null ? "—" : horas < 24 ? `${horas.toLocaleString("es-DO", { maximumFractionDigits: 1 })} h` : `${dias(horas)} d`;

export function useColoresPasos() {
  const oscuro = useTheme().palette.mode === "dark";
  return PASOS.map((p) => ({ ...p, color: oscuro ? p.oscuro : p.claro }));
}

export function LeyendaPasos() {
  const pasos = useColoresPasos();
  return (
    <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
      {pasos.map((p) => (
        <Tooltip key={p.clave} title={p.detalle} arrow>
          <Stack direction="row" spacing={0.75} alignItems="center">
            <Box sx={{ width: 12, height: 12, borderRadius: "3px", bgcolor: p.color, flexShrink: 0 }} />
            <Typography variant="caption" color="text.secondary">
              {p.nombre}
            </Typography>
          </Stack>
        </Tooltip>
      ))}
    </Stack>
  );
}

/**
 * Barra horizontal apilada con el tiempo de cada paso. `escala` = horas que ocupan el 100 % del
 * ancho (la mayor de la lista), para que las barras se comparen entre sí. Separación de 2 px entre
 * segmentos y extremos redondeados (marcas de dataviz).
 */
export function BarraPasos({ pasos, escala, alto = 14 }: { pasos: PasosPermanenciaDto; escala: number; alto?: number }) {
  const colores = useColoresPasos();
  const segmentos = colores.map((c) => ({ ...c, horas: pasos[c.clave] ?? 0 })).filter((s) => s.horas > 0);
  return (
    <Box sx={{ display: "flex", gap: "2px", height: alto, width: "100%", alignItems: "stretch" }}>
      {segmentos.map((s, i) => (
        <Tooltip key={s.clave} arrow title={`${s.nombre}: ${duracion(s.horas)} (${s.detalle})`}>
          <Box
            sx={{
              flex: `0 0 ${escala > 0 ? (100 * s.horas) / escala : 0}%`,
              minWidth: 3,
              bgcolor: s.color,
              borderTopLeftRadius: i === 0 ? 4 : 0,
              borderBottomLeftRadius: i === 0 ? 4 : 0,
              borderTopRightRadius: i === segmentos.length - 1 ? 4 : 0,
              borderBottomRightRadius: i === segmentos.length - 1 ? 4 : 0,
              cursor: "default",
            }}
          />
        </Tooltip>
      ))}
    </Box>
  );
}

/** Total de los pasos (puede diferir del promedio de permanencia si a algunas órdenes les falta un paso). */
export const sumaPasos = (p: PasosPermanenciaDto) => PASOS.reduce((t, s) => t + (p[s.clave] ?? 0), 0);
