"use client";
import { Box, Stack, Typography } from "@mui/material";
import { OrdenResumenDto } from "@/types";
import { fechaCorta } from "@/utils/formato";

/** Órdenes que dejan al equipo fuera de servicio, en formato compacto. */
export default function ListaOrdenes({ ordenes, mostrarEquipo }: { ordenes: OrdenResumenDto[]; mostrarEquipo?: boolean }) {
  if (ordenes.length === 0) {
    return (
      <Typography variant="body2" color="text.disabled">
        —
      </Typography>
    );
  }
  return (
    <Stack spacing={0.5}>
      {ordenes.map((o) => (
        <Box key={o.ordenSapId} sx={{ fontSize: "0.8125rem", lineHeight: 1.4 }}>
          {mostrarEquipo && (
            <Box component="span" sx={{ fontWeight: 600, mr: 1 }}>
              {o.codigoLegado}
            </Box>
          )}
          <Box component="span" sx={{ fontWeight: 600, fontVariantNumeric: "tabular-nums" }}>
            {o.ordenSapId}
          </Box>{" "}
          <Box component="span" sx={{ color: "text.secondary" }}>
            {o.claseOrden} · {o.estadoOrden} · desde {o.fechaInicioReal ? fechaCorta(o.fechaInicioReal) : "?"}
            {o.fechaFinReal ? ` hasta ${fechaCorta(o.fechaFinReal)}` : ""}
          </Box>
          {o.descripcion && (
            <Box component="span" sx={{ color: "text.secondary", display: "block" }}>
              {o.descripcion}
            </Box>
          )}
        </Box>
      ))}
    </Stack>
  );
}
