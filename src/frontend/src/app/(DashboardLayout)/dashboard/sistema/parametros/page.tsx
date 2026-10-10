"use client";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { IconPencil } from "@tabler/icons-react";
import { sistemaService } from "@/services/sistema/sistemaService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, ParametroDto, ParametrosDto } from "@/types";
import { fechaHora } from "@/utils/formato";

/** Parámetros de cálculo: todos los ven, solo los administradores del portal los cambian. */
export default function ParametrosPage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [datos, setDatos] = useState<ParametrosDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [editando, setEditando] = useState<ParametroDto | null>(null);
  const [valor, setValor] = useState("");
  const [errorEdicion, setErrorEdicion] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const cargar = useCallback(async () => {
    showLoading("Cargando parámetros...");
    try {
      setDatos(await sistemaService.getParametros());
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudieron cargar los parámetros.");
    } finally {
      hideLoading();
    }
  }, [showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const secciones = useMemo(() => {
    const grupos = new Map<string, ParametroDto[]>();
    datos?.parametros.forEach((p) => grupos.set(p.seccion, [...(grupos.get(p.seccion) ?? []), p]));
    return [...grupos.entries()];
  }, [datos]);

  const abrir = (p: ParametroDto) => {
    setEditando(p);
    setValor(p.valor);
    setErrorEdicion(null);
  };

  const guardar = async () => {
    if (!editando) return;
    setGuardando(true);
    setErrorEdicion(null);
    try {
      const actualizado = await sistemaService.actualizarParametro(editando.clave, valor);
      setDatos((d) => d && { ...d, parametros: d.parametros.map((p) => (p.clave === actualizado.clave ? actualizado : p)) });
      setAviso(
        `"${actualizado.nombre}" quedó en ${actualizado.valor}. Los reportes lo usan desde la próxima actualización de datos (o use "Actualizar ahora" en Estado de actualización).`
      );
      setEditando(null);
    } catch (e) {
      setErrorEdicion((e as ApiError)?.errorMessage || "No se pudo guardar.");
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h6">Parámetros de cálculo</Typography>
        <Typography variant="body2" color="text.secondary">
          Reglas con las que el portal calcula la disponibilidad y el avance.{" "}
          {datos && !datos.puedeEditar && "Puede consultarlas; para cambiarlas hace falta el rol de parámetros."}
          {datos?.puedeEditar && "Los parámetros técnicos (SAP y horario de la actualización) solo los cambia TI."}
        </Typography>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}
      {aviso && (
        <Alert severity="success" onClose={() => setAviso(null)}>
          {aviso}
        </Alert>
      )}

      {secciones.map(([seccion, parametros]) => (
        <Paper key={seccion} variant="outlined">
          <Typography variant="subtitle1" sx={{ px: 2, pt: 1.5, fontWeight: 700 }}>
            {seccion}
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: "32%" }}>Parámetro</TableCell>
                  <TableCell>Valor</TableCell>
                  <TableCell sx={{ display: { xs: "none", md: "table-cell" } }}>Último cambio</TableCell>
                  {datos?.puedeEditar && <TableCell align="right" />}
                </TableRow>
              </TableHead>
              <TableBody>
                {parametros.map((p) => (
                  <TableRow key={p.clave} hover>
                    <TableCell>
                      <Typography variant="body2" fontWeight={600}>
                        {p.nombre}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" display="block">
                        {p.descripcion}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ fontFamily: "monospace", wordBreak: "break-word" }}>{p.valor}</TableCell>
                    <TableCell sx={{ display: { xs: "none", md: "table-cell" }, whiteSpace: "nowrap" }}>
                      {p.actualizadoPor ? `${p.actualizadoPor} · ${fechaHora(p.actualizadoEn)}` : "Valor inicial"}
                    </TableCell>
                    {datos?.puedeEditar && (
                      <TableCell align="right">
                        {p.editable ? (
                          <Button size="small" startIcon={<IconPencil size={16} />} onClick={() => abrir(p)}>
                            Cambiar
                          </Button>
                        ) : (
                          <Typography variant="caption" color="text.secondary">
                            Solo TI
                          </Typography>
                        )}
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      ))}

      <Dialog open={editando != null} onClose={() => !guardando && setEditando(null)} fullWidth maxWidth="sm">
        <DialogTitle>{editando?.nombre}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {editando?.descripcion}
            </Typography>
            {editando?.opciones ? (
              <FormControl fullWidth size="small">
                <InputLabel id="valor-parametro">Valor</InputLabel>
                <Select labelId="valor-parametro" label="Valor" value={valor} onChange={(e) => setValor(e.target.value)}>
                  {editando.opciones.map((o) => (
                    <MenuItem key={o} value={o}>
                      {o}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            ) : (
              <TextField label="Valor" size="small" fullWidth value={valor} onChange={(e) => setValor(e.target.value)} autoFocus />
            )}
            {errorEdicion && <Alert severity="error">{errorEdicion}</Alert>}
            <Box>
              <Typography variant="caption" color="text.secondary">
                Clave técnica: {editando?.clave}
              </Typography>
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditando(null)} disabled={guardando}>
            Cancelar
          </Button>
          <Button variant="contained" onClick={guardar} disabled={guardando || valor.trim() === editando?.valor}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
