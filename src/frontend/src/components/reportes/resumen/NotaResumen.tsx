"use client";
import { useState } from "react";
import { Chip, IconButton, Stack, TextField, Tooltip, Typography } from "@mui/material";
import { IconCheck, IconPencil, IconX } from "@tabler/icons-react";
import { fechaHora } from "@/utils/formato";

/**
 * Nota de una fila del resumen ("2 pendiente revisión"): se ve como un chip; quien puede editar la
 * cambia con el lápiz y la guarda con el visto. Vacía = se borra.
 */
export default function NotaResumen({
  nota,
  por,
  en,
  puedeEditar,
  onGuardar,
}: {
  nota: string | null;
  por: string | null;
  en: string | null;
  puedeEditar: boolean;
  onGuardar: (texto: string) => Promise<void>;
}) {
  const [editando, setEditando] = useState(false);
  const [texto, setTexto] = useState(nota ?? "");
  const [guardando, setGuardando] = useState(false);

  const guardar = async () => {
    setGuardando(true);
    try {
      await onGuardar(texto.trim());
      setEditando(false);
    } finally {
      setGuardando(false);
    }
  };

  if (editando)
    return (
      <Stack direction="row" spacing={0.5} alignItems="center" className="no-imprimir-borde">
        <TextField
          size="small"
          autoFocus
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") guardar();
            if (e.key === "Escape") setEditando(false);
          }}
          placeholder="Ej.: 2 pendiente revisión"
          slotProps={{ htmlInput: { maxLength: 300 } }}
          sx={{ minWidth: 200 }}
        />
        <IconButton size="small" color="primary" onClick={guardar} disabled={guardando} aria-label="Guardar nota">
          <IconCheck size={18} />
        </IconButton>
        <IconButton size="small" onClick={() => setEditando(false)} disabled={guardando} aria-label="Cancelar">
          <IconX size={18} />
        </IconButton>
      </Stack>
    );

  return (
    <Stack direction="row" spacing={0.5} alignItems="center">
      {nota ? (
        <Tooltip title={por && en ? `${por} · ${fechaHora(en)}` : ""}>
          <Chip size="small" label={nota} sx={{ maxWidth: 260, "& .MuiChip-label": { whiteSpace: "normal" }, height: "auto", py: 0.25 }} />
        </Tooltip>
      ) : (
        puedeEditar && (
          <Typography variant="caption" color="text.disabled" className="no-imprimir">
            —
          </Typography>
        )
      )}
      {puedeEditar && (
        <IconButton
          size="small"
          className="no-imprimir"
          onClick={() => {
            setTexto(nota ?? "");
            setEditando(true);
          }}
          aria-label="Editar nota"
        >
          <IconPencil size={16} />
        </IconButton>
      )}
    </Stack>
  );
}
