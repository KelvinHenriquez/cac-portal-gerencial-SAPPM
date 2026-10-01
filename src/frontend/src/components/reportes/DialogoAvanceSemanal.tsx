"use client";
import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  Grid2 as Grid,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { avanceService } from "@/services/avance/avanceService";
import { ApiError, DetalleSemanaAvanceDto } from "@/types";
import { fechaConDia, fechaHora, fechaLarga } from "@/utils/formato";
import { num } from "./comunes";

export interface SeleccionSemanaAvance {
  familiaCodigo: string;
  noSemana: number;
}

/** Drill-down "Avance Semanal" de una familia, como la pantalla de Oracle. */
export default function DialogoAvanceSemanal({
  reporteId,
  seleccion,
  onClose,
}: {
  reporteId: number;
  seleccion: SeleccionSemanaAvance | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<DetalleSemanaAvanceDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pestana, setPestana] = useState(0);
  const [sistema, setSistema] = useState("");

  useEffect(() => {
    if (!seleccion) return;
    setDatos(null);
    setError(null);
    setPestana(0);
    setSistema("");
    avanceService
      .getDetalleSemana(reporteId, seleccion.familiaCodigo, seleccion.noSemana)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el detalle."));
  }, [reporteId, seleccion]);

  const sistemas = useMemo(
    () => Array.from(new Set(datos?.actividades.map((a) => a.sistema ?? "(sin sistema)") ?? [])).sort(),
    [datos]
  );
  const actividades = datos?.actividades.filter((a) => !sistema || (a.sistema ?? "(sin sistema)") === sistema) ?? [];

  return (
    <Dialog open={!!seleccion} onClose={onClose} fullWidth maxWidth="xl" fullScreen={pantallaChica}>
      <DialogTitle>
        {datos ? `${datos.familiaNombre} — Semana #${datos.noSemana}` : "Avance semanal"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            Del {fechaLarga(datos.fechaInicio)} al {fechaLarga(datos.fechaFin)}
          </Typography>
        )}
      </DialogTitle>
      <DialogContent dividers>
        {error && <Alert severity="error">{error}</Alert>}
        {!datos && !error && (
          <Box sx={{ display: "flex", justifyContent: "center", py: 6 }}>
            <CircularProgress />
          </Box>
        )}
        {datos && (
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1 }}>
                Resumen semana
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableBody>
                    {[
                      ["Programado", datos.programado],
                      ["Real Ej. Programado", datos.realEjProg],
                      ["Real Ej. No Programado", datos.realEjNoProg],
                      ["Real Ejecutado (actividades)", datos.realEjecutado],
                      ["Real por órdenes cerradas", datos.realOrdenes],
                    ].map(([etiqueta, valor]) => (
                      <TableRow key={etiqueta as string}>
                        <TableCell sx={{ fontWeight: 600 }}>{etiqueta}</TableCell>
                        <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }}>
                          {num(valor as number)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>

              <Typography variant="subtitle2" fontWeight={700} sx={{ mt: 2, mb: 1 }}>
                Resumen días
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Día</TableCell>
                      <TableCell align="right">Actividades terminadas</TableCell>
                      <TableCell align="right">Órdenes cerradas</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {datos.dias.map((d) => (
                      <TableRow key={d.fecha}>
                        <TableCell sx={{ textTransform: "capitalize", whiteSpace: "nowrap" }}>{fechaConDia(d.fecha)}</TableCell>
                        <TableCell align="right">{d.actTerminadas}</TableCell>
                        <TableCell align="right">{d.ordenesCerradas}</TableCell>
                      </TableRow>
                    ))}
                    <TableRow sx={{ "& td": { fontWeight: 700 } }}>
                      <TableCell>Total</TableCell>
                      <TableCell align="right">{datos.dias.reduce((s, d) => s + d.actTerminadas, 0)}</TableCell>
                      <TableCell align="right">{datos.dias.reduce((s, d) => s + d.ordenesCerradas, 0)}</TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Grid>

            <Grid size={{ xs: 12, md: 8 }}>
              <Tabs value={pestana} onChange={(_, v) => setPestana(v)} sx={{ mb: 1 }}>
                <Tab label={`Actividades terminadas (${datos.actividades.length})`} />
                <Tab label={`Órdenes cerradas (${datos.ordenesCerradas.length})`} />
              </Tabs>
              {pestana === 0 && (
                <>
                  <FormControl size="small" sx={{ minWidth: 220, mb: 1 }}>
                    <InputLabel id="sistema">Sistema</InputLabel>
                    <Select labelId="sistema" label="Sistema" value={sistema} onChange={(e) => setSistema(e.target.value)}>
                      <MenuItem value="">Todos</MenuItem>
                      {sistemas.map((s) => (
                        <MenuItem key={s} value={s}>
                          {s}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 420 }}>
                    <Table size="small" stickyHeader>
                      <TableHead>
                        <TableRow>
                          <TableCell>No. OT</TableCell>
                          <TableCell>Ficha</TableCell>
                          <TableCell>Actividad</TableCell>
                          <TableCell>Sistema</TableCell>
                          <TableCell>Sub-sistema</TableCell>
                          <TableCell>Componente</TableCell>
                          <TableCell>Terminada</TableCell>
                          <TableCell>Plan</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {actividades.map((a) => (
                          <TableRow key={`${a.ordenSapId}-${a.operacion}`} hover>
                            <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{a.ordenSapId}</TableCell>
                            <TableCell>{a.ficha}</TableCell>
                            <TableCell>{a.texto}</TableCell>
                            <TableCell>{a.sistema}</TableCell>
                            <TableCell>{a.subsistema}</TableCell>
                            <TableCell>{a.componente}</TableCell>
                            <TableCell sx={{ whiteSpace: "nowrap" }}>{a.fechaTerminada ? fechaHora(a.fechaTerminada) : "—"}</TableCell>
                            <TableCell>{a.programada ? "Programada" : "No programada"}</TableCell>
                          </TableRow>
                        ))}
                        {actividades.length === 0 && (
                          <TableRow>
                            <TableCell colSpan={8} align="center" sx={{ color: "text.secondary", py: 3 }}>
                              Ninguna actividad terminada esta semana.
                            </TableCell>
                          </TableRow>
                        )}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </>
              )}
              {pestana === 1 && (
                <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 460 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell>No. OT</TableCell>
                        <TableCell>Ficha</TableCell>
                        <TableCell>Descripción</TableCell>
                        <TableCell>Cierre técnico</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {datos.ordenesCerradas.map((o) => (
                        <TableRow key={o.ordenSapId} hover>
                          <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{o.ordenSapId}</TableCell>
                          <TableCell>{o.ficha}</TableCell>
                          <TableCell>{o.descripcion}</TableCell>
                          <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaHora(o.fechaCierre)}</TableCell>
                        </TableRow>
                      ))}
                      {datos.ordenesCerradas.length === 0 && (
                        <TableRow>
                          <TableCell colSpan={4} align="center" sx={{ color: "text.secondary", py: 3 }}>
                            Ninguna orden se cerró esta semana.
                          </TableCell>
                        </TableRow>
                      )}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </Grid>
          </Grid>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Regresar</Button>
      </DialogActions>
    </Dialog>
  );
}
