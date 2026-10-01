"use client";
import { Box, Chip } from "@mui/material";
import { ColorSemaforo, EstadoEquipo } from "@/types";
import { porcentaje } from "@/utils/formato";

// Colores propios del semáforo: el "success" del tema es el teal de la marca, no un verde de
// semáforo. Fondo suave + texto oscuro para que el número se lea, y el valor siempre visible
// (el color nunca es la única señal).
export const COLORES_SEMAFORO: Record<ColorSemaforo, { fondo: string; texto: string; borde: string; nombre: string }> = {
  ROJO: { fondo: "#FDE8E4", texto: "#B42318", borde: "#F4B4A8", nombre: "Rojo" },
  NARANJA: { fondo: "#FFF1DB", texto: "#9A5B00", borde: "#F7C877", nombre: "Naranja" },
  VERDE: { fondo: "#E3F5E9", texto: "#17663A", borde: "#9BD6B0", nombre: "Verde" },
};

export function CeldaPorcentaje({
  valor,
  color,
  negrita,
}: {
  valor: number | null;
  color: ColorSemaforo | null;
  negrita?: boolean;
}) {
  if (valor == null || color == null) {
    return (
      <Box component="span" sx={{ color: "text.disabled" }}>
        —
      </Box>
    );
  }
  const c = COLORES_SEMAFORO[color];
  return (
    <Box
      component="span"
      sx={{
        display: "inline-block",
        minWidth: 64,
        px: 1,
        py: 0.25,
        borderRadius: "6px",
        bgcolor: c.fondo,
        color: c.texto,
        border: `1px solid ${c.borde}`,
        fontWeight: negrita ? 700 : 600,
        fontVariantNumeric: "tabular-nums",
        textAlign: "center",
      }}
    >
      {porcentaje(valor)}
    </Box>
  );
}

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

export function etiquetaEstadoEquipo(estado: EstadoEquipo) {
  return ESTADOS[estado].etiqueta;
}
