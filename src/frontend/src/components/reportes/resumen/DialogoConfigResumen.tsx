"use client";
import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Autocomplete,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { IconArrowDown, IconArrowUp, IconPlus, IconTrash } from "@tabler/icons-react";
import { resumenAvanceService } from "@/services/resumenAvance/resumenAvanceService";
import { ApiError, ConfigResumenDto, FilaConfigResumenDto } from "@/types";

const clave = (f: { reporteId: number; familiaCodigo: string }) => `${f.reporteId}|${f.familiaCodigo}`;

/**
 * Qué filas tiene el resumen, en qué grupo y con qué nombre. Cada fila es una familia de un
 * programa de Oracle. Lo configura la gente del área (o TI).
 */
export default function DialogoConfigResumen({
  area,
  abierto,
  onClose,
  onGuardado,
}: {
  area: string;
  abierto: boolean;
  onClose: () => void;
  onGuardado: () => void;
}) {
  const [config, setConfig] = useState<ConfigResumenDto | null>(null);
  const [filas, setFilas] = useState<FilaConfigResumenDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (!abierto) return;
    setError(null);
    setConfig(null);
    setFilas([]);
    resumenAvanceService
      .getConfig(area)
      .then((c) => {
        setConfig(c);
        setFilas(c.filas);
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar la configuración."));
  }, [abierto, area]);

  const grupos = useMemo(() => [...new Set(filas.map((f) => f.grupo).filter(Boolean))], [filas]);
  const opciones = config?.opciones ?? [];

  const cambiar = (i: number, cambio: Partial<FilaConfigResumenDto>) =>
    setFilas((fs) => fs.map((f, j) => (j === i ? { ...f, ...cambio } : f)));
  const mover = (i: number, d: number) =>
    setFilas((fs) => {
      const j = i + d;
      if (j < 0 || j >= fs.length) return fs;
      const c = [...fs];
      [c[i], c[j]] = [c[j], c[i]];
      return c;
    });

  const agregar = () => {
    const usadas = new Set(filas.map(clave));
    const libre = opciones.find((o) => !usadas.has(clave(o)));
    if (!libre) return;
    setFilas((fs) => [
      ...fs,
      { grupo: fs[fs.length - 1]?.grupo ?? "Otros", nombre: libre.familiaNombre, reporteId: libre.reporteId, familiaCodigo: libre.familiaCodigo, orden: fs.length + 1 },
    ]);
  };

  const guardar = async () => {
    setGuardando(true);
    setError(null);
    try {
      await resumenAvanceService.guardarConfig(area, filas.map((f, i) => ({ ...f, orden: i + 1 })));
      onGuardado();
      onClose();
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo guardar.");
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Dialog open={abierto} onClose={onClose} maxWidth="lg" fullWidth>
      <DialogTitle>Configurar el resumen de avance</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Cada fila es una familia de un programa de reparación de Oracle. El grupo da la fila &quot;General&quot; (con los % ponderados
          por cantidad de máquinas) y el color. {config && !config.guardada && "Esta es la configuración sugerida: todavía nadie la guardó."}
        </Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {!config && !error && (
          <Typography variant="body2" sx={{ py: 2 }}>
            Cargando…
          </Typography>
        )}
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ width: 220 }}>Grupo</TableCell>
              <TableCell sx={{ width: 240 }}>Nombre en el resumen</TableCell>
              <TableCell>Programa · familia (máquinas)</TableCell>
              <TableCell align="right" sx={{ width: 120 }} />
            </TableRow>
          </TableHead>
          <TableBody>
            {filas.map((f, i) => (
              <TableRow key={`${clave(f)}-${i}`}>
                <TableCell>
                  <Autocomplete
                    freeSolo
                    size="small"
                    options={grupos}
                    value={f.grupo}
                    onInputChange={(_, v) => cambiar(i, { grupo: v })}
                    renderInput={(p) => <TextField {...p} />}
                    sx={{ minWidth: 220 }}
                  />
                </TableCell>
                <TableCell>
                  <TextField size="small" fullWidth value={f.nombre} onChange={(e) => cambiar(i, { nombre: e.target.value })} />
                </TableCell>
                <TableCell>
                  <TextField
                    select
                    size="small"
                    fullWidth
                    value={clave(f)}
                    onChange={(e) => {
                      const o = opciones.find((x) => clave(x) === e.target.value);
                      if (o) cambiar(i, { reporteId: o.reporteId, familiaCodigo: o.familiaCodigo });
                    }}
                  >
                    {opciones.map((o) => (
                      <MenuItem key={clave(o)} value={clave(o)} disabled={filas.some((x, j) => j !== i && clave(x) === clave(o))}>
                        {o.reporteNombre} · {o.familiaNombre} ({o.cantEquipo})
                      </MenuItem>
                    ))}
                  </TextField>
                </TableCell>
                <TableCell align="right" sx={{ whiteSpace: "nowrap" }}>
                  <IconButton size="small" onClick={() => mover(i, -1)} aria-label="Subir">
                    <IconArrowUp size={16} />
                  </IconButton>
                  <IconButton size="small" onClick={() => mover(i, 1)} aria-label="Bajar">
                    <IconArrowDown size={16} />
                  </IconButton>
                  <IconButton size="small" color="error" onClick={() => setFilas((fs) => fs.filter((_, j) => j !== i))} aria-label="Quitar">
                    <IconTrash size={16} />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <Stack direction="row" sx={{ mt: 1.5 }}>
          <Button startIcon={<IconPlus size={16} />} onClick={agregar} disabled={opciones.length === 0}>
            Agregar fila
          </Button>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="contained" onClick={guardar} disabled={guardando || filas.length === 0}>
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
