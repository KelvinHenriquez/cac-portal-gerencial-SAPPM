"use client";
import { Box, Chip } from "@mui/material";
import { ColorSemaforo, EstadoEquipo } from "@/types";
import { porcentaje } from "@/utils/formato";

// Colores como el reporte de Oracle: texto rojo bajo Comp. 1, naranja bajo Comp. 2 y negro desde
// ahí. El valor siempre está escrito: el color nunca es la única señal.
export const COLOR_TEXTO: Record<ColorSemaforo, string> = {
  ROJO: "#D32F2F",
  NARANJA: "#E65100",
  VERDE: "inherit",
};

// Fondo de la semana actual, como el verde con que Oracle la resalta.
export const FONDO_SEMANA_ACTUAL = "#E3F5E9";

export function Pct({ valor, color, negrita }: { valor: number | null | undefined; color?: ColorSemaforo | null; negrita?: boolean }) {
  if (valor == null) {
    return (
      <Box component="span" sx={{ color: "text.disabled" }}>
        —
      </Box>
    );
  }
  return (
    <Box
      component="span"
      sx={{ color: color ? COLOR_TEXTO[color] : "inherit", fontWeight: negrita || (color && color !== "VERDE") ? 700 : 500, fontVariantNumeric: "tabular-nums" }}
    >
      {porcentaje(valor)}
    </Box>
  );
}

/** Número con dos decimales (horas, porcentajes del reporte de avance). */
export const num = (valor: number | null | undefined) =>
  valor == null ? "—" : valor.toLocaleString("es-DO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const ESTADOS: Record<EstadoEquipo, { etiqueta: string; color: "error" | "success" | "default" | "warning" }> = {
  FUERA_SERVICIO: { etiqueta: "Fuera de servicio", color: "error" },
  DISPONIBLE: { etiqueta: "Disponible", color: "success" },
  INACTIVO: { etiqueta: "Inactivo", color: "default" },
  SIN_SAP: { etiqueta: "No está en SAP", color: "warning" },
};

export function ChipEstadoEquipo({ estado }: { estado: EstadoEquipo }) {
  const e = ESTADOS[estado];
  return <Chip size="small" label={e.etiqueta} color={e.color} variant={estado === "FUERA_SERVICIO" ? "filled" : "outlined"} />;
}

export const etiquetaEstadoEquipo = (estado: EstadoEquipo) => ESTADOS[estado].etiqueta;

export const TIPO_ORDEN: Record<string, string> = {
  PLANIFICADA: "Planificada",
  PREVENTIVO: "Preventivo",
  CORRECTIVA: "Correctiva",
};

/** Primera columna fija al desplazar la tabla a lo ancho. */
export const celdaFija = {
  position: "sticky" as const,
  left: 0,
  zIndex: 1,
  bgcolor: "background.paper",
  minWidth: { xs: 140, sm: 200 },
  borderRight: 1,
  borderColor: "divider",
};
