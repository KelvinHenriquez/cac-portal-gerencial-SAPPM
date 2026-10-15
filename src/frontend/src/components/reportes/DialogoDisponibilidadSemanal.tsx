"use client";
import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid2 as Grid,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { disponibilidadService } from "@/services/disponibilidad/disponibilidadService";
import { ApiError, DetalleSemanaDto, DiaDetalleDto, HoraDisponibilidadDto, OrdenDetalleDto } from "@/types";
import { fechaConDia, fechaCorta, fechaHora, fechaLarga } from "@/utils/formato";
import { COLOR_TEXTO, num, Pct, TIPO_ORDEN } from "./comunes";
import AvisoParada from "./AvisoParada";

export interface SeleccionSemana {
  asignacionId: number;
  noSemana: number;
}

// Filas de la matriz, en el orden de la pantalla de Oracle.
const FILAS: { etiqueta: string; valor: (d: DiaDetalleDto) => React.ReactNode }[] = [
  { etiqueta: "Disp. Real %", valor: (d) => <Pct valor={d.dispRealPct} color={d.semaforo} /> },
  { etiqueta: "Disp. Comp. %", valor: (d) => num(d.dispCompPct) },
  { etiqueta: "Hrs al día", valor: (d) => num(d.hrsDia) },
  { etiqueta: "Hrs. OT Planificada", valor: (d) => num(d.hrsOtPlanificada) },
  { etiqueta: "Hrs. OT Preventivo", valor: (d) => num(d.hrsOtPreventivo) },
  { etiqueta: "Hrs. OT Correctiva", valor: (d) => num(d.hrsOtCorrectiva) },
  { etiqueta: "Hrs Real Efec.", valor: (d) => num(d.hrsRealEfec) },
  { etiqueta: "Hrs. Día Disp. Comp.", valor: (d) => num(d.hrsDiaDispComp) },
];

