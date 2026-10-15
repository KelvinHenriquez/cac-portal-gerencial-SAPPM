"use client";
import { Chip, Stack, Typography } from "@mui/material";
import { fechaHora } from "@/utils/formato";

/**
 * Lo que dice el aviso de la orden sobre la máquina: si quedó parada o puede seguir trabajando
 * (pedido del Taller, 14/10/2026). Sin aviso no muestra nada.
 */
export default function AvisoParada({
  parada,
  inicio,
  fin,
  conFechas = false,
}: {
  parada: boolean | null | undefined;
  inicio?: string | null;
  fin?: string | null;
  conFechas?: boolean;
}) {
  if (parada == null) return null;
  return (
    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
      <Chip
        size="small"
        variant="outlined"
        color={parada ? "error" : "default"}
        label={parada ? "Aviso: máquina parada" : "Aviso: puede seguir trabajando"}
      />
      {conFechas && parada && (inicio || fin) && (
        <Typography variant="caption" color="text.secondary">
          Avería {inicio ? `desde ${fechaHora(inicio)}` : ""} {fin ? `hasta ${fechaHora(fin)}` : "(sin fin informado)"}
        </Typography>
      )}
    </Stack>
  );
}
