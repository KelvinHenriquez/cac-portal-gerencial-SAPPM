"use client";
import { Paper, Stack, Typography, Chip } from "@mui/material";
import { IconBarrierBlock } from "@tabler/icons-react";

// Marcador de las pantallas que se construyen en etapas posteriores del plan (docs/ETAPAS.md).
export default function EnConstruccion({ etapa, descripcion }: { etapa: string; descripcion: string }) {
  return (
    <Paper variant="outlined" sx={{ p: { xs: 3, md: 5 }, textAlign: "center" }}>
      <Stack spacing={2} alignItems="center">
        <IconBarrierBlock size={48} stroke={1.25} />
        <Chip label={etapa} color="primary" variant="outlined" size="small" />
        <Typography variant="body1" color="text.secondary" maxWidth={560}>
          {descripcion}
        </Typography>
      </Stack>
    </Paper>
  );
}