/** Drill-down "Disponibilidad Semanal" de una asignación, como la pantalla de Oracle. */
export default function DialogoDisponibilidadSemanal({
  reporteId,
  seleccion,
  onClose,
}: {
  reporteId: number;
  seleccion: SeleccionSemana | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<DetalleSemanaDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dia, setDia] = useState<string | null>(null);
  const [horas, setHoras] = useState<HoraDisponibilidadDto[] | null>(null);
  const [orden, setOrden] = useState<OrdenDetalleDto | null>(null);

  useEffect(() => {
    if (!seleccion) return;
    setDatos(null);
    setError(null);
    setDia(null);
    setOrden(null);
    disponibilidadService
      .getDetalleSemana(reporteId, seleccion.asignacionId, seleccion.noSemana)
      .then((d) => {
        setDatos(d);
        const ultimo = d.dias[d.dias.length - 1]?.fecha ?? null;
        setDia(ultimo);
        setOrden(d.ordenes[0] ?? null);
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el detalle."));
  }, [reporteId, seleccion]);

  useEffect(() => {
    if (!seleccion || !dia) return;
    setHoras(null);
    disponibilidadService
      .getHoras(reporteId, seleccion.asignacionId, dia)
      .then(setHoras)
      .catch(() => setHoras([]));
  }, [reporteId, seleccion, dia]);

  return (
    <Dialog open={!!seleccion} onClose={onClose} fullWidth maxWidth="xl" fullScreen={pantallaChica}>
      <DialogTitle>
        {datos ? datos.asignacion : "Disponibilidad semanal"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            Semana No. {datos.noSemana} · del {fechaLarga(datos.fechaInicio)} al {fechaLarga(datos.fechaFin)}. Toque un día
            para ver su disponibilidad hora por hora.
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
        {datos && datos.dias.length === 0 && <Alert severity="info">No hay días calculados en esta semana.</Alert>}
        {datos && datos.dias.length > 0 && (
          <Grid container spacing={2}>
            <Grid size={12}>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell />
                      {datos.dias.map((d) => (
                        <TableCell
                          key={d.fecha}
                          align="right"
                          onClick={() => setDia(d.fecha)}
                          sx={{
                            cursor: "pointer",
                            whiteSpace: "nowrap",
                            textTransform: "capitalize",
                            fontWeight: 700,
                            bgcolor: dia === d.fecha ? "primary.light" : undefined,
                          }}
                        >
                          {fechaConDia(d.fecha!)}
                        </TableCell>
                      ))}
                      <TableCell align="right" sx={{ fontWeight: 700, bgcolor: "grey.100", whiteSpace: "nowrap" }}>
                        Resumen sem.
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {FILAS.map((f) => (
                      <TableRow key={f.etiqueta}>
                        <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{f.etiqueta}</TableCell>
                        {datos.dias.map((d) => (
                          <TableCell key={d.fecha} align="right" sx={{ fontVariantNumeric: "tabular-nums", bgcolor: dia === d.fecha ? "primary.light" : undefined }}>
                            {f.valor(d)}
                          </TableCell>
                        ))}
                        <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums", bgcolor: "grey.100", fontWeight: 600 }}>
                          {datos.resumen ? f.valor(datos.resumen) : "—"}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Grid>

            <Grid size={{ xs: 12, md: 5 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1 }}>
                Disponibilidad {dia ? `del ${fechaLarga(dia)}` : ""}
              </Typography>
              <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 340 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>Hora</TableCell>
                      <TableCell align="right">Cant. Eq.</TableCell>
                      <TableCell align="right">Eq. Req.</TableCell>
                      <TableCell align="right">Eq. Disp.</TableCell>
                      <TableCell align="right">Disp. %</TableCell>
                      <TableCell align="right">Pot. %</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {horas == null && (
                      <TableRow>
                        <TableCell colSpan={6} align="center">
                          <CircularProgress size={20} />
                        </TableCell>
                      </TableRow>
                    )}
                    {horas?.map((h) => {
                      const color = h.dispPct < datos.umbralRojo ? "ROJO" : h.dispPct < datos.umbralNaranja ? "NARANJA" : "VERDE";
                      return (
                        <TableRow key={h.hora}>
                          <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{String(h.hora).padStart(2, "0")}:00</TableCell>
                          <TableCell align="right">{h.cantEquipo}</TableCell>
                          <TableCell align="right">{h.eqRequerido}</TableCell>
                          <TableCell align="right">{h.eqDisponible}</TableCell>
                          <TableCell align="right" sx={{ color: COLOR_TEXTO[color], fontWeight: color === "VERDE" ? 500 : 700 }}>
                            {num(h.dispPct)}
                          </TableCell>
                          <TableCell align="right">{num(h.potencialPct)}</TableCell>
                        </TableRow>
                      );
                    })}
                    {horas?.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={6} align="center" sx={{ color: "text.secondary" }}>
                          Sin horas medidas.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </Grid>

            <Grid size={{ xs: 12, md: 7 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1 }}>
                Órdenes de trabajo ({datos.ordenes.length})
              </Typography>
              <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 260 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>No. Orden</TableCell>
                      <TableCell>Tipo</TableCell>
                      <TableCell>Fecha</TableCell>
                      <TableCell>Fecha fin</TableCell>
                      <TableCell>Ficha</TableCell>
                      <TableCell>Estado actual</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {datos.ordenes.map((o) => (
                      <TableRow
                        key={`${o.ordenSapId}-${o.ficha}`}
                        hover
                        selected={orden?.ordenSapId === o.ordenSapId}
                        onClick={() => setOrden(o)}
                        sx={{ cursor: "pointer" }}
                      >
                        <TableCell sx={{ fontVariantNumeric: "tabular-nums", whiteSpace: "nowrap" }}>
                          {o.ordenSapId}
                          {o.avisoParada && (
                            <Typography component="span" variant="caption" color="error.main" fontWeight={700} sx={{ ml: 0.75 }} title="El aviso marcó la máquina parada">
                              ● parada
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>{TIPO_ORDEN[o.tipo] ?? o.tipo}</TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaHora(o.desde)}</TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>{o.hasta ? fechaHora(o.hasta) : "Abierta"}</TableCell>
                        <TableCell>{o.ficha}</TableCell>
                        <TableCell>{o.estadoActual}</TableCell>
                      </TableRow>
                    ))}
                    {datos.ordenes.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={6} align="center" sx={{ color: "text.secondary", py: 3 }}>
                          Ningún equipo estuvo fuera de servicio esta semana.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
              {orden && (
                <Paper variant="outlined" sx={{ mt: 1, p: 1.5 }}>
                  <Typography variant="caption" color="text.secondary" fontWeight={700}>
                    OBS. — {orden.ordenSapId} · {orden.equipoDescripcion ?? orden.ficha} · desde {fechaCorta(orden.desde)}
                  </Typography>
                  <Typography variant="body2">{orden.observacion ?? "—"}</Typography>
                  <AvisoParada parada={orden.avisoParada} inicio={orden.inicioAveria} fin={orden.finAveria} conFechas />
                </Paper>
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
