// Cómo se colorea el atraso del avance (ejecutado - programado, en puntos). La spec no define
// umbrales para este reporte: 5 puntos de tolerancia es un supuesto a confirmar con taller.
export const TOLERANCIA_ATRASO = 5;

export type NivelAtraso = "ok" | "leve" | "grave";

export function nivelAtraso(diferencia: number): NivelAtraso {
  if (diferencia >= 0) return "ok";
  return diferencia >= -TOLERANCIA_ATRASO ? "leve" : "grave";
}

export const COLOR_ATRASO: Record<NivelAtraso, string> = {
  ok: "#17663A",
  leve: "#9A5B00",
  grave: "#B42318",
};
